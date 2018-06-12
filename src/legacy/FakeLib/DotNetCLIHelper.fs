/// Contains a task which can be used to run dotnet CLI commands.
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
module Fake.DotNetCli

open Fake
open System
open System.IO
open System.IO.Compression
open System.Text
open Newtonsoft.Json.Linq

/// The dotnet command name
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let commandName = "dotnet"

/// Gets the installed dotnet version
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let getVersion() = 
    let processResult = 
        ExecProcessAndReturnMessages (fun info ->  
          info.FileName <- commandName
          info.WorkingDirectory <- Environment.CurrentDirectory
          info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

    processResult.Messages |> separated ""

/// Checks wether the dotnet CLI is installed
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let isInstalled() =
    try
        let processResult =
            ExecProcessAndReturnMessages (fun info ->  
              info.FileName <- commandName
              info.WorkingDirectory <- Environment.CurrentDirectory
              info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

        processResult.OK
    with _ -> false

/// DotNet parameters
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
[<CLIMutable>]
type CommandParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan
}

let private DefaultCommandParams : CommandParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    TimeOut = TimeSpan.FromMinutes 30.
}

/// Runs a dotnet command.
/// ## Parameters
///
///  - `setCommandParams` - Function used to overwrite the default parameters.
///  - `args` - command and additional arguments.
///
/// ## Sample
///
///     DotNetCli.RunCommand
///         (fun p -> 
///              { p with 
///                   TimeOut = TimeSpan.FromMinutes 10. })
///         "restore"
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let RunCommand (setCommandParams: CommandParams -> CommandParams) args =
    use __ = traceStartTaskUsing "DotNet" ""

    let parameters = setCommandParams DefaultCommandParams

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "dotnet command failed on %s" args

/// DotNet restore parameters
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
[<CLIMutable>]
type RestoreParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string
    
    /// Project (optional).
    Project: string

    /// A timeout for the command.
    TimeOut: TimeSpan
    
    /// Whether to use the NuGet cache.
    NoCache : bool

    /// Additional Args
    AdditionalArgs : string list
    
    //Disables restoring multiple projects in parallel.
    DisableParallel : bool
}

let private DefaultRestoreParams : RestoreParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    NoCache = false
    Project = ""
    TimeOut = TimeSpan.FromMinutes 30.
    AdditionalArgs = []
    DisableParallel = false
}

/// Runs the dotnet "restore" command.
/// ## Parameters
///
///  - `setRestoreParams` - Function used to overwrite the restore default parameters.
///
/// ## Sample
///
///     DotNetCli.Restore 
///         (fun p -> 
///              { p with 
///                   NoCache = true })
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let Restore (setRestoreParams: RestoreParams -> RestoreParams) =
    use __ = traceStartTaskUsing "DotNet.Restore" ""

    let parameters = setRestoreParams DefaultRestoreParams
    let args =
        new StringBuilder()
        |> append "restore"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrue parameters.NoCache "--no-cache"
        |> appendIfTrue parameters.DisableParallel "--disable-parallel"
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Restore failed on %s" args

/// DotNet build parameters
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
[<CLIMutable>]
type BuildParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan

    /// Project (optional).
    Project: string
    
    /// The build configuration.
    Configuration : string

    /// Allows to build for a specific framework
    Framework : string

    /// Allows to build for a specific runtime
    Runtime : string

    /// Additional Args
    AdditionalArgs : string list
    
    /// Optional outputh path
    Output : string
}

let private DefaultBuildParams : BuildParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
    Framework = ""
    Runtime = ""
    Project = ""
    AdditionalArgs = []
    Output = ""
}

/// Runs the dotnet "build" command.
/// ## Parameters
///
///  - `setBuildParams` - Function used to overwrite the build default parameters.
///
/// ## Sample
///
///     DotNetCli.Build
///       (fun p -> 
///            { p with 
///                 Configuration = "Release" })
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let Build (setBuildParams: BuildParams -> BuildParams) =
    use __ = traceStartTaskUsing "DotNet.Build" ""

    let parameters = setBuildParams DefaultBuildParams
    let args =
        new StringBuilder()
        |> append "build"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Framework) (sprintf "--framework %s"  parameters.Framework)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Runtime) (sprintf "--runtime %s"  parameters.Runtime)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Output) (sprintf "--output %s"  parameters.Output)   
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Build failed on %s" args


/// DotNet test parameters
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
[<CLIMutable>]
type TestParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan

    /// Project (optional).
    Project: string    
    
    /// The build configuration.
    Configuration : string

    /// Allows to test a specific framework
    Framework : string

    /// Allows to test a specific runtime
    Runtime : string

    /// Additional Args
    AdditionalArgs : string list
}

let private DefaultTestParams : TestParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
    Framework = ""
    Project = ""
    Runtime = ""
    AdditionalArgs = []
}

