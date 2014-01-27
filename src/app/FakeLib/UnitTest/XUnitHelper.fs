[<AutoOpen>]
/// Contains tasks to run [xUnit](http://xunit.codeplex.com/) unit tests.
module Fake.XUnitHelper

open System
open System.IO
open System.Text

/// Option which allows to specify if an xUnit error should break the build.
type XUnitErrorLevel = TestRunnerErrorLevel // a type alias to keep backwards compatibility

/// The xUnit parameter type
type XUnitParams = { 
      /// The path to the xunit.console.clr4.exe - FAKE will scan all subfolders to find it automatically.
      ToolPath: string
      /// The file name of the config file (optional).
      ConfigFile: string
      /// If set to true a HTML output file will be generated.
      HtmlOutput: bool
      /// If set to true a HTML output file will be generated in NUnit format.
      NUnitXmlOutput: bool
      /// If set to true XML output will be generated.
      XmlOutput: bool
      /// The working directory (optional).
      WorkingDir: string
      /// If set to true xUnit will run in ShadowCopy mode.
      ShadowCopy: bool
      /// If set to true xUnit will generate verbose output.
      Verbose: bool
      /// If the timeout is reached the xUnit task will be killed. Default is 5 minutes.
      TimeOut: TimeSpan
      /// The output directory. It's the current directoy if nothing else is specified.
      OutputDir: string
      /// Test runner error level. Option which allows to specify if an xUnit error should break the build.
      ErrorLevel: XUnitErrorLevel }

/// The xUnit default parameters
let XUnitDefaults =
    { ToolPath = findToolInSubPath "xunit.console.clr4.exe" (currentDirectory @@ "tools" @@ "xUnit")
      ConfigFile = null;
      HtmlOutput = false;
      NUnitXmlOutput = false;
      WorkingDir = null;
      ShadowCopy = true;
      Verbose = true;
      XmlOutput = false;
      TimeOut = TimeSpan.FromMinutes 5.
      OutputDir = null
      ErrorLevel = Error }

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
let buildXUnitArgs parameters assembly =
    let fi = fileInfo assembly
    let name = fi.Name

    let dir =
        if isNullOrEmpty parameters.OutputDir then String.Empty else
        Path.GetFullPath parameters.OutputDir

    new StringBuilder()
        |> appendFileNamesIfNotNull [assembly]
        |> appendIfFalse parameters.ShadowCopy "/noshadow"
        |> appendIfTrue (buildServer = TeamCity) "/teamcity"
        |> appendIfFalse parameters.Verbose "/silent"
        |> appendIfTrue parameters.XmlOutput (sprintf "/xml\" \"%s" (dir @@ (name + ".xml")))
        |> appendIfTrue parameters.HtmlOutput (sprintf "/html\" \"%s" (dir @@ (name + ".html")))
        |> appendIfTrue parameters.NUnitXmlOutput (sprintf "/nunit\" \"%s" (dir @@ (name + ".xml")))
        |> toText
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
///           |> xUnit (fun p -> {p with OutputDir = testDir })
///     )
let xUnit setParams assemblies = 
    let details = separated ", " assemblies
    traceStartTask "xUnit" details
    let parameters = setParams XUnitDefaults

    let runTests assembly =
       let args = buildXUnitArgs parameters assembly

       0 = ExecProcess (fun info ->
           info.FileName <- parameters.ToolPath
           info.WorkingDirectory <- parameters.WorkingDir
           info.Arguments <- args) parameters.TimeOut

    let failedTests =
        [ for asm in List.ofSeq assemblies do
              if runTests asm |> not then 
                  yield asm ]

    if not (List.isEmpty failedTests) then
        sprintf "xUnit failed for the following assemblies: %s" (separated ", " failedTests)
        |> match parameters.ErrorLevel with
           | Error -> failwith
           | DontFailBuild -> traceImportant

    traceEndTask "xUnit" details