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
      OutputPath: string;
      PublishUrl: string;
      AccessKey:string;
      Dependencies: (string*string) list;
      Publish:bool }

/// NuGet default params  
let NuGetDefaults() =
    { ToolPath = currentDirectory @@ "tools" @@ "NuGet" @@ "NuGet.exe"
      TimeOut = TimeSpan.FromMinutes 5.
      Version = if not isLocalBuild then buildVersion else "0.1.0.0"
      Authors = []
      Project = "";
      Summary = null;
      Description = null;
      Dependencies = [];
      OutputPath = currentDirectory @@ "NuGet";
      PublishUrl = "http://packages.nuget.org/v1/";
      AccessKey = null;
      Publish = false}
 
let replaceAccessKey key (s:string) = s.Replace(key,"PRIVATEKEY")

let private runNuget parameters nuSpec = 
    // create .nuspec file
    CopyFile parameters.OutputPath nuSpec

    let specFile = parameters.OutputPath @@ nuSpec
    let packageFile = sprintf "%s.%s.nupkg" parameters.Project parameters.Version
    let dependencies =
        if parameters.Dependencies = [] then "" else
        parameters.Dependencies
          |> Seq.map (fun (package,version) -> sprintf "<dependency id=\"%s\" version=\"%s\" />" package version)
          |> separated "\r\n"
          |> fun s -> sprintf "<dependencies>\r\n%s\r\n</dependencies>" s

    let replacements =
        ["@build.number@",parameters.Version
         "@authors@",parameters.Authors |> separated ", "
         "@project@",parameters.Project
         "@summary@",if isNullOrEmpty parameters.Summary then "" else parameters.Summary
         "@dependencies@",dependencies
         "@description@",parameters.Description]

    processTemplates replacements [specFile]

    // create package
    let args = sprintf "pack %s" nuSpec
    let result = 
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.OutputPath |> FullName
            info.Arguments <- args) parameters.TimeOut
               
    if result <> 0 then failwithf "Error during NuGet creation. %s %s" parameters.ToolPath args

    // push package
    if parameters.Publish then
        let tracing = enableProcessTracing
        enableProcessTracing <- false
        let args = sprintf "push -source %s \"%s\" %s" parameters.PublishUrl packageFile parameters.AccessKey

        if tracing then 
            args
              |> replaceAccessKey parameters.AccessKey
              |> tracefn "%s %s" parameters.ToolPath 

        let result = 
            ExecProcess (fun info ->
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.OutputPath |> FullName
                info.Arguments <- args) parameters.TimeOut
        
        enableProcessTracing <- tracing
        if result <> 0 then failwithf "Error during NuGet push. %s %s" parameters.ToolPath args                   

/// Creates a new NuGet package   
let NuGet setParams nuSpec =
    traceStartTask "NuGet" nuSpec
    let parameters = NuGetDefaults() |> setParams
    try    
        runNuget parameters nuSpec
    with
    | exn -> 
        exn.Message
          |> replaceAccessKey parameters.AccessKey
          |> failwith

    traceEndTask "NuGet" nuSpec