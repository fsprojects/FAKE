[<AutoOpen>]
/// Contains tasks to run [xUnit](https://github.com/xunit/xunit) v1 unit tests.
module Fake.Testing.XUnit

open System
open System.IO
open System.Text
open Fake

/// The xUnit parameter type.
type XUnitParams =
    { /// The path to the xUnit console runner: `xunit.console.clr4.exe`
      ToolPath : string
      /// The output path of the xUnit HTML report.
      HtmlOutputPath : string option
      /// The output path of the xUnit XML report (in the NUnit style).
      NUnitXmlOutputPath : string option
      /// The output path of the xUnit XML report.
      XmlOutputPath : string option
      /// The working directory for running the xunit console rnner.
      WorkingDir : string option
      /// Run xUnit with shadow copy enabled.
      ShadowCopy : bool
      /// Run xUnit without reporting test progress.
      Silent : bool
      /// Maximum time to allow xUnit to run before being killed.
      TimeOut : TimeSpan
      /// Test runner error level.
      ErrorLevel : TestRunnerErrorLevel
      /// List of traits to include.
      IncludeTraits : (string * string) list
      /// List of traits to exclude.
      ExcludeTraits : (string * string) list
      /// Forces TeamCity mode (normally auto-detected)
      ForceTeamCity : bool }

/// The xUnit default parameters.
///
/// ## Defaults
///
/// - `HtmlOutputPath` - `None`
/// - `XmlOutputPath` - `None`
/// - `NUnitXmlOutputPath` - `None`
/// - `IncludeTraits` - `[]`
/// - `ExcludeTraits` - `[]`
/// - `ShadowCopy` - `true`
/// - `ErrorLevel` - `Error`
/// - `ToolPath` - The `xunit.console.clr4.exe` path if it exists in a subdirectory of the current directory.
/// - `WorkingDir` - `None`
/// - `TimeOut` - 5 minutes
/// - `ForceTeamCity` - `false`
/// - `Silent` - `false`
let XUnitDefaults =
    { HtmlOutputPath = None
      NUnitXmlOutputPath = None
      XmlOutputPath = None
      IncludeTraits = []
      ExcludeTraits = []
      ShadowCopy = true
      ErrorLevel = Error
      ToolPath = findToolInSubPath "xunit.console.clr4.exe" (currentDirectory @@ "tools" @@ "xUnit")
      WorkingDir = None
      TimeOut = TimeSpan.FromMinutes 5.
      ForceTeamCity = false
      Silent = false }

let internal buildXUnitArgs assembly parameters =
    let formatTrait traitFlag (name, value) =
        sprintf @"%s ""%s=%s""" traitFlag name value
    let appendTraits traitsList traitFlag sb =
        traitsList |>
        Seq.fold (fun sb traitPair -> sb |> appendWithoutQuotes (formatTrait traitFlag traitPair)) sb

    new StringBuilder()
    |> appendFileNamesIfNotNull [ assembly ]
    |> appendIfTrueWithoutQuotes (not parameters.ShadowCopy) "/noshadow"
    |> appendIfTrueWithoutQuotes parameters.ForceTeamCity "/teamcity"
    |> appendIfTrueWithoutQuotes parameters.Silent "/silent"
    |> appendIfSome parameters.XmlOutputPath (sprintf @"/xml ""%s""")
    |> appendIfSome parameters.HtmlOutputPath (sprintf @"/html ""%s""")
    |> appendIfSome parameters.NUnitXmlOutputPath (sprintf @"/nunit ""%s""")
    |> appendTraits parameters.IncludeTraits "/trait"
    |> appendTraits parameters.ExcludeTraits "/-trait"
    |> toText

