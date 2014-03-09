[<AutoOpen>]
/// Contains tasks which allow to use MSBuild (or xBuild on Linux/Unix) to build .NET project files or solution files.
module Fake.MSBuildHelper

open System
open System.Text
open System.IO
open System.Configuration
open System.Xml
open System.Xml.Linq

/// An type to represent MSBuild project files.
type MSBuildProject = XDocument

/// An exception type to signal build errors.
exception BuildException of string*list<string>
  with
    override x.ToString() = x.Data0.ToString() + "\r\n" + (separated "\r\n" x.Data1)

let private MSBuildPath = 
    @"[ProgramFilesX86]\MSBuild\12.0\bin\;[ProgramFilesX86]\MSBuild\12.0\bin\amd64\;c:\Windows\Microsoft.NET\Framework\v4.0.30319\;c:\Windows\Microsoft.NET\Framework\v4.0.30128\;c:\Windows\Microsoft.NET\Framework\v3.5\"

/// Tries to detect the right version of MSBuild.
///   - On Linux/Unix Systems we use xBuild.
///   - On Windows we try to find a "MSBuild" build parameter or read the MSBuild tool location from the AppSettings file.
let msBuildExe = 
    if isUnix then "xbuild"
    else 
        let ev = environVar "MSBuild"
        if not (isNullOrEmpty ev) then ev
        else if "true".Equals(ConfigurationManager.AppSettings.["IgnoreMSBuild"], StringComparison.OrdinalIgnoreCase) then 
            String.Empty
        else findPath "MSBuildPath" MSBuildPath "MSBuild.exe"

/// [omit]
let msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003"

/// [omit]
let xname name = XName.Get(name, msbuildNamespace)

/// [omit]
let loadProject (projectFileName : string) : MSBuildProject = 
    MSBuildProject.Load(projectFileName, LoadOptions.PreserveWhitespace)

/// [omit]
let internal getReferenceElements elementName projectFileName (doc : XDocument) = 
    let fi = fileInfo projectFileName
    doc.Descendants(xname "Project").Descendants(xname "ItemGroup").Descendants(xname elementName) 
    |> Seq.map (fun e -> 
           let a = e.Attribute(XName.Get "Include")
           let value = convertWindowsToCurrentPath a.Value
           
           let fileName = 
               if value.StartsWith(".." + directorySeparator) || (not <| value.Contains directorySeparator) then 
                   fi.Directory.FullName @@ value
               else value
           a, fileName |> FullName)

/// [omit]
let processReferences elementName f projectFileName (doc : XDocument) = 
    let fi = fileInfo projectFileName
    doc
    |> getReferenceElements elementName projectFileName
    |> Seq.iter (fun (a, fileName) -> a.Value <- f fileName)
    doc

/// [omit]
let rec getProjectReferences (projectFileName : string) = 
    if projectFileName.EndsWith ".sln" then Set.empty
    else // exclude .sln-files since the are not XML
         
        let doc = loadProject projectFileName
        let references = getReferenceElements "ProjectReference" projectFileName doc |> Seq.map snd
        references
        |> Seq.map getProjectReferences
        |> Seq.concat
        |> Seq.append references
        |> Set.ofSeq

/// MSBuild verbosity option
type MSBuildVerbosity = 
    | Quiet
    | Minimal
    | Normal
    | Detailed
    | Diagnostic

/// MSBuild log option
type MSBuildLogParameter = 
    | Append
    | PerformanceSummary
    | Summary
    | NoSummary
    | ErrorsOnly
    | WarningsOnly
    | NoItemAndPropertyList
    | ShowCommandLine
    | ShowTimestamp
    | ShowEventId
    | ForceNoAlign
    | DisableConsoleColor
    | DisableMPLogging
    | EnableMPLogging

/// A type for MSBuild configuration
type MSBuildFileLoggerConfig = 
    { Number : int
      Filename : string option
      Verbosity : MSBuildVerbosity option
      Parameters : MSBuildLogParameter list option }

/// A type for MSBuild task parameters
type MSBuildParams = 
    { Targets : string list
      Properties : (string * string) list
      MaxCpuCount : int option option
      NodeReuse : bool
      ToolsVersion : string option
      Verbosity : MSBuildVerbosity option
      FileLoggers : MSBuildFileLoggerConfig list option }

