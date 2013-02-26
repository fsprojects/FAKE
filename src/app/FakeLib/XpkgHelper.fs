[<AutoOpen>]
module XpkgHelper

open System

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
        ToolPath = "./tools/xpkg/xpkg.exe"
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

let private packageFileName parameters = sprintf "%s-%s.xam" parameters.Package parameters.Version

let xpkgPack setParams =
    traceStartTask "xpkg" packageFileName
    let parameters = XpkgDefaults() |> setParams

    let commandLineBuilder =
        new StringBuilder()
          |> append "create"
          |> append sprintf "\"%s\"" OutputPath @@ packageFileName 
          |> appendIfNotNull parameters.Project sprintf "--name=\"%s\"" parameters.Project 
          |> appendIfNotNull parameters.Summary sprintf "--summary=\"%s\"" parameters.Summary 
          |> appendIfNotNull parameters.Publisher sprintf "--publisher=\"%s\"" parameters.Publisher 
          |> appendIfNotNull parameters.Website sprintf "--website=\"%s\"" parameters.Website 
          |> appendIfNotNull parameters.Details sprintf "--details=\"%s\"" parameters.Details 
          |> appendIfNotNull parameters.License sprintf "--license=\"%s\"" parameters.License 
          |> appendIfNotNull parameters.GettingStarted sprintf "--getting-started=\"%s\"" parameters.GettingStarted 

          parameters.Icons
          |> List.map (fun (icon) -> sprintf "--icon=\"%s\"" icon)
          |> List.iter (fun x -> commandLineBuilder.Add(x))
          
          parameters.Libraries
          |> List.map (fun (platform, library) -> sprintf "--library=\"%s\":\"%s\"" platform library)
          |> List.iter (fun x -> commandLineBuilder.Add(x))
          
          parameters.Samples
          |> List.map (fun (sample, solution) -> sprintf "--sample=\"%s\":\"%s\"" sample solution)
          |> List.iter (fun x -> commandLineBuilder.Add(x))


    let args = commandLineBuilder.ToString()
    trace (parameters.ToolPath + " " + args)
    let result =
        execProcessAndReturnExitCode (fun info ->  
            info.FileName <- tool
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut

    if result = 0 then          
        traceEndTask "xpkg" packageFileName
    else
        failwithf "xpkg create package failed. Process finished with exit code %d." result