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
      ProjectFile:string;
      Dependencies: (string*string) list;
      PublishTrials: int;
      Publish:bool }

/// NuGet default params  
let NuGetDefaults() =
    { ToolPath = currentDirectory @@ "tools" @@ "NuGet" @@ "NuGet.exe"
      TimeOut = TimeSpan.FromMinutes 5.
      Version = if not isLocalBuild then buildVersion else "0.1.0.0"
      Authors = []
      Project = "";
      Summary = null;
      ProjectFile = null;
      Description = null;
      Dependencies = [];
      OutputPath = currentDirectory @@ "NuGet";
      PublishUrl = null;
      AccessKey = null;
      PublishTrials = 5;
      Publish = false}

let RequireExactly version = sprintf "[%s]" version

/// Gets the version no. for a given package in the packages folder
let GetPackageVersion packagesDir package = 
    let version = 
        Directory.GetDirectories(packagesDir, sprintf "%s.*" package) 
        |> Seq.head
        |> fun full -> full.Substring (full.LastIndexOf package + package.Length + 1)

    logfn "Version %s found for package %s" version package
    version

let private replaceAccessKey key (s:string) = s.Replace(key,"PRIVATEKEY")

let private runNuget parameters nuSpec =
    let version = NormalizeVersion parameters.Version
    // create .nuspec file
    CopyFile parameters.OutputPath nuSpec

    let specFile = parameters.OutputPath @@ (nuSpec |> Path.GetFileName)
    let packageFile = sprintf "%s.%s.nupkg" parameters.Project version
    let dependencies =
        if parameters.Dependencies = [] then "" else
        parameters.Dependencies
          |> Seq.map (fun (package,version) -> sprintf "<dependency id=\"%s\" version=\"%s\" />" package version)
          |> separated "\r\n"
          |> fun s -> sprintf "<dependencies>\r\n%s\r\n</dependencies>" s

    let replacements =
        ["@build.number@",version
         "@authors@",parameters.Authors |> separated ", "
         "@project@",parameters.Project
         "@summary@",if isNullOrEmpty parameters.Summary then "" else parameters.Summary
         "@dependencies@",dependencies
         "@description@",parameters.Description]

    processTemplates replacements [specFile]

    if parameters.ProjectFile <> null then
        // create symbols package
        let args = sprintf "pack -sym \"%s\"" (parameters.ProjectFile |> FullName)
        let result = 
            ExecProcess (fun info ->
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.OutputPath |> FullName
                info.Arguments <- args) parameters.TimeOut
               
        if result <> 0 then failwithf "Error during NuGet symbols creation. %s %s" parameters.ToolPath args
        parameters.OutputPath @@ packageFile |> DeleteFile

    // create package
    let args = sprintf "pack %s" (Path.GetFileName specFile)
    let result = 
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.OutputPath |> FullName
            info.Arguments <- args) parameters.TimeOut
               
    if result <> 0 then failwithf "Error during NuGet creation. %s %s" parameters.ToolPath args

    // push package (and try again if something fails)
    let rec publish trials =
        let tracing = enableProcessTracing
        enableProcessTracing <- false
        let source = if isNullOrEmpty parameters.PublishUrl then "" else sprintf "-s %s" parameters.PublishUrl
        let args = sprintf "push \"%s\" %s %s" packageFile parameters.AccessKey source

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
        if result <> 0 then 
            if trials > 0 then publish (trials - 1) else
            failwithf "Error during NuGet push. %s %s" parameters.ToolPath args
            
    let symbolsPackage = packageFile.Replace(".nupkg",".symbols.nupkg")

    // push package to symbol server (and try again if something fails)
    let rec publishSymbols trials =
        let tracing = enableProcessTracing
        enableProcessTracing <- false
        let args = sprintf "push -source %s \"%s\" %s" parameters.PublishUrl symbolsPackage parameters.AccessKey

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
        if result <> 0 then 
            if trials > 0 then publishSymbols (trials - 1) else
            failwithf "Error during NuGet symbol push. %s %s" parameters.ToolPath args                            
            
    if parameters.Publish then 
        publish parameters.PublishTrials
        if parameters.ProjectFile <> null then publishSymbols parameters.PublishTrials
        
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

let feedUrl = "http://go.microsoft.com/fwlink/?LinkID=206669"

let getRepoUrl() =
    let webClient = new System.Net.WebClient()

    let resp = webClient.DownloadString(feedUrl)
    let doc = XMLDoc resp

    doc.["service"].GetAttribute("xml:base")