/// Defines a default for MSBuild task parameters
let mutable MSBuildDefaults = 
    { Targets = []
      Properties = []
      MaxCpuCount = Some None
      NodeReuse = true
      ToolsVersion = None
      Verbosity = None
      FileLoggers = None }

/// [omit]
let getAllParameters targets maxcpu nodeReuse tools verbosity fileLoggers properties = 
    if isUnix then [ targets; tools; verbosity ] @ fileLoggers @ properties
    else [ targets; maxcpu; nodeReuse; tools; verbosity ] @ fileLoggers @ properties

let private serializeArgs args =
    args
    |> Seq.map (function 
           | None -> ""
           | Some(k, v) -> 
               "/" + k + (if isNullOrEmpty v then ""
                          else ":" + v))
    |> separated " "

/// [omit]
let serializeMSBuildParams (p : MSBuildParams) = 
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
        | t -> Some("t", t |> separated ";")
    
    let properties = p.Properties |> List.map (fun (k, v) -> Some("p", sprintf "%s=\"%s\"" k v))
    
    let maxcpu = 
        match p.MaxCpuCount with
        | None -> None
        | Some x -> 
            Some("m", 
                 match x with
                 | Some v -> v.ToString()
                 | _ -> "")
    
    let nodeReuse = 
        if p.NodeReuse then None
        else Some("nodeReuse", "False")
    
    let tools = 
        match p.ToolsVersion with
        | None -> None
        | Some t -> Some("tv", t)
    
    let verbosity = 
        match p.Verbosity with
        | None -> None
        | Some v -> Some("v", verbosityName v)
    
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
            fls 
            |> List.map 
                   (fun fl -> 
                   Some
                       ("flp" + (string fl.Number), 
                        
                        sprintf "%s%s%s" (match fl.Filename with
                                          | None -> ""
                                          | Some f -> sprintf "logfile=%s;" f) 
                            (match fl.Verbosity with
                             | None -> ""
                             | Some v -> sprintf "Verbosity=%s;" (verbosityName v)) (match fl.Parameters with
                                                                                     | None -> ""
                                                                                     | Some ps -> 
                                                                                         ps
                                                                                         |> List.map 
                                                                                                (fun p -> 
                                                                                                logParams p 
                                                                                                |> sprintf "%s;")
                                                                                         |> String.concat "")))
    
    getAllParameters targets maxcpu nodeReuse tools verbosity fileLoggers properties
    |> serializeArgs

/// [omit]
let TeamCityLoggerName = typedefof<Fake.MsBuildLogger.TeamCityLogger>.FullName

/// [omit]
let ErrorLoggerName = typedefof<Fake.MsBuildLogger.ErrorLogger>.FullName

/// Defines the loggers to use for MSBuild task
let mutable MSBuildLoggers =
    let pathToLogger = typedefof<MSBuildParams>.Assembly.Location
    [ TeamCityLoggerName; ErrorLoggerName ]
    |> List.map (fun a -> sprintf "%s,\"%s\"" a pathToLogger)
    
/// Runs a MSBuild project
/// ## Parameters
///  - `setParams` - A function that overwrites the default MsBuildParams
///  - `project` - A string with the path to the project file to build.
///
/// ## Sample
///
///     let buildMode = getBuildParamOrDefault "buildMode" "Release"
///     let setParams defaults =
///             { defaults with
///                 Verbosity = Some(Quiet)
///                 Targets = ["Build"]
///                 Properties =
///                     [
///                         "Optimize", "True"
///                         "DebugSymbols", "True"
///                         "Configuration", buildMode
///                     ]
///              }
///     build setParams "./MySolution.sln"
///           |> DoNothing
let build setParams project = 
    traceStartTask "MSBuild" project
    let args = 
        MSBuildDefaults
        |> setParams
        |> serializeMSBuildParams

    let errorLoggerParam = 
        MSBuildLoggers
        |> List.map (fun a -> Some ("logger", a))
        |> serializeArgs
    
    let args = toParam project + " " + args + " " + errorLoggerParam
    tracefn "Building project: %s\n  %s %s" project msBuildExe args
    let exitCode = 
        ExecProcess (fun info -> 
            info.FileName <- msBuildExe
            info.Arguments <- args) TimeSpan.MaxValue
    if exitCode <> 0 then 
        if Diagnostics.Debugger.IsAttached then Diagnostics.Debugger.Break()
        let errors = 
            System.Threading.Thread.Sleep(200) // wait for the file to write
            if File.Exists MsBuildLogger.ErrorLoggerFile then 
                File.ReadAllLines(MsBuildLogger.ErrorLoggerFile) |> List.ofArray
            else []
        
        let errorMessage = sprintf "Building %s failed with exitcode %d." project exitCode
        raise (BuildException(errorMessage, errors))
    traceEndTask "MSBuild" project

