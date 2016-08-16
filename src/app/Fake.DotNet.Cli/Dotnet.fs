// From https://raw.githubusercontent.com/dolly22/FAKE.Dotnet/master/src/Fake.Dotnet/Dotnet.fs
// Temporary copied into Fake until the dotnetcore conversation has finished.
// This probably needs to stay within Fake to bootstrap?
// Currently last file in FakeLib, until all dependencies are available in dotnetcore.
/// .NET Core + CLI tools helpers
module Fake.DotNet.Cli

open Fake.Core
open Fake.IO.FileSystem
open Fake.IO.FileSystem.Operators
open System
open System.IO
open System.Security.Cryptography
open System.Text

/// .NET Core SDK default install directory (set to default localappdata dotnet dir). Update this to redirect all tool commands to different location. 
let mutable DefaultDotnetCliDir = Environment.environVar "LocalAppData" @@ "Microsoft" @@ "dotnet"

/// Get dotnet cli executable path
/// ## Parameters
///
/// - 'dotnetCliDir' - dotnet cli install directory 
let private dotnetCliPath dotnetCliDir = dotnetCliDir @@ "dotnet.exe"

/// Get .NET Core SDK download uri
let private getDotnetCliInstallerUrl branch = sprintf "https://raw.githubusercontent.com/dotnet/cli/%s/scripts/obtain/dotnet-install.ps1" branch

/// Download .NET Core SDK installer
let private downloadDotnetInstaller branch fileName =  
    let url = getDotnetCliInstallerUrl branch
#if USE_HTTPCLIENT
    let h = new System.Net.Http.HttpClient();
    use f = File.Open(fileName, FileMode.Create);
    h.GetStreamAsync(url).Result.CopyTo(f);
#else
    use w = new System.Net.WebClient()
    w.DownloadFile(url, fileName) // Http.RequestStream url
#endif
    //use outFile = File.Open(fileName, FileMode.Create)
    //installScript.ResponseStream.CopyTo(outFile)
    Trace.trace (sprintf "downloaded dotnet installer (%s) to %s" url fileName)

/// [omit]
let private md5 (data : byte array) : string =
    use md5 = MD5.Create()
    (StringBuilder(), md5.ComputeHash(data))
    ||> Array.fold (fun sb b -> sb.Append(b.ToString("x2")))
    |> string


/// .NET Core SDK installer download options
type DotNetInstallerOptions =
    {   
        /// Always download install script (otherwise install script is cached in temporary folder)
        AlwaysDownload: bool;
        /// Download installer from this github branch
        Branch: string;
    }

    /// Parameter default values.
    static member Default = {
        AlwaysDownload = false
        Branch = "rel/1.0.0"
    }

/// Download .NET Core SDK installer
/// ## Parameters
///
/// - 'setParams' - set download installer options
let DotnetDownloadInstaller setParams =
    let param = DotNetInstallerOptions.Default |> setParams

    let scriptName = sprintf "dotnet_install_%s.ps1" <| md5 (Encoding.ASCII.GetBytes(param.Branch))
    let tempInstallerScript = Path.GetTempPath() @@ scriptName

    // maybe download installer script
    match param.AlwaysDownload || not(File.Exists(tempInstallerScript)) with
        | true -> downloadDotnetInstaller param.Branch tempInstallerScript 
        | _ -> ()

    tempInstallerScript


/// .NET Core SDK architecture
type DotnetCliArchitecture =
    /// this value represents currently running OS architecture 
    | Auto
    | X86
    | X64

/// .NET Core SDK version (used to specify version when installing .NET Core SDK)
type DotnetCliVersion =
    /// most latest build on specific channel 
    | Latest
    ///  last known good version on specific channel (Note: LKG work is in progress. Once the work is finished, this will become new default)
    | Lkg
    /// 4-part version in a format A.B.C.D - represents specific version of build
    | Version of string
  
