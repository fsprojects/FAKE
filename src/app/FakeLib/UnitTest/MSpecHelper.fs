[<AutoOpen>]
/// Contains a task to run [machine.specifications](https://github.com/machine/machine.specifications) tests.
module Fake.MSpecHelper

open System
open System.IO
open System.Text

/// Parameter type to configure the MSpec runner.
type MSpecParams = 
    { /// FileName of the mspec runner exe. Use mspec-clr4.exe if you are on .NET 4.0 or above.
      ToolPath : string
      /// Output directory for html reports (optional).
      HtmlOutputDir : string
      /// Output file path for xml reports (optional).
      XmlOutputPath : string
      /// Working directory (optional)
      WorkingDir : string
      /// Can be used to run MSpec in silent mode.
      Silent : bool
      /// Tests with theses tags are ignored by MSpec
      ExcludeTags : string list
      /// Tests with theses tags are included by MSpec
      IncludeTags : string list
      /// A timeout for the test runner
      TimeOut : TimeSpan
      /// An error level setting to specify whether a failed test should break the build
      ErrorLevel : TestRunnerErrorLevel }

/// MSpec default parameters - tries to locate mspec-clr4.exe in any subfolder.
let MSpecDefaults = 
    { ToolPath = findToolInSubPath "mspec-clr4.exe" (currentDirectory @@ "tools" @@ "MSpec")
      HtmlOutputDir = null
      XmlOutputPath = null
      WorkingDir = null
      Silent = false
      ExcludeTags = []
      IncludeTags = []
      TimeOut = TimeSpan.FromMinutes 5.
      ErrorLevel = Error }

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
let buildMSpecArgs parameters assemblies = 
    let html, htmlText = 
        if isNotNullOrEmpty parameters.HtmlOutputDir then 
            true, sprintf "--html\" \"%s" <| parameters.HtmlOutputDir.TrimEnd Path.DirectorySeparatorChar
        else false, ""

    let xml, xmlText = 
        if isNotNullOrEmpty parameters.XmlOutputPath then 
            true, sprintf "--xml\" \"%s" <| parameters.XmlOutputPath.TrimEnd Path.DirectorySeparatorChar
        else false, ""
    
    let includes = parameters.IncludeTags |> separated ","
    let excludes = parameters.ExcludeTags |> separated ","
    new StringBuilder()
    |> appendIfTrue (buildServer = TeamCity) "--teamcity"
    |> appendIfTrue parameters.Silent "-s"
    |> appendIfTrue html "-t"
    |> appendIfTrue html htmlText
    |> appendIfTrue xml "-t"
    |> appendIfTrue xml xmlText
    |> appendIfTrue (isNotNullOrEmpty excludes) (sprintf "-x\" \"%s" excludes)
    |> appendIfTrue (isNotNullOrEmpty includes) (sprintf "-i\" \"%s" includes)
    |> appendFileNamesIfNotNull assemblies
    |> toText

/// This task to can be used to run [machine.specifications](https://github.com/machine/machine.specifications) on test libraries.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the MSpec default parameters.
///  - `assemblies` - The file names of the test assemblies.
///
/// ## Sample
///
///     !! (testDir @@ "Test.*.dll") 
///       |> MSpec (fun p -> {p with ExcludeTags = ["HTTP"]; HtmlOutputDir = reportDir})
///
/// ## Hint
/// 
/// XmlOutputPath expects a full file path whereas the HtmlOutputDir expects a directory name
let MSpec setParams assemblies = 
    let details = separated ", " assemblies
    traceStartTask "MSpec" details
    let parameters = setParams MSpecDefaults
    let args = buildMSpecArgs parameters assemblies
    trace (parameters.ToolPath + " " + args)
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
    then 
        sprintf "MSpec test failed on %s." details |> match parameters.ErrorLevel with
                                                      | Error | FailOnFirstError -> failwith
                                                      | DontFailBuild -> traceImportant
    traceEndTask "MSpec" details
