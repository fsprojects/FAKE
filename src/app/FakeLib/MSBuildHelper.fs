[<AutoOpen>]
/// Contains tasks which allow to use MSBuild (or xBuild on Linux/Unix) to build .NET project files or solution files.
module Fake.MSBuildHelper

open System
open System.IO
open System.Configuration
open System.Xml.Linq
open BuildServerHelper

/// A type to represent MSBuild project files.
type MSBuildProject = XDocument

/// An exception type to signal build errors.
exception BuildException of string*list<string>
  with
    override x.ToString() = x.Data0.ToString() + "\r\n" + (separated "\r\n" x.Data1)

/// Tries to detect the right version of MSBuild.
///   - On Linux/Unix Systems we use xBuild.
///   - On Windows we try to find a "MSBuild" build parameter or read the MSBuild tool location from the AppSettings file.
let msBuildExe =   
    if isUnix then "xbuild"
    else
        let MSBuildPath = 
            (ProgramFilesX86 @@ @"\MSBuild\14.0\Bin") + ";" +
            (ProgramFilesX86 @@ @"\MSBuild\12.0\Bin") + ";" +
            (ProgramFilesX86 @@ @"\MSBuild\12.0\Bin\amd64") + ";" + 
            @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\;" + 
            @"c:\Windows\Microsoft.NET\Framework\v4.0.30128\;" + 
            @"c:\Windows\Microsoft.NET\Framework\v3.5\"

        let ev = environVar "MSBuild"
        if not (isNullOrEmpty ev) then
            if isDirectory ev && Directory.Exists ev then ev @@ "MSBuild.exe" else ev
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

// See: http://msdn.microsoft.com/en-us/library/ms228186.aspx
let internal unescapeMSBuildSpecialChars s =
    let replExpr = new Text.RegularExpressions.Regex("%..")
    replExpr.Replace(s, new Text.RegularExpressions.MatchEvaluator(
                            fun _match -> match _match.Value with
                                          | "%24" -> "$" | "%25" -> "%" | "%27" -> "'" | "%40" -> "@"
                                          | "%3B" -> ";" | "%3F" -> "?" | "%2A" -> "*" 
                                          | _ -> _match.Value))

