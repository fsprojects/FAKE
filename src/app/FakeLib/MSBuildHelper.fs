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
    if isUnix then
        "xbuild"
    else
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
            let value = convertWindowsToCurrentPath a.Value
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

let rec getProjectReferences (projectFileName:string) =
    if projectFileName.EndsWith ".sln" then Set.empty else // exclude .sln-files since the are not XML
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
type MSBuildLogParameter = Append | PerformanceSummary | Summary | NoSummary | ErrorsOnly | WarningsOnly | NoItemAndPropertyList | ShowCommandLine | ShowTimestamp | ShowEventId | ForceNoAlign  | DisableConsoleColor | DisableMPLogging | EnableMPLogging

type MSBuildFileLoggerConfig =
    { Number : int
      Filename : string option
      Verbosity : MSBuildVerbosity option
      Parameters : MSBuildLogParameter list option }

type MSBuildParams = 
    { Targets: string list
      Properties: (string * string) list
      MaxCpuCount: int option option
      ToolsVersion: string option
      Verbosity: MSBuildVerbosity option
      FileLoggers: MSBuildFileLoggerConfig list option }

let MSBuildDefaults = 
    { Targets = []
      Properties = []
      MaxCpuCount = Some None
      ToolsVersion = None
      Verbosity = None
      FileLoggers = None }

let getAllParameters targets maxcpu tools verbosity fileLoggers properties =
    if isUnix then
        [targets; tools; verbosity] @ fileLoggers @ properties
    else
        [targets; maxcpu; tools; verbosity] @ fileLoggers @ properties

let serializeMSBuildParams (p: MSBuildParams) = 
    let verbosityName v =
        match v with
        | Quiet -> "q"
        | Minimal -> "m"
        | Normal -> "n"
        | Detailed -> "d"
        | Diagnostic -> "diag"
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
            Some ("v", verbosityName v)
    let fileLoggers =
        let logParams param =
            match param with
            | Append -> "Append"
            | PerformanceSummary -> "PerformanceSummary"
            | Summary -> "Summary"
            | NoSummary -> "NoSummary"
            | ErrorsOnly -> "ErrorsOnly"
            | WarningsOnly -> "WarningsOnly"
            | NoItemAndPropertyList -> "NoItemAndPropertyList"
            | ShowCommandLine -> "ShowCommandLine"
            | ShowTimestamp -> "ShowTimestamp"
            | ShowEventId -> "ShowEventId"
            | ForceNoAlign -> "ForceNoAlign"
            | DisableConsoleColor -> "DisableConsoleColor"
            | DisableMPLogging -> "DisableMPLogging"
            | EnableMPLogging -> "EnableMPLogging"
        match p.FileLoggers with
        | None -> []
        | Some fls ->
            fls |> List.map (fun fl -> 
                    Some("flp" + (string fl.Number), 
                        sprintf "%s%s%s"
                            (match fl.Filename with | None -> "" | Some f -> sprintf "logfile=%s;" f)
                            (match fl.Verbosity with | None -> "" | Some v -> sprintf "Verbosity=%s;" (verbosityName v))
                            (match fl.Parameters with | None -> "" | Some ps -> ps |> List.map (fun p -> logParams p |> sprintf "%s;") |> String.concat "")))
    let allParameters = getAllParameters targets maxcpu tools verbosity fileLoggers properties
    allParameters
    |> Seq.map (function
                    | None -> ""
                    | Some (k,v) -> "/" + k + (if isNullOrEmpty v then "" else ":" + v))
    |> separated " "

let TeamCityLoggerName = typedefof<Fake.MsBuildLogger.TeamCityLogger>.FullName
let ErrorLoggerName = typedefof<Fake.MsBuildLogger.ErrorLogger>.FullName

let private errorLoggerParam = 
    let pathToLogger = (Uri(typedefof<MSBuildParams>.Assembly.CodeBase)).LocalPath
    [ TeamCityLoggerName; ErrorLoggerName ]
    |> List.map(fun a -> sprintf "/logger:%s,\"%s\"" a pathToLogger)
    |> fun lst -> String.Join(" ", lst)

/// Runs a msbuild project
let build setParams project =
    traceStartTask "MSBuild" project
    let args = MSBuildDefaults |> setParams |> serializeMSBuildParams        
    let args = toParam project + " " + args + " " + errorLoggerParam
    tracefn "Building project: %s\n  %s %s" project msBuildExe args
    if not (execProcess3 (fun info ->  
        info.FileName <- msBuildExe
        info.Arguments <- args) TimeSpan.MaxValue)
    then
        if Diagnostics.Debugger.IsAttached then Diagnostics.Debugger.Break()
        let errors = File.ReadAllLines(MsBuildLogger.ErrorLoggerFile) |> List.ofArray
        let errorMessage = sprintf "Building %s project failed." project
        raise (BuildException(errorMessage, errors))
    traceEndTask "MSBuild" project

/// Builds the given project files and collects the output files.
/// Properties are parameterized by project name.
/// If the outputpath is null or empty then the project settings are used.>
let MSBuildWithProjectProperties outputPath (targets: string) (properties: string -> (string*string) list) projects = 
    let projects = projects |> Seq.toList
    let output = 
        if isNullOrEmpty outputPath then "" else
        outputPath
          |> FullName
          |> trimSeparator

    let properties = 
        if isNullOrEmpty output 
            then properties 
            else fun x -> ("OutputPath", output)::(properties x)

    let dependencies =
        projects 
            |> List.map getProjectReferences
            |> Set.unionMany

    let setBuildParam project projectParams = 
        { projectParams with
            Targets = targets |> split ';' 
            Properties = projectParams.Properties @ properties project }

    projects
      |> List.filter (fun project -> not <| Set.contains project dependencies)
      |> List.iter (fun project -> build (setBuildParam project) project)

    !! (outputPath + "/**/*.*")

/// Builds the given project files or solution files and collects the output files
/// If the outputpath is null or empty then the project settings are used.
let MSBuild outputPath targets properties = MSBuildWithProjectProperties outputPath targets (fun _ -> properties)

/// Builds the given project files or solution files and collects the output files
/// If the outputpath is null or empty then the project settings are used.
let MSBuildDebug outputPath targets = MSBuild outputPath targets ["Configuration","Debug"]

/// Builds the given project files or solution files and collects the output files
/// If the outputpath is null or empty then the project settings are used.
let MSBuildRelease outputPath targets = MSBuild outputPath targets ["Configuration","Release"]

/// Builds the given project files or solution files in release mode to the default outputs.
let MSBuildWithDefaults targets = MSBuild null targets ["Configuration","Release"]