/// .NET Core SDK install options
type DotNetCliInstallOptions =
    {   
        /// Custom installer obtain (download) options
        InstallerOptions: DotNetInstallerOptions -> DotNetInstallerOptions
        /// .NET Core SDK channel (defaults to normalized installer branch)
        Channel: string option;
        /// .NET Core SDK version
        Version: DotnetCliVersion;
        /// Custom installation directory (for local build installation)
        CustomInstallDir: string option
        /// Architecture
        Architecture: DotnetCliArchitecture;
        /// Include symbols in the installation (Switch does not work yet. Symbols zip is not being uploaded yet) 
        DebugSymbols: bool;
        /// If set it will not perform installation but instead display what command line to use
        DryRun: bool
        /// Do not update path variable
        NoPath: bool
    }

    /// Parameter default values.
    static member Default = {
        InstallerOptions = id
        Channel = None
        Version = Latest        
        CustomInstallDir = None
        Architecture = Auto        
        DebugSymbols = false
        DryRun = false
        NoPath = true
    }

/// .NET Core SDK install options preconfigured for preview2 tooling
let Preview2ToolingOptions options = 
    { options with
        InstallerOptions = (fun io -> 
            { io with
                Branch = "v1.0.0-preview2"                    
            })
        Channel = Some "preview"
        Version = Version "1.0.0-preview2-003121"
    }

/// [omit]
let private optionToParam option paramFormat =
    match option with
    | Some value -> sprintf paramFormat value
    | None -> ""

/// [omit]
let private boolToFlag value flagParam = 
    match value with
    | true -> flagParam
    | false -> ""