/// Runs the dotnet "test" command.
/// ## Parameters
///
///  - `setTestParams` - Function used to overwrite the test default parameters.
///
/// ## Sample
///
///     DotNetCli.Test
///       (fun p -> 
///            { p with 
///                 Configuration = "Release" })
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let Test (setTestParams: TestParams -> TestParams) =
    use __ = traceStartTaskUsing "DotNet.Test" ""

    let parameters = setTestParams DefaultTestParams
    let args =
        new StringBuilder()
        |> append "test"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Framework) (sprintf "--framework %s"  parameters.Framework)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Runtime) (sprintf "--runtime %s"  parameters.Runtime)
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Test failed on %s" args


/// DotNet pack parameters
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
[<CLIMutable>]
type PackParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Optional output path.
    OutputPath: string

    /// Optional version suffix.
    VersionSuffix: string

    /// Project (optional).
    Project: string    

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan
    
    /// The build configuration.
    Configuration : string

    /// Additional Args
    AdditionalArgs : string list
}

let private DefaultPackParams : PackParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    OutputPath = ""
    VersionSuffix = ""
    Project = ""
    TimeOut = TimeSpan.FromMinutes 30.
    AdditionalArgs = []
}

/// Runs the dotnet "pack" command.
/// ## Parameters
///
///  - `setPackParams` - Function used to overwrite the pack default parameters.
///
/// ## Sample
///
///     DotNetCli.Pack
///       (fun p -> 
///            { p with 
///                 Configuration = "Release" })
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let Pack (setPackParams: PackParams -> PackParams) =
    use __ = traceStartTaskUsing "DotNet.Pack" ""

    let parameters = setPackParams DefaultPackParams
    let args =
        new StringBuilder()
        |> append "pack"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.OutputPath) (sprintf "--output %s"  parameters.OutputPath)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.VersionSuffix) (sprintf "--version-suffix %s"  parameters.VersionSuffix)
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Pack failed on %s" args

/// DotNet publish parameters
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
[<CLIMutable>]
type PublishParams = {
    /// ToolPath - usually just "dotnet"
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the command.
    TimeOut: TimeSpan

    /// Project (optional).
    Project: string
    
    /// The build configuration.
    Configuration : string

    /// Allows to publish to a specific framework
    Framework : string

    /// Allows to test a specific runtime
    Runtime : string

    /// Optional version suffix.
    VersionSuffix: string

    /// Optional outputh path
    Output : string

    /// Additional Args
    AdditionalArgs : string list
}

let private DefaultPublishParams : PublishParams = {
    ToolPath = commandName
    WorkingDir = Environment.CurrentDirectory
    Configuration = "Release"
    TimeOut = TimeSpan.FromMinutes 30.
    Project = ""
    Framework = ""
    Runtime = ""
    VersionSuffix = ""
    Output = ""
    AdditionalArgs = []
}

/// Runs the dotnet "publish" command.
/// ## Parameters
///
///  - `setPublishParams` - Function used to overwrite the publish default parameters.
///
/// ## Sample
///
///     DotNetCli.Publish
///       (fun p -> 
///            { p with 
///                 Configuration = "Release" })
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let Publish (setPublishParams: PublishParams -> PublishParams) =
    use __ = traceStartTaskUsing "DotNet.Publish" ""

    let parameters = setPublishParams DefaultPublishParams
    let args =
        new StringBuilder()
        |> append "publish"
        |> appendStringIfValueIsNotNullOrEmpty parameters.Project parameters.Project
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Configuration) (sprintf "--configuration %s"  parameters.Configuration)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Framework) (sprintf "--framework %s"  parameters.Framework)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Runtime) (sprintf "--runtime %s"  parameters.Runtime)
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.Output) (sprintf "--output %s"  parameters.Output)     
        |> appendIfTrueWithoutQuotes (isNotNullOrEmpty parameters.VersionSuffix) (sprintf "--version-suffix %s"  parameters.VersionSuffix)           
        |> fun sb ->
            parameters.AdditionalArgs
            |> List.fold (fun sb arg -> appendWithoutQuotes arg sb) sb
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Test Publish on %s" args



/// Sets version in project.json
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let SetVersionInProjectJson (version:string) fileName = 
    use __ = traceStartTaskUsing "DotNet.SetVersion" fileName
    let original = File.ReadAllText fileName
    let p = JObject.Parse(original)
    p.["version"] <- JValue version
    let newText = p.ToString()
    if newText <> original then
        File.WriteAllText(fileName,newText)

[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let mutable DotnetSDKPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) </> "dotnetcore" |> FullName


/// Gets the DotNet SDK from the global.json
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let GetDotNetSDKVersionFromGlobalJson() : string = 
    if not (File.Exists "global.json") then
        failwithf "global.json not found"
    try
        let content = File.ReadAllText "global.json"
        let json = Newtonsoft.Json.Linq.JObject.Parse content
        let sdk = json.Item("sdk") :?> JObject
        let version = sdk.Property("version").Value.ToString()
        version
    with
    | exn -> failwithf "Could not parse global.json: %s" exn.Message


