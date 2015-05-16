[<System.Obsolete("Open Fake.Testing to use the latest xUnit2 task.")>]
/// DEPRECATED. See [`Fake.Testing.XUnit2`](fake-testing-xunit2.html).
///
/// Contains tasks to run [xUnit](https://github.com/xunit/xunit) unit tests.
module Fake.XUnit2Helper

open System
open System.IO
open System.Text

#nowarn "44"

(*
xUnit.net console test runner (64-bit .NET 4.0.30319.34209)
Copyright (C) 2014 Outercurve Foundation.

usage: xunit.console <assemblyFile> [configFile] [options]

Valid options:
  -parallel option       : set parallelization based on option
                         :   none - turn off all parallelization
                         :   collections - only parallelize collections
                         :   assemblies - only parallelize assemblies
                         :   all - parallelize assemblies & collections
  -maxthreads count      : maximum thread count for collection parallelization
                         :   0 - run with unbounded thread count
                         :   >0 - limit task thread pool size to 'count'
  -silent                : do not output running test count
  -noshadow              : do not shadow copy assemblies
  -teamcity              : forces TeamCity mode (normally auto-detected)
  -appveyor              : forces AppVeyor CI mode (normally auto-detected)
  -wait                  : wait for input after completion
  -trait "name=value"    : only run tests with matching name/value traits
                         : if specified more than once, acts as an OR operation
  -notrait "name=value"  : do not run tests with matching name/value traits
                         : if specified more than once, acts as an AND operation
  -xml <filename>        : output results to xUnit.net v2 style XML file
  -xmlv1 <filename>      : output results to xUnit.net v1 style XML file
  -html <filename>       : output results to HTML file
*)

/// DEPRECATED.
[<Obsolete("This type will be removed in a future version.")>]
type ParallelOption =
    | None = 0
    | Collections = 1
    | Assemblies = 2
    | All = 3

/// DEPRECATED.
/// Option which allows to specify if an xUnit error should break the build.
[<Obsolete("This type alias will be removed in a future version.")>]
type XUnit2ErrorLevel = TestRunnerErrorLevel // a type alias to keep backwards compatibility

/// DEPRECATED.
/// The xUnit parameter type
[<Obsolete("This type will be removed in a future version. See Fake.Testing.XUnit2.XUnit2Params")>]
type XUnit2Params =
    { /// The path to the xunit.console.exe - FAKE will scan all subfolders to find it automatically.
      ToolPath : string
      /// The file name of the config file (optional).
      ConfigFile : string
      /// set parallelization based on option
      ///   none - turn off all parallelization
      ///   collections - only parallelize collections
      ///   assemblies - only parallelize assemblies
      ///   all - parallelize assemblies & collections
      Parallel : ParallelOption
      /// maximum thread count for collection parallelization
      /// 0 - run with unbounded thread count
      /// >0 - limit task thread pool size to 'count'
      MaxThreads : int
      /// Output running test count
      Silent : bool
      /// Shadow copy
      ShadowCopy : bool
      /// forces TeamCity mode (normally auto-detected)
      Teamcity : bool
      /// forces AppVeyor CI mode (normally auto-detected)
      Appveyor : bool
      // wait for input after completion
      Wait : bool
      /// The working directory (optional).
      WorkingDir : string
      /// If the timeout is reached the xUnit task will be killed. Default is 5 minutes.
      TimeOut : TimeSpan
      /// Test runner error level. Option which allows to specify if an xUnit error should break the build.
      ErrorLevel : XUnit2ErrorLevel
      /// Include named traits with comma separated values
      IncludeTraits : (string * string) option
      /// Exclude named traits with comma separated values
      ExcludeTraits : (string * string) option
      /// output results to xUnit.net v2 style XML file
      XmlOutput : bool
      /// output results to xUnit.net v1 style XML file
      XmlOutputV1 : bool
      /// output results to HTML file
      HtmlOutput : bool
      /// output directory
      OutputDir : string }

/// DEPRECATED.
/// The xUnit default parameters
[<Obsolete("This value will be removed in a future version.")>]
let empty2Trait : (string * string) option = None

