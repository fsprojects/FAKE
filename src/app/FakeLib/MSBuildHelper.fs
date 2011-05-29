[<AutoOpen>]
module Fake.MSBuildHelper

open System
open System.Text
open System.IO
open System.Configuration
open System.Xml
open System.Xml.Linq

type MSBuildProject = XDocument

/// MSBuild exe fileName
let msBuildExe =   
    let ev = environVar "MSBuild"
    if not (isNullOrEmpty ev) then ev else
        if "true".Equals(ConfigurationManager.AppSettings.["IgnoreMSBuild"],StringComparison.OrdinalIgnoreCase) then 
            String.Empty 
        else 
            findPath "MSBuildPath" "MSBuild.exe"


let msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003"
let xname name = XName.Get(name,msbuildNamespace)

let loadProject (projectFileName:string) : MSBuildProject = 
    MSBuildProject.Load(projectFileName,LoadOptions.PreserveWhitespace)

let internal getReferenceElements elementName projectFileName (doc:XDocument) =
    let fi = fileInfo projectFileName
    doc
      .Descendants(xname "Project")
      .Descendants(xname "ItemGroup")
      .Descendants(xname elementName)
        |> Seq.map(fun e -> 
            let a = e.Attribute(XName.Get "Include")
            let value = a.Value
            let fileName =
                if value.StartsWith(".." + directorySeparator) || (not <| value.Contains directorySeparator) then
                    fi.Directory.FullName @@ value
                else
                    value
            a,fileName |> FullName)   


let processReferences elementName f projectFileName (doc:XDocument) =
    let fi = fileInfo projectFileName
    doc
        |> getReferenceElements elementName projectFileName
        |> Seq.iter (fun (a,fileName) -> a.Value <- f fileName)
    doc

let rec getProjectReferences projectFileName= 
    let doc = loadProject projectFileName
    let references =
        getReferenceElements "ProjectReference" projectFileName doc
            |> Seq.map snd

    references
      |> Seq.map getProjectReferences
      |> Seq.concat
      |> Seq.append references
      |> Set.ofSeq

type MSBuildVerbosity = Quiet | Minimal | Normal | Detailed | Diagnostic

type MSBuildParams = 
    { Targets: string list
      Properties: (string * string) list
      MaxCpuCount: int option option
      ToolsVersion: string option
      Verbosity: MSBuildVerbosity option }

let MSBuildDefaults = 
    { Targets = []
      Properties = []
      MaxCpuCount = Some None
      ToolsVersion = None
      Verbosity = None }

let serializeMSBuildParams (p: MSBuildParams) = 
    let targets = 
        match p.Targets with
        | [] -> None
        | t -> Some ("t", t |> separated ";")
    let properties = 
        p.Properties |> List.map (fun (k,v) -> Some ("p", sprintf "%s=\"%s\"" k v))
    let maxcpu = 
        match p.MaxCpuCount with
        | None -> None
        | Some x -> Some ("m", match x with Some v -> v.ToString() | _ -> "")
    let tools =
        match p.ToolsVersion with
        | None -> None
        | Some t -> Some ("tv", t)
    let verbosity = 
        match p.Verbosity with
        | None -> None
        | Some v -> 
            let level = 
                match v with
                | Quiet -> "q"
                | Minimal -> "m"
                | Normal -> "n"
                | Detailed -> "d"
                | Diagnostic -> "diag"
            Some ("v", level)
    let allParameters = [targets; maxcpu; tools; verbosity] @ properties
    allParameters
    |> Seq.map (function
                    | None -> ""
                    | Some (k,v) -> "/" + k + (if isNullOrEmpty v then "" else ":" + v))
    |> separated " "

/// Runs a msbuild project
let build setParams project =
    traceStartTask "MSBuild" project
    let args = MSBuildDefaults |> setParams |> serializeMSBuildParams        
    let args = toParam project + " " + args
    logfn "Building project: %s\n  %s %s" project msBuildExe args
    if not (execProcess3 (fun info ->  
        info.FileName <- msBuildExe
        info.Arguments <- args) TimeSpan.MaxValue)
    then failwithf "Building %s project failed." project
    traceEndTask "MSBuild" project

/// Builds the given project files and collects the output files
let MSBuild outputPath (targets: string) (properties: (string*string) list) projects = 
    let projects = projects |> Seq.toList
    let output = 
        if isNullOrEmpty outputPath then "" else
        outputPath
          |> FullName
          |> trimSeparator

    let properties = if isNullOrEmpty output then properties else ("OutputPath", output)::properties

    let dependencies =
        projects 
            |> List.map getProjectReferences
            |> Set.unionMany

    let setBuildParam project = 
        { project with
            Targets = targets |> split ';' 
            Properties = project.Properties @ properties }

    projects
      |> List.filter (fun project -> not <| Set.contains project dependencies)
      |> List.iter (build setBuildParam)

    !! (outputPath + "/**/*.*")

/// Builds the given project files and collects the output files
let MSBuildDebug outputPath targets = MSBuild outputPath targets ["Configuration","Debug"]

/// Builds the given project files and collects the output files
let MSBuildRelease outputPath targets = MSBuild outputPath targets ["Configuration","Release"]