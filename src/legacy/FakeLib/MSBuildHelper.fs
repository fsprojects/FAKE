[<AutoOpen>]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
/// Contains tasks which allow to use MSBuild (or xBuild on Linux/Unix) to build .NET project files or solution files.
module Fake.MSBuildHelper

open System
open System.IO
open System.Configuration
open System.Xml.Linq
open BuildServerHelper

/// A type to represent MSBuild project files.
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
type MSBuildProject = XDocument

/// An exception type to signal build errors.
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
exception BuildException of string*list<string>
  with
    override x.ToString() = x.Data0.ToString() + "\r\n" + (separated "\r\n" x.Data1)

[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
type MsBuildEntry = {
    Version: string;
    Paths: string list;
}

[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let knownMsBuildEntries =
    [
        { Version = "16.0"; Paths = [@"\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin"
                                     @"\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin"
                                     @"\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin"
                                     @"\MSBuild\Current\Bin"
                                     @"\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin"] }
        { Version = "15.0"; Paths = [@"\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"
                                     @"\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin"
                                     @"\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin"
                                     @"\MSBuild\15.0\Bin"
                                     @"\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"] }
        { Version = "14.0"; Paths = [@"\MSBuild\14.0\Bin"] }
        { Version = "12.0"; Paths = [@"\MSBuild\12.0\Bin"; @"\MSBuild\12.0\Bin\amd64"] }
    ]

[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let oldMsBuildLocations =
    [ @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\";
      @"c:\Windows\Microsoft.NET\Framework\v4.0.30128\";
      @"c:\Windows\Microsoft.NET\Framework\v3.5\"
    ]

[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let toDict items =
    items |> Seq.map (fun f -> f.Version, f.Paths) |> Map.ofSeq

[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let getAllKnownPaths =
    (knownMsBuildEntries |> List.collect (fun m -> m.Paths)) @ oldMsBuildLocations

/// Versions of Mono prior to this one have faulty implementations of MSBuild
/// NOTE: in System.Version 5.0 >= 5.0.0.0 is false while 5.0.0.0 >= 5.0 is true...
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let monoVersionToUseMSBuildOn = System.Version("5.0")

/// Tries to detect the right version of MSBuild.
///   - On all OS's, we check a `MSBuild` environment variable which is either
///     * a direct path to a file to use, or
///     * a directory that contains a file called
///         * `msbuild` on non-Windows systems with mono >= 5.0.0.0, or
///         * `xbuild` on non-Windows systems with mono < 5.0.0.0,
///         * `MSBuild.exe` on Windows systems, or
///     * a tool that exists on the current PATH
///   - In addition, on non-Windows systems we check the current PATH for the following binaries, in this order:
///     * Mono >= 5.0.0.0: `msbuild`, `xbuild`
///     * Mono < 5.0.0.0: `xbuild`, `msbuild`
///     * This is due to several known issues in the Mono < 5.0 implementation of MSBuild.
///   - In addition, on Windows systems we
///     * try to read the MSBuild tool location from the AppSettings file using a parameter named `MSBuild`, and finally
///     * if a `VisualStudioVersion` environment variable is specified, we try to use the specific MSBuild version, matching that Visual Studio version.
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let msBuildExe =
    /// the value we're given can be a:
    ///     * full path to a file or
    ///     * just a directory
    /// if just a directory we can make it the path to a file by Path-Combining the tool name to the directory.
    let exactPathOrBinaryOnPath tool input =
        if FileSystemHelper.isDirectory input && Directory.Exists input
        then input </> tool
        else input

    let which tool = ProcessHelper.tryFindFileOnPath tool
    let msbuildEnvironVar = EnvironmentHelper.environVarOrNone "MSBuild"

    let foundExe =
        match isUnix, EnvironmentHelper.monoVersion with
        | true, Some(_, Some(version)) when version >= monoVersionToUseMSBuildOn ->
            let sources = [
                msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "msbuild")
                msbuildEnvironVar |> Option.bind which
                which "msbuild"
                which "xbuild"
            ]
            defaultArg (sources |> List.choose id |> List.tryHead) "msbuild"
        | true, _ ->
            let sources = [
                msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "xbuild")
                msbuildEnvironVar |> Option.bind which
                which "xbuild"
                which "msbuild"
            ]
            defaultArg (sources |> List.choose id |> List.tryHead) "xbuild"
        | false, _ ->

            let configIgnoreMSBuild =
                if "true".Equals(ConfigurationManager.AppSettings.["IgnoreMSBuild"], StringComparison.OrdinalIgnoreCase)
                then Some ""
                else None
            let findOnVSPathsThenSystemPath =
                let dict = toDict knownMsBuildEntries
                let vsVersionPaths =
                    defaultArg (EnvironmentHelper.environVarOrNone "VisualStudioVersion" |> Option.bind dict.TryFind) getAllKnownPaths
                    |> List.map ((@@) ProgramFilesX86)

                ProcessHelper.tryFindFileInDirsThenPath vsVersionPaths "MSBuild.exe"

            let sources = [
                msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "MSBuild.exe")
                msbuildEnvironVar |> Option.bind which
                configIgnoreMSBuild
                findOnVSPathsThenSystemPath
            ]
            defaultArg (sources |> List.choose id |> List.tryHead) "MSBuild.exe"

    if foundExe.Contains @"\BuildTools\" then
        traceFAKE "If you encounter msbuild errors make sure you have copied the required SDKs, see https://github.com/Microsoft/msbuild/issues/1697"
    elif foundExe.Contains @"\2017\" then
        logVerbosefn "Using msbuild of VS2017 (%s), if you encounter build errors make sure you have installed the necessary workflows!" foundExe
    elif foundExe.Contains @"\2019\" then
        logVerbosefn "Using msbuild of VS2019 (%s), if you encounter build errors make sure you have installed the necessary workflows!" foundExe        
    foundExe

/// [omit]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003"

/// [omit]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let xname name = XName.Get(name, msbuildNamespace)

/// [omit]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
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
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let processReferences elementName f projectFileName (doc : XDocument) =
    let fi = fileInfo projectFileName
    doc
        |> getReferenceElements elementName projectFileName
    |> Seq.iter (fun (a, fileName) -> a.Value <- f fileName)
    doc

/// [omit]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
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
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
type MSBuildVerbosity =
    | Quiet
    | Minimal
    | Normal
    | Detailed
    | Diagnostic

/// MSBuild log option
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
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
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
type MSBuildFileLoggerConfig =
    { Number : int
      Filename : string option
      Verbosity : MSBuildVerbosity option
      Parameters : MSBuildLogParameter list option }

[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
type MSBuildDistributedLoggerConfig =
    {
        ClassName : string option
        AssemblyPath : string
        Parameters : (string * string) list option }

/// A type for MSBuild task parameters
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
[<CLIMutable>]
type MSBuildParams =
    {
      /// Set the MSBuild executable to use. Defaults to the latest installed MSBuild.
      ToolPath : string
      Targets : string list
      Properties : (string * string) list
      /// corresponds to the msbuild option '/m':
      ///  - 'None' will omit the option.
      ///  - 'Some None' will emit '/m'.
      ///  - 'Some 2' will emit '/m:2'.
      MaxCpuCount : int option option
      NoLogo : bool
      NodeReuse : bool
      RestorePackagesFlag : bool
      ToolsVersion : string option
      Verbosity : MSBuildVerbosity option
      NoConsoleLogger : bool
      WarnAsError: string list option
      NoWarn: string list option
      /// corresponds to the msbuild option '/fl'
      FileLoggers : MSBuildFileLoggerConfig list option
      /// corresponds to the msbuild option '/bl'
      BinaryLoggers : string list option
      /// corresponds to the msbuild option '/dl'
      DistributedLoggers : (MSBuildDistributedLoggerConfig * MSBuildDistributedLoggerConfig option) list option }

/// Defines a default for MSBuild task parameters
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let mutable MSBuildDefaults =
    { ToolPath = msBuildExe
      Targets = []
      Properties = []
      MaxCpuCount = Some None
      NoLogo = false
      NodeReuse = false
      ToolsVersion = None
      Verbosity = None
      NoConsoleLogger = false
      WarnAsError = None
      NoWarn = None
      RestorePackagesFlag = false
      FileLoggers = None
      BinaryLoggers = None
      DistributedLoggers = None }

/// [omit]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let getAllParameters targets maxcpu noLogo nodeReuse tools verbosity noconsolelogger warnAsError nowarn fileLoggers binaryLoggers distributedFileLoggers properties =
    if isUnix then [ targets; tools; verbosity; noconsolelogger; warnAsError; nowarn ] @ fileLoggers @ binaryLoggers @ distributedFileLoggers @ properties
    else [ targets; maxcpu; noLogo; nodeReuse; tools; verbosity; noconsolelogger; warnAsError; nowarn ] @ fileLoggers @ binaryLoggers @ distributedFileLoggers @ properties

let private serializeArgs args =
    args
    |> Seq.map (function
           | None -> ""
           | Some(k, v) ->
               "/" + k + (if isNullOrEmpty v then ""
                          else ":" + v))
    |> separated " "

/// [omit]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
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

    let warnAsError =
        match p.WarnAsError with
        | None -> None
        | Some w -> Some("warnaserror", w |> String.concat ";")

    let nowarn =
        match p.NoWarn with
        | None -> None
        | Some w -> Some("nowarn", w |> String.concat ";")

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

    let binaryLoggers =
        match p.BinaryLoggers with
        | None -> []
        | Some bls ->
            bls
            |> List.map (fun bl -> Some ("bl", bl) )

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

    getAllParameters targets maxcpu noLogo nodeReuse tools verbosity noconsolelogger warnAsError nowarn fileLoggers binaryLoggers distributedFileLoggers properties
    |> serializeArgs

/// [omit]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let TeamCityLoggerName = typedefof<Fake.MsBuildLogger.TeamCityLogger>.FullName

/// [omit]
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let ErrorLoggerName = typedefof<Fake.MsBuildLogger.ErrorLogger>.FullName

let private pathToLogger = typedefof<MSBuildParams>.Assembly.Location

/// Defines the loggers to use for MSBuild task
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
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
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let build setParams project =
    use __ = traceStartTaskUsing "MSBuild" project
    let msBuildParams =
        MSBuildDefaults
        |> setParams

    let argsString = msBuildParams |> serializeMSBuildParams

    let errorLoggerParam =
        MSBuildLoggers
        |> List.map (fun a -> Some ("logger", a))
        |> serializeArgs

    let args = toParam project + " " + argsString + " " + errorLoggerParam
    tracefn "Building project: %s\n  %s %s" project msBuildParams.ToolPath args
    let enableProcessTracingPreviousValue = enableProcessTracing
    enableProcessTracing <- false
    let exitCode =
        ExecProcess (fun info ->
            info.FileName <- msBuildParams.ToolPath
            info.Arguments <- args) TimeSpan.MaxValue
    enableProcessTracing <- enableProcessTracingPreviousValue
    if exitCode <> 0 then
        let errors =
            System.Threading.Thread.Sleep(200) // wait for the file to write
            if File.Exists MsBuildLogger.ErrorLoggerFile then
                File.ReadAllLines(MsBuildLogger.ErrorLoggerFile) |> List.ofArray
            else []

        let errorMessage = sprintf "Building %s failed with exitcode %d." project exitCode
        raise (BuildException(errorMessage, errors))

/// Builds the given project files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `properties` - A list with tuples of property name and property values.
///  - `projects` - A list of project or solution files.
[<System.Obsolete("Use Fake.DotNet.MSBuild.RunWithProjectProperties instead")>]
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
        { projectParams with
            Targets = targets |> split ';' |> List.filter ((<>) "")
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
///  - `projects` - A list of project or solution files.
[<System.Obsolete("Use Fake.DotNet.MSBuild.Run instead")>]
let MSBuild outputPath targets properties projects = MSBuildWithProjectProperties outputPath targets (fun _ -> properties) projects

/// Builds the given project files or solution files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `projects` - A list of project or solution files.
[<System.Obsolete("Use Fake.DotNet.MSBuild.RunDebug instead")>]
let MSBuildDebug outputPath targets projects = MSBuild outputPath targets [ "Configuration", "Debug" ] projects

/// Builds the given project files or solution files and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `projects` - A list of project or solution files.
[<System.Obsolete("Use Fake.DotNet.MSBuild.RunRelease instead")>]
let MSBuildRelease outputPath targets projects = MSBuild outputPath targets [ "Configuration", "Release" ] projects

/// Builds the given project files or solution files in release mode to the default outputs.
/// ## Parameters
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `projects` - A list of project or solution files.
[<System.Obsolete("Use Fake.DotNet.MSBuild.RunWithDefaults instead")>]
let MSBuildWithDefaults targets projects = MSBuild null targets [ "Configuration", "Release" ] projects

/// Builds the given project files or solution files in release mode and collects the output files.
/// ## Parameters
///  - `outputPath` - If it is null or empty then the project settings are used.
///  - `properties` - A list with tuples of property name and property values.
///  - `targets` - A string with the target names which should be run by MSBuild.
///  - `projects` - A list of project or solution files.
[<System.Obsolete("Use Fake.DotNet.MSBuild.RunReleaseExt instead")>]
let MSBuildReleaseExt outputPath properties targets projects =
    let properties = ("Configuration", "Release") :: properties
    MSBuild outputPath targets properties projects

/// Builds the given web project file in the specified configuration and copies it to the given outputPath.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `configuration` - MSBuild configuration.
///  - `projectFile` - The project file path.
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let BuildWebsiteConfig outputPath configuration projectFile  =
    use __ = traceStartTaskUsing "BuildWebsite" projectFile
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

/// Builds the given web project file with debug configuration and copies it to the given outputPath.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `projectFile` - The project file path.
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let BuildWebsite outputPath projectFile = BuildWebsiteConfig outputPath "Debug" projectFile

/// Builds the given web project files in specified configuration and copies them to the given outputPath.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `configuration` - MSBuild configuration.
///  - `projectFiles` - The project file paths.
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let BuildWebsitesConfig outputPath configuration projectFiles = Seq.iter (BuildWebsiteConfig outputPath configuration) projectFiles

/// Builds the given web project files with debug configuration and copies them to the given websiteDir.
/// ## Parameters
///  - `outputPath` - The output path.
///  - `projectFiles` - The project file paths.
[<System.Obsolete("Use Fake.DotNet.MSBuild instead")>]
let BuildWebsites outputPath projectFiles = BuildWebsitesConfig outputPath "Debug" projectFiles