module internal ResultHandling =
    let (|OK|TestsFailed|FatalError|) = function
        | 0 -> OK
        | errorCode when errorCode < 0 -> FatalError errorCode
        | x -> TestsFailed x

    let buildErrorMessage = function
        | OK -> None
        | TestsFailed failedTestCount ->
            Some (sprintf "xUnit reported %d failed tests" failedTestCount)
        | FatalError errorCode ->
            Some (sprintf "xUnit reported a fatal error (Error Code %d)" errorCode)

    let failBuildWithMessage = function
        | DontFailBuild -> traceImportant
        | _ -> failwith

    let failBuildIfXUnitReportedError errorLevel =
        buildErrorMessage
        >> Option.iter (failBuildWithMessage errorLevel)

    let consolidateErrorMessages errorLevel =
        Seq.map (fun (a, result) -> a, buildErrorMessage result)
        >> Seq.choose (fun (a, msgOpt) -> Option.map (fun msg -> (a, msg)) msgOpt)
        >> List.ofSeq

    let failBuildIfXUnitReportedErrors errorLevel =
        consolidateErrorMessages errorLevel
        >> function
        | [] -> ()
        | messages ->
            ( "Errors reported by xUnit runner:"
              :: ( messages |> List.map (fun (a, msg) -> sprintf "\t%s: %s" a msg) ) )
            |> toLines
            |> failBuildWithMessage errorLevel


let internal runXUnitForOneAssembly parameters assembly =
    let result =
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- defaultArg parameters.WorkingDir "."
            info.Arguments <- parameters |> buildXUnitArgs assembly) parameters.TimeOut

    ResultHandling.failBuildIfXUnitReportedError parameters.ErrorLevel result

    result

/// Runs xUnit unit tests in the given assemblies via the given xUnit runner.
/// Will fail if the runner terminates with non-zero exit code for any of the assemblies.
///
/// The xUnit runner terminates with a non-zero exit code if any of the tests
/// in the given assembly fail.
///
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default `XUnitParams` value.
///  - `assemblies` - Sequence of one or more assemblies containing xUnit unit tests.
///
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         xUnit (fun p -> {p with HtmlOutputPath = testDir @@ "xunit.html"}) "xUnit.Test.dll"
///     )
let xUnitSingle setParams assembly =
    traceStartTask "xUnit" assembly

    let parameters = XUnitDefaults |> setParams

    runXUnitForOneAssembly parameters assembly |> ignore

    traceEndTask "xUnit" assembly

let internal overrideAssemblyReportParams assembly p =
    let prependAssemblyName path =
        (directory path) @@ ((filename assembly) + "." + (filename path))
    { p with
        HtmlOutputPath = Option.map prependAssemblyName p.HtmlOutputPath
        NUnitXmlOutputPath = Option.map prependAssemblyName p.NUnitXmlOutputPath
        XmlOutputPath = Option.map prependAssemblyName p.XmlOutputPath
        ErrorLevel =
            match p.ErrorLevel with
            | Error -> DontFailBuild
            | _ -> p.ErrorLevel }

/// Runs xUnit unit tests in the given assemblies via the given xUnit runner.
/// Will fail if the runner terminates with non-zero exit code for any of the assemblies.
///
/// The xUnit runner terminates with a non-zero exit code if any of the tests
/// in the given assembly fail.
///
/// This task runs xUnit once per assembly specified, prepending the assembly file name to
/// the output report filenames to ensure that there is a unique report file for each
/// assembly tested.
///
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default XUnitParams value.
///  - `assemblies` - Sequence of one or more assemblies containing xUnit unit tests.
///
/// ## Sample usage
/// The sample below will generate HTML reports in `testDir` with names following the
/// pattern `xUnit.Test.Example.dll.html`.
///
///     Target "Test" (fun _ ->
///         !! (testDir @@ "xUnit.Test.*.dll")
///           |> xUnit (fun p -> {p with HtmlOutputPath = testDir @@ "html"})
///     )
let xUnit setParams assemblies =
    let details = separated ", " assemblies
    traceStartTask "xUnit" details

    let parameters = XUnitDefaults |> setParams

    let assemblyResults =
        assemblies
        |> Seq.map (fun a -> a, runXUnitForOneAssembly (parameters |> overrideAssemblyReportParams a) a)

    ResultHandling.failBuildIfXUnitReportedErrors parameters.ErrorLevel assemblyResults

    traceEndTask "xUnit" details