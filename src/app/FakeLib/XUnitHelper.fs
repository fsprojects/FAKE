[<AutoOpen>]
/// Contains tasks to run [xUnit](http://xunit.codeplex.com/) unit tests.
module Fake.XUnitHelper

open System
open System.IO
open System.Text

/// The xUnit parameter type
type XUnitParams = { 
      /// The path to the xunit.console.clr4.exe - FAKE will scan all subfolders to find it automatically.
      ToolPath: string
      /// The file name of the config file (optional).
      ConfigFile :string
      /// If set to true a HTML output file will be generated.
      HtmlOutput: bool
      /// If set to true a HTML output file will be generated in NUnit format.
      NUnitXmlOutput: bool
      /// If set to true XML output will be generated.
      XmlOutput: bool
      /// The working directory (optional).
      WorkingDir:string
      /// If set to true xUnit will run in ShadowCopy mode.
      ShadowCopy :bool
      /// If set to true xUnit will generate verbose output.
      Verbose:bool
      /// If the timeout is reached the xUnit task will be killed. Default is 5 minutes.
      TimeOut: TimeSpan
      /// The output directory. It's the current directoy if nothing else is specified.
      OutputDir: string }

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
      OutputDir = null}

/// Runs xUnit unit tests via the given xUnit runner.
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
       let fi = fileInfo assembly
       let name = fi.Name

       let dir =
           if isNullOrEmpty parameters.OutputDir then String.Empty else
           Path.GetFullPath parameters.OutputDir

       let args =
           new StringBuilder()
             |> appendFileNamesIfNotNull [assembly]
             |> appendIfFalse parameters.ShadowCopy "/noshadow"
             |> appendIfTrue (buildServer = TeamCity) "/teamcity"
             |> appendIfFalse parameters.Verbose "/silent"
             |> appendIfTrue parameters.XmlOutput (sprintf "/xml\" \"%s" (dir @@ (name + ".xml")))
             |> appendIfTrue parameters.HtmlOutput (sprintf "/html\" \"%s" (dir @@ (name + ".html")))
             |> appendIfTrue parameters.NUnitXmlOutput (sprintf "/nunit\" \"%s" (dir @@ (name + ".xml")))
             |> toText

       if 0 <> ExecProcess (fun info ->
           info.FileName <- parameters.ToolPath
           info.WorkingDirectory <- parameters.WorkingDir
           info.Arguments <- args) parameters.TimeOut
       then true
       else false

    let failedTests =
        [ for asm in List.ofSeq assemblies do
              let succeeded = runTests asm
              if not succeeded then yield asm ]

    if not (List.isEmpty failedTests)
    then failwithf "xUnit failed for the following assemblies: %s" (separated ", " failedTests)

    traceEndTask "xUnit" details