/// DEPRECATED.
[<Obsolete("This value will be removed in a future version. See Fake.Testing.XUnit2.XUnit2Defaults")>]
let XUnit2Defaults =
    { ToolPath = findToolInSubPath "xunit.console.exe" (currentDirectory @@ "tools" @@ "xUnit")
      ConfigFile = null
      Parallel = ParallelOption.None
      MaxThreads = 0
      Silent = false
      ShadowCopy = true
      Teamcity = false
      Appveyor = false
      Wait = false
      WorkingDir = null
      TimeOut = TimeSpan.FromMinutes 5.
      ErrorLevel = Error
      IncludeTraits = empty2Trait
      ExcludeTraits = empty2Trait
      XmlOutput = false
      XmlOutputV1 = false
      HtmlOutput = false
      OutputDir = null }

/// DEPRECATED.
/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
[<Obsolete("This function will be removed in a future version.")>]
let buildXUnit2Args parameters assembly =
    let fi = fileInfo assembly
    let name = fi.Name

    let dir =
        if isNullOrEmpty parameters.OutputDir then String.Empty
        else Path.GetFullPath parameters.OutputDir

    let traits includeExclude (name, values : string) =
        values.Split([| ',' |], System.StringSplitOptions.RemoveEmptyEntries)
        |> Seq.collect (fun value ->
               [| includeExclude
                  sprintf "\"%s=%s\"" name value |])
        |> String.concat " "

    let parallelOptionsText =
        parameters.Parallel.ToString().ToLower()

    new StringBuilder()
    |> appendFileNamesIfNotNull [ assembly ]
    |> append "-parallel"
    |> append (sprintf "%s" parallelOptionsText)
    |> append "-maxthreads"
    |> append (sprintf "%i" parameters.MaxThreads)
    |> appendIfFalse parameters.ShadowCopy "-noshadow"
    |> appendIfTrue (buildServer = TeamCity || parameters.Teamcity) "-teamcity"
    |> appendIfTrue (buildServer = AppVeyor || parameters.Appveyor) "-appveyor"
    |> appendIfTrue parameters.Wait "-wait"
    |> appendIfTrue parameters.Silent "-silent"
    |> appendIfTrue parameters.XmlOutput (sprintf "-xml\" \"%s" (dir @@ (name + ".xml")))
    |> appendIfTrue parameters.XmlOutputV1 (sprintf "-xmlv1\" \"%s" (dir @@ (name + ".xml")))
    |> appendIfTrue parameters.HtmlOutput (sprintf "-html\" \"%s" (dir @@ (name + ".html")))
    |> appendIfSome parameters.IncludeTraits (traits "-trait")
    |> appendIfSome parameters.ExcludeTraits (traits "-notrait")
    |> toText


/// DEPRECATED. See [`Fake.Testing.XUnit2.xUnit2`](fake-testing-xunit2.html).
///
/// Runs xUnit unit tests in the given assemblies via the given xUnit runner.
/// Will fail if the runner terminates with non-zero exit code for any of the assemblies.
/// Offending assemblies will be listed in the error message.
///
/// The xUnit runner terminates with a non-zero exit code if any of the tests
/// in the given assembly fail.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default XUnitParams value.
///  - `assemblies` - Sequence of one or more assemblies containing xUnit unit tests.
///
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\xUnit.Test.*.dll")
///           |> xUnit2 (fun p -> {p with OutputDir = testDir })
///     )
[<Obsolete("Deprecated. This task will be removed in a future version. Open Fake.Testing to use the latest xUnit2 task.")>]
let xUnit2 setParams assemblies =
    let details = separated ", " assemblies
    traceStartTask "xUnit2" details
    let parameters = setParams XUnit2Defaults

    let runTests assembly =
        let args = buildXUnit2Args parameters assembly
        0 = ExecProcess (fun info ->
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut

    let failedTests =
        [ for asm in List.ofSeq assemblies do
              if runTests asm |> not then yield asm ]

    if not (List.isEmpty failedTests) then
        sprintf "xUnit2 failed for the following assemblies: %s" (separated ", " failedTests)
        |> match parameters.ErrorLevel with
           | Error | FailOnFirstError -> failwith
           | DontFailBuild -> traceImportant
    traceEndTask "xUnit2" details