/// Builds the given project files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `properties` - A list with tuples of property name and property values.
let MSBuildWithProjectProperties outputPath (targets : string) (properties : string -> (string * string) list) projects = 
    let projects = projects |> Seq.toList
    
    let output = 
        if isNullOrEmpty outputPath then ""
        else 
            outputPath
            |> FullName
            |> trimSeparator
    
    let properties = 
        if isNullOrEmpty output then properties
        else fun x -> ("OutputPath", output) :: (properties x)
    
    let dependencies = 
        projects
        |> List.map getProjectReferences
        |> Set.unionMany
    
    let setBuildParam project projectParams = 
        { projectParams with Targets = targets |> split ';'
                             Properties = projectParams.Properties @ properties project }
    
    projects
    |> List.filter (fun project -> not <| Set.contains project dependencies)
    |> List.iter (fun project -> build (setBuildParam project) project)
    // it makes no sense to output the root dir content here since it does not contain the build output
    if isNotNullOrEmpty output then !!(outputPath @@ "/**/*.*") |> Seq.toList
    else []

/// Builds the given project files or solution files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `properties` - A list with tuples of property name and property values.
let MSBuild outputPath targets properties = MSBuildWithProjectProperties outputPath targets (fun _ -> properties)

/// Builds the given project files or solution files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
let MSBuildDebug outputPath targets = MSBuild outputPath targets [ "Configuration", "Debug" ]

/// Builds the given project files or solution files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
let MSBuildRelease outputPath targets = MSBuild outputPath targets [ "Configuration", "Release" ]

/// Builds the given project files or solution files in release mode to the default outputs.
/// ## Parameters
///  - `targets` - A string with the target names which should be run by MSBuild.
let MSBuildWithDefaults targets = MSBuild null targets [ "Configuration", "Release" ]

/// Builds the given project files or solution files in release mode and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `properties` - A list with tuples of property name and property values.
///  - `targets` - A string with the target names which should be run by MSBuild.
let MSBuildReleaseExt outputPath properties targets = 
    let properties = ("Configuration", "Release") :: properties
    MSBuild outputPath targets properties

/// Builds the given web project file in debug mode and copies it to the given websiteDir.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `projectFile` - The project file path.
let BuildWebsite outputPath projectFile = 
    traceStartTask "BuildWebsite" projectFile
    let projectName = (fileInfo projectFile).Name.Replace(".csproj", "").Replace(".fsproj", "").Replace(".vbproj", "")
    
    let slashes (dir : string) = 
        dir.Replace("\\", "/").TrimEnd('/')
        |> Seq.filter ((=) '/')
        |> Seq.length
    
    let currentDir = (directoryInfo ".").FullName
    let projectDir = (fileInfo projectFile).Directory.FullName
    let mutable prefix = ""
    let diff = slashes projectDir - slashes currentDir
    for i in 1..diff do
        prefix <- prefix + "../"
    MSBuildDebug "" "Rebuild" [ projectFile ] |> ignore
    MSBuild "" "_CopyWebApplication;_BuiltWebOutputGroupOutput" 
        [ "OutDir", prefix + outputPath
          "WebProjectOutputDir", prefix + outputPath + "/" + projectName ] [ projectFile ]
    |> ignore
    !!(projectDir + "/bin/*.*") |> Copy(outputPath + "/" + projectName + "/bin/")
    traceEndTask "BuildWebsite" projectFile

/// Builds the given web project files in debug mode and copies them to the given websiteDir.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `projectFiles` - The project file paths.
let BuildWebsites websiteDir projectFiles = Seq.iter (BuildWebsite websiteDir) projectFiles
