namespace Fake.DotNet.Testing

open System.IO
open Fake.IO
open Fake.Testing.Common
open Fake.Core
open System
open System.Text

/// Contains tasks to run [xUnit](https://github.com/xunit/xunit) v2 unit tests.
module XUnit2 =

    /// The parallelization mode of the xUnit2 runner.
    type ParallelMode =
        /// Turn off all parallelization.
        | NoParallelization
        /// Only parallelize collections.
        | Collections
        /// Only parallelize assemblies.
        | Assemblies
        /// Parallelize assemblies and collections.
        | All

        static member internal ToArgument =
            function
            | NoParallelization -> "none"
            | Collections -> "collections"
            | Assemblies -> "assemblies"
            | All -> "all"

    /// The collection concurrency mode used by the xUnit2 runner.
    type CollectionConcurrencyMode =
        /// Uses the default concurrency mode for collections.
        | Default
        /// Does not limit the number of concurrently executing collections.
        | Unlimited
        /// Limits the number of concurrently executing collections to `count`.
        | MaxThreads of count: int

        static member internal ToArgument =
            function
            | Default -> None
            | Unlimited -> Some 0
            | MaxThreads count -> Some count

    /// The xUnit2 parameter type.
    [<CLIMutable>]
    type XUnit2Params =
        {
            /// The path to the xUnit console runner: `xunit.console.exe`
            ToolPath: string
            
            /// Do not use app domains to run test code.
            NoAppDomain: bool
            
            /// The xUnit parallelization mode.
            Parallel: ParallelMode
            
            /// The xUnit thread limiting strategy.
            MaxThreads: CollectionConcurrencyMode
            
            /// The output path of the xUnit HTML report.
            HtmlOutputPath: string option
            
            /// The output path of the xUnit XML report.
            XmlOutputPath: string option
            
            /// The output path of the xUnit XML report (in the xUnit v1 style).
            XmlV1OutputPath: string option
            
            /// The output path of the NUnit XML report.
            NUnitXmlOutputPath: string option
            
            /// The working directory for running the xunit console runner.
            WorkingDir: string option
            
            /// Run xUnit with shadow copy enabled.
            ShadowCopy: bool
            
            /// Run xUnit without reporting test progress.
            Silent: bool
            
            /// Maximum time to allow xUnit to run before being killed.
            TimeOut: TimeSpan
            
            /// Test runner error level.
            ErrorLevel: TestRunnerErrorLevel
            
            /// List of traits to include.
            IncludeTraits: (string * string) list
            
            /// List of traits to exclude.
            ExcludeTraits: (string * string) list
            
            /// Forces TeamCity mode (normally auto-detected).
            ForceTeamCity: bool
            
            /// Forces AppVeyor CI mode (normally auto-detected).
            ForceAppVeyor: bool
            
            /// Waits for input after completion.
            Wait: bool
            
            /// Run xUnit against a specific namespace
            Namespace: string option
            
            /// Run xUnit against a specific class
            Class: string option
            
            /// Run xUnit against a specific method
            Method: string option
        }

    let private toolPath =
        let xUnitTool = "xunit.console.exe"

        let toolPath = ProcessUtils.tryFindLocalTool "TOOL" xUnitTool [ Directory.GetCurrentDirectory() ]

        match toolPath with
        | Some path -> path
        | None ->
            match ProcessUtils.tryFindFileOnPath xUnitTool with
            | Some inno when File.exists inno -> inno
            | _ -> xUnitTool
    
    /// The xUnit2 default parameters.
    let XUnit2Defaults =
        { NoAppDomain = false
          Parallel = NoParallelization
          MaxThreads = Default
          HtmlOutputPath = None
          XmlOutputPath = None
          XmlV1OutputPath = None
          NUnitXmlOutputPath = None
          IncludeTraits = []
          ExcludeTraits = []
          ShadowCopy = true
          ErrorLevel = Error
          ToolPath = toolPath
          WorkingDir = None
          TimeOut = TimeSpan.FromMinutes 5.
          ForceTeamCity = false
          ForceAppVeyor = false
          Silent = false
          Wait = false
          Namespace = None
          Class = None
          Method = None }

    /// Builds the command line arguments from the given parameter record and the given assemblies.
    ///
    /// ## Parameters
    ///  - `parameters` - XUnit parameters
    ///  - `assemblies` - List of assemblies to run tests in
    let buildArgs (parameters: XUnit2Params) (assemblies: string seq) =
        let formatTrait traitFlag (name, value) =
            sprintf @"%s ""%s=%s""" traitFlag name value

        let appendTraits traitsList traitFlag sb =
            traitsList
            |> Seq.fold
                (fun sb traitPair -> sb |> StringBuilder.appendWithoutQuotes (formatTrait traitFlag traitPair))
                sb

        StringBuilder()
        |> StringBuilder.appendFileNamesIfNotNull assemblies
        |> StringBuilder.appendIfTrueWithoutQuotes parameters.NoAppDomain "-noappdomain"
        |> StringBuilder.appendWithoutQuotes "-parallel"
        |> StringBuilder.appendWithoutQuotes (ParallelMode.ToArgument parameters.Parallel)
        |> StringBuilder.appendIfSome
            (CollectionConcurrencyMode.ToArgument parameters.MaxThreads)
            (sprintf "-maxthreads %d")
        |> StringBuilder.appendIfTrueWithoutQuotes (not parameters.ShadowCopy) "-noshadow"
        |> StringBuilder.appendIfTrueWithoutQuotes parameters.ForceTeamCity "-teamcity"
        |> StringBuilder.appendIfTrueWithoutQuotes parameters.ForceAppVeyor "-appveyor"
        |> StringBuilder.appendIfTrueWithoutQuotes parameters.Wait "-wait"
        |> StringBuilder.appendIfTrueWithoutQuotes parameters.Silent "-quiet"
        |> StringBuilder.appendIfSome parameters.XmlOutputPath (sprintf @"-xml ""%s""")
        |> StringBuilder.appendIfSome parameters.XmlV1OutputPath (sprintf @"-xmlv1 ""%s""")
        |> StringBuilder.appendIfSome parameters.NUnitXmlOutputPath (sprintf @"-nunit ""%s""")
        |> StringBuilder.appendIfSome parameters.HtmlOutputPath (sprintf @"-html ""%s""")
        |> appendTraits parameters.IncludeTraits "-trait"
        |> appendTraits parameters.ExcludeTraits "-notrait"
        |> StringBuilder.appendIfSome parameters.Namespace (sprintf @"-namespace ""%s""")
        |> StringBuilder.appendIfSome parameters.Class (sprintf @"-class ""%s""")
        |> StringBuilder.appendIfSome parameters.Method (sprintf @"-method ""%s""")
        |> StringBuilder.toText

    /// Helper method to detect if the xunit console runner supports the -noappdomain flag.
    /// If the xunit console runner does not support this flag, it will change the value to false
    /// so it does not interfere with older versions.
    let internal discoverNoAppDomainExists parameters =                
        let helpText =
            CreateProcess.fromRawCommandLine parameters.ToolPath ""
            |> CreateProcess.withTimeout (TimeSpan.FromMinutes 1.)
            |> CreateProcess.withFramework
            |> CreateProcess.redirectOutput
            |> Proc.run
        
        let canSetNoAppDomain = helpText.Result.Output.Contains("-noappdomain")

        { parameters with NoAppDomain = canSetNoAppDomain }

    module internal ResultHandling =
        let (|OK|Failure|) =
            function
            | 0 -> OK
            | x -> Failure x

        let buildErrorMessage =
            function
            | OK -> None
            | Failure errorCode -> Some(sprintf "xUnit2 reported an error (Error Code %d)" errorCode)

        let failBuildWithMessage =
            function
            | DontFailBuild -> Trace.traceImportant
            | _ -> (fun m -> raise (FailedTestsException m))

        let failBuildIfXUnitReportedError errorLevel =
            buildErrorMessage >> Option.iter (failBuildWithMessage errorLevel)

    /// Runs xUnit v2 unit tests in the given assemblies via the given xUnit2 runner.
    /// Will fail if the runner terminates with non-zero exit code.
    /// The xUnit2 runner terminates with a non-zero exit code if any of the tests
    /// in the given assembly fail.
    ///
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default `XUnit2Params` value.
    ///  - `assemblies` - Sequence of one or more assemblies containing xUnit unit tests.
    ///
    /// ## Sample
    ///     open Fake.DotNet.Testing
    ///     open Fake.IO.Globbing.Operators
    ///     Target.create "Test" (fun _ ->
    ///         !! (testDir @@ "xUnit.Test.*.dll")
    ///         |> XUnit2.run (fun p -> { p with HtmlOutputPath = Some (testDir @@ "xunit.html") })
    ///     )
    let run setParams assemblies =
        let details = String.separated ", " assemblies
        use __ = Trace.traceTask "xUnit2" details
        let parametersFirst = setParams XUnit2Defaults

        let parameters =
            if parametersFirst.NoAppDomain then
                discoverNoAppDomainExists parametersFirst
            else
                parametersFirst

        let processResult =
            CreateProcess.fromRawCommandLine parameters.ToolPath (buildArgs parameters assemblies)
            |> CreateProcess.withWorkingDirectory (defaultArg parameters.WorkingDir ".")
            |> CreateProcess.withTimeout parameters.TimeOut
            |> CreateProcess.withFramework
            |> Proc.run

        ResultHandling.failBuildIfXUnitReportedError parameters.ErrorLevel processResult.ExitCode
        __.MarkSuccess()
