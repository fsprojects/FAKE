[<AutoOpen>]
module Fake.XpkgHelper

open System
open System.Text

type xpkgParams =
    {
        ToolPath: string;
        WorkingDir:string;
        TimeOut: TimeSpan;
        Package: string;
        Version: string;
        OutputPath: string;
        Project: string;
        Summary: string;
        Publisher: string;
        Website: string;
        Details: string;
        License: string;
        GettingStarted: string;
        Icons: string list;
        Libraries: (string*string) list;
        Samples: (string*string) list;
    }

/// xpkg default params  
let XpkgDefaults() =
    {
        ToolPath = findToolInSubPath "xpkg.exe" (currentDirectory @@ "tools" @@ "xpkg")
        WorkingDir = "./";
        TimeOut = TimeSpan.FromMinutes 5.
        Package = null
        Version = if not isLocalBuild then buildVersion else "0.1.0.0"
        OutputPath = "./xpkg"
        Project = null
        Summary = null
        Publisher = null
        Website = null
        Details = "Details.md"
        License = "License.md"
        GettingStarted = "GettingStarted.md"
        Icons = []
        Libraries = []
        Samples = [];
    }

let private getPackageFileName parameters = sprintf "%s-%s.xam" parameters.Package parameters.Version

/// Creates a new xpkg package based on the packageFileName
let xpkgPack setParams =    
    let parameters = XpkgDefaults() |> setParams
    let packageFileName = getPackageFileName parameters 
    traceStartTask "xpkgPack" packageFileName
    let fullPath = parameters.OutputPath @@ packageFileName

    let commandLineBuilder =
        new StringBuilder()
          |> append "create"
          |> append (sprintf "\"%s\"" fullPath)
          |> appendQuotedIfNotNull parameters.Project "--name="
          |> appendQuotedIfNotNull parameters.Summary "--summary="
          |> appendQuotedIfNotNull parameters.Publisher "--publisher="
          |> appendQuotedIfNotNull parameters.Website "--website="
          |> appendQuotedIfNotNull parameters.Details "--details=" 
          |> appendQuotedIfNotNull parameters.License "--license="
          |> appendQuotedIfNotNull parameters.GettingStarted "--getting-started="

    parameters.Icons
    |> List.map (fun (icon) -> sprintf " --icon=\"%s\"" icon)
    |> List.iter (fun x -> commandLineBuilder.Append x |> ignore)
          
    parameters.Libraries
    |> List.map (fun (platform, library) -> sprintf " --library=\"%s\":\"%s\"" platform library)
    |> List.iter (fun x -> commandLineBuilder.Append x |> ignore)
          
    parameters.Samples
    |> List.map (fun (sample, solution) -> sprintf " --sample=\"%s\":\"%s\"" sample solution)
    |> List.iter (fun x -> commandLineBuilder.Append x |> ignore)


    let args = commandLineBuilder.ToString()
    trace (parameters.ToolPath + " " + args)
    let result =
        execProcessAndReturnExitCode (fun info ->  
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut

    if result = 0 then          
        traceEndTask "xpkgPack" packageFileName
    else
        failwithf "Create xpkg package failed. Process finished with exit code %d." result
        
/// Validates a xpkg package based on the packageFileName
let xpkgValidate setParams =    
    let parameters = XpkgDefaults() |> setParams
    let packageFileName = getPackageFileName parameters 
    traceStartTask "xpkgValidate" packageFileName
    let fullPath = parameters.OutputPath @@ packageFileName

    let commandLineBuilder =
        new StringBuilder()
          |> append "validate"
          |> append (sprintf "\"%s\"" fullPath)

    let args = commandLineBuilder.ToString()
    trace (parameters.ToolPath + " " + args)
    let result =
        execProcessAndReturnExitCode (fun info ->  
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut

    if result = 0 then          
        traceEndTask "xpkgValidate" packageFileName
    else
        failwithf "Validate xpkg package failed. Process finished with exit code %d." result