/// Installs the DotNet SDK locally to the given path
[<System.Obsolete("Please add 'open Fake.DotNet' and use 'DotNet.*' instead, see https://fake.build/dotnet-dotnet.html for an example (the fake 5 module is called Fake.DotNet.Cli)")>]
let InstallDotNetSDK sdkVersion =
    let buildLocalPath = DotnetSDKPath </> (if isWindows then "dotnet.exe" else "dotnet")
    let mutable dotnetExePath = "dotnet"
    let correctVersionInstalled exe = 
        try
            let processResult = 
                ExecProcessAndReturnMessages (fun info ->  
                info.FileName <- exe
                info.WorkingDirectory <- Environment.CurrentDirectory
                info.Arguments <- "--info") (TimeSpan.FromMinutes 30.)

            processResult.Messages
            |> Seq.exists (fun m -> m.Contains "Version" && m.Contains(sdkVersion)) // This checks sdk and cli version
        with 
        | _ ->
            try
                let processResult = 
                    ExecProcessAndReturnMessages (fun info ->  
                    info.FileName <- exe
                    info.WorkingDirectory <- Environment.CurrentDirectory
                    info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)
                processResult.Messages |> separated "" = sdkVersion
            with 
            | _ -> false

    if correctVersionInstalled dotnetExePath then
        tracefn "dotnetcli %s already installed in PATH" sdkVersion
    elif correctVersionInstalled buildLocalPath then
        tracefn "cmd %s already installed in LocalApplicationData" sdkVersion
        dotnetExePath <- buildLocalPath
    else
        CleanDir DotnetSDKPath
        let tempDir = Path.GetTempPath()

        let downloadSDK downloadPath archiveFileName =

            let localPath = Path.Combine(tempDir, archiveFileName) |> FullName
            if not (File.Exists localPath) then
                tracefn "Downloading '%s' to '%s'" downloadPath localPath
            
                let proxy = Net.WebRequest.DefaultWebProxy
                proxy.Credentials <- Net.CredentialCache.DefaultCredentials

                use webclient = new Net.WebClient(Proxy = proxy)
                webclient.DownloadFile(downloadPath, localPath)
            localPath

        let localPath =
            try
                let archiveFileName = 
                    if isWindows then
                        if Environment.Is64BitOperatingSystem then
                            sprintf "dotnet-dev-win-x64.%s.zip" sdkVersion
                        else
                            sprintf "dotnet-dev-win-x86.%s.zip" sdkVersion
                    elif isLinux then
                        sprintf "dotnet-dev-ubuntu-x64.%s.tar.gz" sdkVersion
                    else
                        sprintf "dotnet-dev-osx-x64.%s.tar.gz" sdkVersion

                let downloadPath = sprintf "https://dotnetcli.azureedge.net/dotnet/Sdk/%s/%s" sdkVersion archiveFileName
                downloadSDK downloadPath archiveFileName
            with
            | _ -> 
                let archiveFileName = 
                    if isWindows then
                        if Environment.Is64BitOperatingSystem then
                            sprintf "dotnet-sdk-%s-win-x64.zip" sdkVersion
                        else
                            sprintf "dotnet-sdk-%s-win-x86.zip" sdkVersion
                    elif isLinux then
                        sprintf "dotnet-sdk-%s-linux-x64.tar.gz" sdkVersion
                    else
                        sprintf "dotnet-sdk-%s-osx-x64.tar.gz" sdkVersion        
                try                
                    let downloadPath = sprintf "https://dotnetcli.blob.core.windows.net/dotnet/Sdk/%s/%s" sdkVersion archiveFileName
                    downloadSDK downloadPath archiveFileName
                with
                | _ ->
                    let downloadPath = sprintf "https://download.microsoft.com/download/F/A/A/FAAE9280-F410-458E-8819-279C5A68EDCF/%s" archiveFileName
                    downloadSDK downloadPath archiveFileName

        if isWindows then
            Unzip DotnetSDKPath localPath
        else
            let assertExitCodeZero x =
                if x = 0 then () else
                failwithf "Command failed with exit code %i" x

            Shell.Exec("tar", sprintf """-xvf "%s" -C "%s" """ localPath DotnetSDKPath)
            |> assertExitCodeZero

        tracefn "dotnet cli path - %s" DotnetSDKPath
        System.IO.Directory.EnumerateFiles DotnetSDKPath
        |> Seq.iter (fun path -> tracefn " - %s" path)
        System.IO.Directory.EnumerateDirectories DotnetSDKPath
        |> Seq.iter (fun path -> tracefn " - %s%c" path System.IO.Path.DirectorySeparatorChar)

        dotnetExePath <- buildLocalPath
    dotnetExePath
