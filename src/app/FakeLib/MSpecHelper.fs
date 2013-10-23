[<AutoOpen>]
/// Contains a task to run [machine.specifications](https://github.com/machine/machine.specifications) tests.
module Fake.MSpecHelper

open System
open System.IO
open System.Text

/// Parameter type to configure the MSpec runner
type MSpecParams = {
    /// FileName of the mspec runner
    ToolPath: string
    /// Output directory for html reports (optional)
    HtmlOutputDir: string
    /// Working directory (optional)
    WorkingDir:string
    /// Can be used to run MSpec in silent mode
    Silent: bool;
    /// Tests with theses tags are ignored by MSpec
    ExcludeTags: string list
    /// Tests with theses tags are included by MSpec
    IncludeTags: string list
    /// A timeout for the test runner
    TimeOut: TimeSpan}

/// MSpec default parameters - tries to locate mspec-clr4.exe in any subfolder.
let MSpecDefaults = { 
    ToolPath = findToolInSubPath "mspec-clr4.exe" (currentDirectory @@ "tools" @@ "MSpec")
    HtmlOutputDir = null
    WorkingDir = null
    Silent = false
    ExcludeTags = []
    IncludeTags = []
    TimeOut = TimeSpan.FromMinutes 5.}

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
let MSpec setParams assemblies = 
    let details = separated ", " assemblies
    traceStartTask "MSpec" details
    let parameters = setParams MSpecDefaults
    
    let commandLineBuilder =
        let html = isNotNullOrEmpty parameters.HtmlOutputDir
        let includes = parameters.IncludeTags |> separated ","
        let excludes = parameters.ExcludeTags |> separated ","

        new StringBuilder()
        |> appendIfTrue (buildServer = TeamCity) "--teamcity"
        |> appendIfTrue parameters.Silent "-s" 
        |> appendIfTrue html "-t" 
        |> appendIfTrue html (sprintf "--html\" \"%s" <| parameters.HtmlOutputDir.TrimEnd Path.DirectorySeparatorChar) 
        |> appendIfTrue (isNotNullOrEmpty excludes) (sprintf "-x\" \"%s" excludes) 
        |> appendIfTrue (isNotNullOrEmpty includes) (sprintf "-i\" \"%s" includes) 
        |> appendFileNamesIfNotNull assemblies

    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- commandLineBuilder.ToString()) parameters.TimeOut)
    then
        failwith "MSpec test failed."
                  
    traceEndTask "MSpec" details