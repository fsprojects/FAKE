[<AutoOpen>]
module Fake.NuGetHelper

open System
open System.IO

type NuGetParams =
    { ToolPath: string;
      TimeOut: TimeSpan;
      Version: string;
      Authors: string list;
      Project: string;
      Summary: string;
      Description: string;                               
      OutputPath: string}

/// NuGet default params  
let NuGetDefaults() =
    { ToolPath = currentDirectory @@ "tools" @@ "NuGet" @@ "NuGet.exe"
      TimeOut = TimeSpan.FromMinutes 5.
      Version = if not isLocalBuild then buildVersion else "0.1.0.0"
      Authors = []
      Project = "";
      Summary = null;
      Description = null;
      OutputPath = currentDirectory @@ "NuGet" }
 
/// Creates a new NuGet package   
let NuGet setParams nuSpec = 
    traceStartTask "NuGet" nuSpec

    let parameters = NuGetDefaults() |> setParams

    CopyFile parameters.OutputPath nuSpec

    let specFile = parameters.OutputPath @@ nuSpec

    let replacements =
        ["@build.number@",parameters.Version
         "@authors@",parameters.Authors |> Seq.map (sprintf "<author>%s</author>") |> separated " "
         "@project@",parameters.Project
         "@summary@",if isNullOrEmpty parameters.Summary then "" else parameters.Summary
         "@description@",parameters.Description]

    processTemplates replacements [specFile]

    let args = sprintf "pack %s" nuSpec
    let result = 
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.OutputPath |> FullName
            info.Arguments <- args) parameters.TimeOut
               
    if result <> 0 then failwithf "Error during NuGet creation. %s %s" parameters.ToolPath args
                    
    traceEndTask "NuGet" nuSpec