/// [omit]
let private buildDotnetCliInstallArgs (param: DotNetCliInstallOptions) =
    let versionParamValue = 
        match param.Version with
        | Latest -> "latest"
        | Lkg -> "lkg"
        | Version ver -> ver

    // get channel value from installer branch info    
    let channelParamValue = 
        match param.Channel with
            | Some ch -> ch
            | None -> 
                let installerOptions = DotNetInstallerOptions.Default |> param.InstallerOptions
                installerOptions.Branch |> String.replace "/" "-"

    let architectureParamValue = 
        match param.Architecture with
        | Auto -> None
        | X86 -> Some "x86"
        | X64 -> Some "x64"
    [   
        sprintf "-Channel '%s'" channelParamValue
        sprintf "-Version '%s'" versionParamValue        
        optionToParam architectureParamValue "-Architecture %s"
        optionToParam param.CustomInstallDir "-InstallDir '%s'"
        boolToFlag param.DebugSymbols "-DebugSymbols"
        boolToFlag param.DryRun "-DryRun"
        boolToFlag param.NoPath "-NoPath"
    ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "



/// Install .NET Core SDK if required
/// ## Parameters
///
/// - 'setParams' - set installation options
let DotnetCliInstall setParams =
    let param = DotNetCliInstallOptions.Default |> setParams  
    let installScript = DotnetDownloadInstaller param.InstallerOptions

    let args = sprintf "-ExecutionPolicy Bypass -NoProfile -NoLogo -NonInteractive -Command \"%s %s; if (-not $?) { exit -1 };\"" installScript (buildDotnetCliInstallArgs param)
    let exitCode = 
        Process.ExecProcess (fun info ->
            info.FileName <- "powershell"
            info.WorkingDirectory <- Path.GetTempPath()
            info.Arguments <- args
        ) TimeSpan.MaxValue

    if exitCode <> 0 then
        // force download new installer script
        Trace.traceError ".NET Core SDK install failed, trying to redownload installer..."
        DotnetDownloadInstaller (param.InstallerOptions >> (fun o -> 
            { o with 
                AlwaysDownload = true
            })) |> ignore
        failwithf ".NET Core SDK install failed with code %i" exitCode

/// dotnet cli command execution options
type DotnetOptions =
    {
        /// Dotnet cli executable path
        DotnetCliPath: string;
        /// Command working directory
        WorkingDirectory: string;
        /// Custom parameters
        CustomParams: string option
    }

    static member Default = {
        DotnetCliPath = dotnetCliPath DefaultDotnetCliDir
        WorkingDirectory = Directory.GetCurrentDirectory()
        CustomParams = None
    }


/// Execute raw dotnet cli command
/// ## Parameters
///
/// - 'options' - common execution options
/// - 'args' - command arguments
let Dotnet (options: DotnetOptions) args = 
    let errors = new System.Collections.Generic.List<string>()
    let messages = new System.Collections.Generic.List<string>()
    let timeout = TimeSpan.MaxValue

    let errorF msg =
        Trace.traceError msg
        errors.Add msg 

    let messageF msg =
        Trace.traceImportant msg
        messages.Add msg

    let cmdArgs = match options.CustomParams with
                    | Some v -> sprintf "%s %s" args v
                    | None -> args

    let result = 
        Process.ExecProcessWithLambdas (fun info ->
            info.FileName <- options.DotnetCliPath
            info.WorkingDirectory <- options.WorkingDirectory
            info.Arguments <- cmdArgs
        ) timeout true errorF messageF

    Process.ProcessResult.New result messages errors


/// [omit]
let private argList2 name values =
    values
    |> Seq.collect (fun v -> ["--" + name; sprintf @"""%s""" v])
    |> String.concat " "

/// [omit]
let private argOption name value =
    match value with
        | true -> sprintf "--%s" name
        | false -> ""

/// dotnet restore verbosity
type NugetRestoreVerbosity =
    | Debug
    | Verbose
    | Information
    | Minimal
    | Warning
    | Error

/// dotnet restore command options
type DotnetRestoreOptions =
    {   
        /// Common tool options
        Common: DotnetOptions;
        /// Nuget feeds to search updates in. Use default if empty.
        Sources: string list;
        /// Directory to install packages in (--packages).
        Packages: string list;
        /// Path to the nuget configuration file (nuget.config).
        ConfigFile: string option;
        /// No cache flag (--no-cache)
        NoCache: bool;
        /// Restore logging verbosity (--verbosity)
        Verbosity: NugetRestoreVerbosity option
        /// Only warning failed sources if there are packages meeting version requirement (--ignore-failed-sources)
        IgnoreFailedSources: bool;
        /// Disables restoring multiple projects in parallel (--disable-parallel)
        DisableParallel: bool;
    }

    /// Parameter default values.
    static member Default = {
        Common = DotnetOptions.Default
        Sources = []
        Packages = []
        ConfigFile = None        
        NoCache = false
        Verbosity = None
        IgnoreFailedSources = false
        DisableParallel = false
    }

/// [omit]
let private buildRestoreArgs (param: DotnetRestoreOptions) =
    [   param.Sources |> argList2 "source"
        param.Packages |> argList2 "packages"
        param.ConfigFile |> Option.toList |> argList2 "configFile"
        param.NoCache |> argOption "no-cache" 
        param.IgnoreFailedSources |> argOption "ignore-failed-sources" 
        param.DisableParallel |> argOption "disable-parallel" 
        param.Verbosity |> Option.toList |> Seq.map (fun v -> v.ToString()) |> argList2 "verbosity"
    ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


/// Execute dotnet restore command
/// ## Parameters
///
/// - 'setParams' - set restore command parameters
/// - 'project' - project to restore packages
let DotnetRestore setParams project =    
    use t = Trace.traceTask "Dotnet:restore" project
    let param = DotnetRestoreOptions.Default |> setParams    
    let args = sprintf "restore %s %s" project (buildRestoreArgs param)
    let result = Dotnet param.Common args    
    if not result.OK then failwithf "dotnet restore failed with code %i" result.ExitCode

/// build configuration
type BuildConfiguration =
    | Debug
    | Release
    | Custom of string

/// [omit]
let private buildConfigurationArg (param: BuildConfiguration) =
    sprintf "--configuration %s" 
        (match param with
        | Debug -> "Debug"
        | Release -> "Release"
        | Custom config -> config)

/// dotnet pack command options
type DotNetPackOptions =
    {   
        /// Common tool options
        Common: DotnetOptions;
        /// Pack configuration (--configuration)
        Configuration: BuildConfiguration;
        /// Version suffix to use
        VersionSuffix: string option;
        /// Build base path (--build-base-path)
        BuildBasePath: string option;
        /// Output path (--output)
        OutputPath: string option;
        /// No build flag (--no-build)
        NoBuild: bool;
    }

    /// Parameter default values.
    static member Default = {
        Common = DotnetOptions.Default
        Configuration = Release
        VersionSuffix = None
        BuildBasePath = None
        OutputPath = None
        NoBuild = false
    }

/// [omit]
let private buildPackArgs (param: DotNetPackOptions) =
    [  
        buildConfigurationArg param.Configuration
        param.VersionSuffix |> Option.toList |> argList2 "version-suffix"
        param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
        param.OutputPath |> Option.toList |> argList2 "output"
        param.NoBuild |> argOption "no-build" 
    ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


/// Execute dotnet pack command
/// ## Parameters
///
/// - 'setParams' - set pack command parameters
/// - 'project' - project to pack
let DotnetPack setParams project =    
    use t = Trace.traceTask "Dotnet:pack" project
    let param = DotNetPackOptions.Default |> setParams    
    let args = sprintf "pack %s %s" project (buildPackArgs param)
    let result = Dotnet param.Common args    
    if not result.OK then failwithf "dotnet pack failed with code %i" result.ExitCode

/// dotnet publish command options
type DotNetPublishOptions =
    {   
        /// Common tool options
        Common: DotnetOptions;
        /// Pack configuration (--configuration)
        Configuration: BuildConfiguration;
        /// Target framework to compile for (--framework)
        Framework: string option;
        /// Target runtime to publish for (--runtime)
        Runtime: string option;
        /// Build base path (--build-base-path)
        BuildBasePath: string option;
        /// Output path (--output)
        OutputPath: string option;
        /// Defines what `*` should be replaced with in version field in project.json (--version-suffix)
        VersionSuffix: string option;
        /// No build flag (--no-build)
        NoBuild: bool;
    }

    /// Parameter default values.
    static member Default = {
        Common = DotnetOptions.Default
        Configuration = Release
        Framework = None
        Runtime = None
        BuildBasePath = None
        OutputPath = None
        VersionSuffix = None
        NoBuild = false
    }

/// [omit]
let private buildPublishArgs (param: DotNetPublishOptions) =
    [  
        buildConfigurationArg param.Configuration
        param.Framework |> Option.toList |> argList2 "framework"
        param.Runtime |> Option.toList |> argList2 "runtime"
        param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
        param.OutputPath |> Option.toList |> argList2 "output"
        param.VersionSuffix |> Option.toList |> argList2 "version-suffix"
        param.NoBuild |> argOption "no-build" 
    ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


/// Execute dotnet publish command
/// ## Parameters
///
/// - 'setParams' - set publish command parameters
/// - 'project' - project to publish
let DotnetPublish setParams project =    
    use t = Trace.traceTask "Dotnet:publish" project
    let param = DotNetPublishOptions.Default |> setParams    
    let args = sprintf "publish %s %s" project (buildPublishArgs param)
    let result = Dotnet param.Common args    
    if not result.OK then failwithf "dotnet publish failed with code %i" result.ExitCode

/// dotnet build command options
type DotNetBuildOptions =
    {   
        /// Common tool options
        Common: DotnetOptions;
        /// Pack configuration (--configuration)
        Configuration: BuildConfiguration;
        /// Target framework to compile for (--framework)
        Framework: string option;
        /// Target runtime to publish for (--runtime)
        Runtime: string option;
        /// Build base path (--build-base-path)
        BuildBasePath: string option;
        /// Output path (--output)
        OutputPath: string option;
        /// Native flag (--native)
        Native: bool;
    }

    /// Parameter default values.
    static member Default = {
        Common = DotnetOptions.Default
        Configuration = Release
        Framework = None
        Runtime = None
        BuildBasePath = None
        OutputPath = None
        Native = false
    }


/// [omit]
let private buildBuildArgs (param: DotNetBuildOptions) =
    [  
        buildConfigurationArg param.Configuration
        param.Framework |> Option.toList |> argList2 "framework"
        param.Runtime |> Option.toList |> argList2 "runtime"
        param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
        param.OutputPath |> Option.toList |> argList2 "output"
        (if param.Native then "--native" else "")
    ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


/// Execute dotnet build command
/// ## Parameters
///
/// - 'setParams' - set compile command parameters
/// - 'project' - project to compile
let DotnetCompile setParams project =    
    use t = Trace.traceTask "Dotnet:build" project
    let param = DotNetBuildOptions.Default |> setParams    
    let args = sprintf "build %s %s" project (buildBuildArgs param)
    let result = Dotnet param.Common args    
    if not result.OK then failwithf "dotnet build failed with code %i" result.ExitCode

/// get sdk version from global.json
/// ## Parameters
///
/// - 'project' - global.json path
//let GlobalJsonSdk project =
//    let data = ReadFileAsString project
//    let info = JsonValue.Parse(data)
//    info?sdk?version.AsString()   