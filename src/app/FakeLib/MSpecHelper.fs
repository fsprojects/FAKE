[<AutoOpen>]
module Fake.MSpecHelper

open System
open System.IO
open System.Text

type MSpecParams =
    { ToolPath: string;
      HtmlOutputDir: string;
      WorkingDir:string; 
      TimeOut: TimeSpan}

/// MSpec default params  
let MSpecDefaults =
    { ToolPath = currentDirectory @@ "tools" @@ "MSpec" @@ "mspec.exe";
      HtmlOutputDir = null;
      WorkingDir = null;
      TimeOut = TimeSpan.FromMinutes 5.}

let MSpec setParams assemblies = 
    let details = separated ", " assemblies
    traceStartTask "MSpec" details
    let parameters = setParams MSpecDefaults
    assemblies
      |> Seq.iter (fun assembly ->
          let commandLineBuilder =
              let html = isNullOrEmpty parameters.HtmlOutputDir |> not

              new StringBuilder()
                |> appendIfTrue (buildServer = TeamCity) "--teamcity"
                |> appendIfTrue html "-t" 
                |> appendIfTrue html (sprintf "--html\" \"%s" <| parameters.HtmlOutputDir.TrimEnd Path.DirectorySeparatorChar) 
                |> appendFileNamesIfNotNull [assembly]

          if not (execProcess3 (fun info ->  
              info.FileName <- parameters.ToolPath
              info.WorkingDirectory <- parameters.WorkingDir
              info.Arguments <- commandLineBuilder.ToString()) parameters.TimeOut)
          then
              failwith "MSpec test failed.")
                  
    traceEndTask "MSpec" details