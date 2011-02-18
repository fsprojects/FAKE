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
      OutputPath = currentDirectory @@ "NuGet";
      PublishUrl = "http://packages.nuget.org/v1/";
      AccessKey = null;
      Publish = false}
 
/// Creates a new NuGet package   
let NuGet setParams nuSpec = 
    traceStartTask "NuGet" nuSpec

    let parameters = NuGetDefaults() |> setParams

    // create .nuspec file
    CopyFile parameters.OutputPath nuSpec

    let specFile = parameters.OutputPath @@ nuSpec
    let packageFile = parameters.OutputPath @@ (sprintf "%s.%s.nupkg" parameters.Project parameters.Version)

    let replacements =
        ["@build.number@",parameters.Version
         "@authors@",parameters.Authors |> separated ", "
         "@project@",parameters.Project
         "@summary@",if isNullOrEmpty parameters.Summary then "" else parameters.Summary
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

        if tracing then tracefn "%s %s" parameters.ToolPath (args.Replace(parameters.AccessKey,"PRIVATEKEY"))

        let result = 
            ExecProcess (fun info ->
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.OutputPath |> FullName
                info.Arguments <- args) parameters.TimeOut
        
        enableProcessTracing <- tracing
        if result <> 0 then failwithf "Error during NuGet push. %s %s" parameters.ToolPath args
                    
    traceEndTask "NuGet" nuSpec