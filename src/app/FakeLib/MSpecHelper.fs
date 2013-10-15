[<AutoOpen>]
module Fake.MSpecHelper

open System
open System.IO
open System.Text

type MSpecParams =
    { ToolPath: string;
      HtmlOutputDir: string;
      WorkingDir:string;
      Silent: bool; 
      ExcludeTags: string list; 
      IncludeTags: string list;
      TimeOut: TimeSpan}

/// MSpec default params  
let MSpecDefaults =
    { ToolPath = findToolInSubPath "mspec.exe" (currentDirectory @@ "tools" @@ "MSpec");
      HtmlOutputDir = null;
      WorkingDir = null;
      Silent = false;
      ExcludeTags = [];
      IncludeTags = [];
      TimeOut = TimeSpan.FromMinutes 5.}

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