/// [omit]
let internal getReferenceElements elementName projectFileName (doc : XDocument) = 
    let fi = fileInfo projectFileName
    doc.Descendants(xname "Project").Descendants(xname "ItemGroup").Descendants(xname elementName) 
    |> Seq.map (fun e -> 
        let a = e.Attribute(XName.Get "Include")
        let value = a.Value |> unescapeMSBuildSpecialChars |> convertWindowsToCurrentPath 
           
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
    let references = getReferenceElements "ProjectReference" projectFileName doc |> Seq.map snd |> Seq.filter File.Exists
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

type MSBuildDistributedLoggerConfig = 
    {
        ClassName : string option
        AssemblyPath : string
        Parameters : (string * string) list option }

/// A type for MSBuild task parameters
type MSBuildParams = 
    { Targets : string list
      Properties : (string * string) list
      MaxCpuCount : int option option
      NoLogo : bool
      NodeReuse : bool
      RestorePackagesFlag : bool
      ToolsVersion : string option
      Verbosity : MSBuildVerbosity option
      NoConsoleLogger : bool
      FileLoggers : MSBuildFileLoggerConfig list option
      DistributedLoggers : (MSBuildDistributedLoggerConfig * MSBuildDistributedLoggerConfig option) list option }

/// Defines a default for MSBuild task parameters
let mutable MSBuildDefaults = 
    { Targets = []
      Properties = []
      MaxCpuCount = Some None
      NoLogo = false
      NodeReuse = not (buildServer = TeamCity || buildServer = TeamFoundation)
      ToolsVersion = None
      Verbosity = None
      NoConsoleLogger = false
      RestorePackagesFlag = false
      FileLoggers = None 
      DistributedLoggers = None }

/// [omit]
let getAllParameters targets maxcpu noLogo nodeReuse tools verbosity noconsolelogger fileLoggers distributedFileLoggers properties =
    if isUnix then [ targets; tools; verbosity; noconsolelogger ] @ fileLoggers @ distributedFileLoggers @ properties
    else [ targets; maxcpu; noLogo; nodeReuse; tools; verbosity; noconsolelogger ] @ fileLoggers @ distributedFileLoggers @ properties

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
        | t -> Some("t", t |> Seq.map (replace "." "_") |> separated ";")
    
    let properties = ("RestorePackages",p.RestorePackagesFlag.ToString()) :: p.Properties |> List.map (fun (k, v) -> Some("p", sprintf "%s=\"%s\"" k v))
    
    let maxcpu = 
        match p.MaxCpuCount with
        | None -> None
        | Some x -> 
            Some("m", 
                 match x with
                 | Some v -> v.ToString()
                 | _ -> "")
   
    let noLogo = 
        if p.NoLogo then Some("nologo", "")
        else None
    
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
    
    let noconsolelogger =
        if p.NoConsoleLogger then Some("noconlog", "")
        else None

    let fileLoggers =
        let serializeLogger fl =    
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

            sprintf "%s%s%s" 
                (match fl.Filename with
                | None -> ""
                | Some f -> sprintf "LogFile=%s;" f)
                (match fl.Verbosity with
                | None -> ""
                | Some v -> sprintf "Verbosity=%s;" (verbosityName v)) 
                (match fl.Parameters with
                | None -> ""
                | Some ps -> 
                    ps
                    |> List.map (fun p -> sprintf "%s;" (logParams p))
                    |> String.concat "")

        match p.FileLoggers with
        | None -> []
        | Some fls ->
            fls 
            |> List.map (fun fl -> Some ("flp" + (string fl.Number), serializeLogger fl) )

    let distributedFileLoggers = 
        let serializeDLogger (dlogger : MSBuildDistributedLoggerConfig) =
            sprintf "%s%s%s" 
                (match dlogger.ClassName with | None -> "" | Some name -> sprintf "%s," name) 
                (sprintf "\"%s\"" dlogger.AssemblyPath)
                (match dlogger.Parameters with 
                    | None -> "" 
                    | Some vars -> vars 
                                    |> List.fold (fun acc (k,v) -> sprintf "%s%s=%s;" acc k v) ""
                                    |> sprintf ";\"%s\""
                )

        let createLoggerString cl fl =
            match fl with
            | None -> serializeDLogger cl
            | Some l -> sprintf "%s*%s" (serializeDLogger cl) (serializeDLogger l)

        match p.DistributedLoggers with
        | None -> []
        | Some dfls ->
            dfls
            |> List.map(fun (cl, fl) -> Some("dl", createLoggerString cl fl))

    getAllParameters targets maxcpu noLogo nodeReuse tools verbosity noconsolelogger fileLoggers distributedFileLoggers properties
    |> serializeArgs

/// [omit]
let TeamCityLoggerName = typedefof<Fake.MsBuildLogger.TeamCityLogger>.FullName

/// [omit]
let ErrorLoggerName = typedefof<Fake.MsBuildLogger.ErrorLogger>.FullName

let private pathToLogger = typedefof<MSBuildParams>.Assembly.Location 

/// Defines the loggers to use for MSBuild task
let mutable MSBuildLoggers =    
    [ ErrorLoggerName ]
    |> List.map (fun a -> sprintf "%s,\"%s\"" a pathToLogger)

// Add MSBuildLogger to track build messages
match buildServer with
| BuildServer.AppVeyor ->
    MSBuildLoggers <- @"""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll""" :: MSBuildLoggers
| BuildServer.TeamCity -> MSBuildLoggers <- sprintf "%s,\"%s\"" TeamCityLoggerName pathToLogger :: MSBuildLoggers
| _ -> ()

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
///  - `projects` - A list of project or solution files.
let MSBuildWithProjectProperties outputPath (targets : string) (properties : (string) -> (string * string) list) projects = 
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
        { projectParams with Targets = targets |> split ';' |> List.filter ((<>) ""); Properties = projectParams.Properties @ properties project }

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
///  - `projects` - A list of project or solution files.
let MSBuild outputPath targets properties projects = MSBuildWithProjectProperties outputPath targets (fun _ -> properties) projects

/// Builds the given project files or solution files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `projects` - A list of project or solution files.
let MSBuildDebug outputPath targets projects = MSBuild outputPath targets [ "Configuration", "Debug" ] projects

/// Builds the given project files or solution files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `projects` - A list of project or solution files.
let MSBuildRelease outputPath targets projects = MSBuild outputPath targets [ "Configuration", "Release" ] projects

/// Builds the given project files or solution files in release mode to the default outputs.
/// ## Parameters
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `projects` - A list of project or solution files.
let MSBuildWithDefaults targets projects = MSBuild null targets [ "Configuration", "Release" ] projects

/// Builds the given project files or solution files in release mode and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `properties` - A list with tuples of property name and property values.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `projects` - A list of project or solution files.
let MSBuildReleaseExt outputPath properties targets projects = 
    let properties = ("Configuration", "Release") :: properties
    MSBuild outputPath targets properties projects

/// Builds the given web project file in the specified configuration and copies it to the given outputPath.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `configuration` - MSBuild configuration.
///  - `projectFile` - The project file path.
let BuildWebsiteConfig outputPath configuration projectFile  =
    traceStartTask "BuildWebsite" projectFile
    let projectName = (fileInfo projectFile).Name.Replace(".csproj", "").Replace(".fsproj", "").Replace(".vbproj", "")

    let slashes (dir : string) =
        dir.Replace("\\", "/").TrimEnd('/')
        |> Seq.filter ((=) '/')
        |> Seq.length

    let currentDir = (directoryInfo ".").FullName
    let projectDir = (fileInfo projectFile).Directory.FullName

    let diff = slashes projectDir - slashes currentDir
    let prefix = if Path.IsPathRooted outputPath
                 then ""
                 else (String.replicate diff "../")

    MSBuild null "Build" [ "Configuration", configuration ] [ projectFile ] |> ignore
    MSBuild null "_CopyWebApplication;_BuiltWebOutputGroupOutput"
        [ "Configuration", configuration
          "OutDir", prefix + outputPath
          "WebProjectOutputDir", prefix + outputPath + "/" + projectName ] [ projectFile ]
        |> ignore
    !!(projectDir + "/bin/*.*") |> Copy(outputPath + "/" + projectName + "/bin/")
    traceEndTask "BuildWebsite" projectFile

/// Builds the given web project file with debug configuration and copies it to the given outputPath.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `projectFile` - The project file path.
let BuildWebsite outputPath projectFile = BuildWebsiteConfig outputPath "Debug" projectFile

/// Builds the given web project files in specified configuration and copies them to the given outputPath.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `configuration` - MSBuild configuration.
///  - `projectFiles` - The project file paths.
let BuildWebsitesConfig outputPath configuration projectFiles = Seq.iter (BuildWebsiteConfig outputPath configuration) projectFiles

/// Builds the given web project files with debug configuration and copies them to the given websiteDir.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `projectFiles` - The project file paths.
let BuildWebsites outputPath projectFiles = BuildWebsitesConfig outputPath "Debug" projectFiles