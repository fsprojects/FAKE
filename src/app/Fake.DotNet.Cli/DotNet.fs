namespace Fake.DotNet

/// <summary>
/// .NET Core + CLI tools helpers
/// </summary>
[<RequireQualifiedAccess>]
module DotNet =

    // NOTE: The #if can be removed once we have a working release with the "new" API
    // Currently we #load this file in build.fsx

    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators
    open Fake.DotNet.NuGet
    open System
    open System.IO
    open System.Security.Cryptography
    open System.Text
    open System.Text.Json

    /// <summary>
    /// .NET Core SDK default install directory (set to default SDK installer paths
    /// (<c>%HOME/.dotnet</c> or <c>%LOCALAPPDATA%/Microsoft/dotnet</c>).
    /// </summary>
    let defaultUserInstallDir =
        if Environment.isUnix then
            Environment.environVar "HOME" @@ ".dotnet"
        else
            Environment.environVar "LocalAppData" @@ "Microsoft" @@ "dotnet"

    /// <summary>
    /// .NET Core SDK default install directory (set to default SDK installer paths
    /// (<c>/usr/local/share/dotnet</c> or <c>C:\Program Files\dotnet</c>))
    /// </summary>
    let defaultSystemInstallDir =
        if Environment.isUnix then
            "/usr/local/share/dotnet"
        else
            @"C:\Program Files\dotnet"

    /// <summary>
    /// Tries to get the DotNet SDK from the global.json, starts searching in the given directory.
    /// Returns None if global.json is not found
    /// </summary>
    ///
    /// <param name="startDir">The directory to start search from</param>
    let internal tryGetSDKVersionFromGlobalJsonDir startDir : string option =
        let globalJsonPaths rootDir =
            let rec loop (dir: DirectoryInfo) =
                seq {
                    match dir.GetFiles "global.json" with
                    | [| json |] -> yield json
                    | _ -> ()

                    if not (isNull dir.Parent) then
                        yield! loop dir.Parent
                }

            loop (DirectoryInfo rootDir)

        match Seq.tryHead (globalJsonPaths startDir) with
        | None -> None
        | Some globalJson ->
            try
                let content = File.ReadAllText globalJson.FullName

                let json =
                    JsonDocument.Parse(content, JsonDocumentOptions(CommentHandling = JsonCommentHandling.Skip))

                let sdk = json.RootElement.GetProperty("sdk")

                match sdk.TryGetProperty("version") with
                | false, _ -> None
                | true, version -> Some(version.GetString())
            with exn ->
                failwithf "Could not parse `sdk.version` from global.json at '%s': %s" globalJson.FullName exn.Message


    /// <summary>
    /// Gets the DotNet SDK from the global.json, starts searching in the given directory.
    /// </summary>
    let internal getSDKVersionFromGlobalJsonDir startDir : string =
        tryGetSDKVersionFromGlobalJsonDir startDir
        |> function
            | Some version -> version
            | None -> failwithf "global.json not found"

    /// <summary>
    /// Tries the DotNet SDK from the global.json. This file can exist in the working
    /// directory or any of the parent directories Returns None if global.json is not found
    /// </summary>
    let tryGetSDKVersionFromGlobalJson () : string option = tryGetSDKVersionFromGlobalJsonDir "."

    /// <summary>
    /// Gets the DotNet SDK from the global.json. This file can exist in the working
    /// directory or any of the parent directories
    /// </summary>
    let getSDKVersionFromGlobalJson () : string = getSDKVersionFromGlobalJsonDir "."

    /// <summary>
    /// Get dotnet cli executable path. Probes the provided path first, then as a fallback tries the system PATH
    /// </summary>
    ///
    /// <param name="dotnetCliDir">the path to check else will probe system PATH</param>
    let findPossibleDotnetCliPaths dotnetCliDir =
        seq {
            let fileName = if Environment.isUnix then "dotnet" else "dotnet.exe"

            yield!
                ProcessUtils.findFilesOnPath "dotnet"
                |> Seq.filter File.Exists
                |> Seq.filter (fun dotPath -> dotPath.EndsWith fileName)

            let userInstallDir = defaultUserInstallDir </> fileName

            if File.exists userInstallDir then
                yield userInstallDir

            let systemInstallDir = defaultSystemInstallDir </> fileName

            if File.exists systemInstallDir then
                yield systemInstallDir

            match dotnetCliDir with
            | Some userSetPath ->
                let defaultCliPath = userSetPath @@ fileName

                match File.Exists defaultCliPath with
                | true -> yield defaultCliPath
                | _ -> ()
            | None -> ()
        }

    /// <summary>
    /// Get .NET Core SDK download uri
    /// </summary>
    let private getGenericDotNetCliInstallerUrl branch installerName =
        sprintf "https://raw.githubusercontent.com/dotnet/install-scripts/%s/src/%s" branch installerName

    let private getPowershellDotNetCliInstallerUrl branch =
        getGenericDotNetCliInstallerUrl branch "dotnet-install.ps1"

    let private getBashDotNetCliInstallerUrl branch =
        getGenericDotNetCliInstallerUrl branch "dotnet-install.sh"


    /// <summary>
    /// Download .NET Core SDK installer
    /// </summary>
    let private downloadDotNetInstallerFromUrl (url: string) fileName =
        //let url = getDotNetCliInstallerUrl branch
#if USE_HTTPCLIENT
        let h = new System.Net.Http.HttpClient()
        use f = File.Open(fileName, FileMode.Create)
        h.GetStreamAsync(url).Result.CopyTo(f)
#else
        use w = new System.Net.WebClient()
        w.DownloadFile(url, fileName) // Http.RequestStream url
#endif
        //use outFile = File.Open(fileName, FileMode.Create)
        //installScript.ResponseStream.CopyTo(outFile)
        Trace.trace (sprintf "downloaded dotnet installer (%s) to %s" url fileName)

    let private md5 (data: byte array) : string =
        use md5 = MD5.Create()

        (StringBuilder(), md5.ComputeHash(data))
        ||> Array.fold (fun sb b -> sb.Append(b.ToString("x2")))
        |> string


    /// <summary>
    /// .NET Core SDK installer download options
    /// </summary>
    type InstallerOptions =
        {
            /// Always download install script (otherwise install script is cached in temporary folder)
            AlwaysDownload: bool

            /// Download installer from this github branch
            Branch: string

            // Use the given directory to download the script into. If None the temp-dir is used
            CustomDownloadDir: string option
        }

        /// Parameter default values.
        static member Default =
            { AlwaysDownload = false
              Branch = "main"
              CustomDownloadDir = None }

    /// <summary>
    /// Download .NET Core SDK installer
    /// </summary>
    ///
    /// <param name="setParams">set download installer options</param>
    let downloadInstaller setParams =
        let param = InstallerOptions.Default |> setParams

        let ext = if Environment.isUnix then "sh" else "ps1"

        let getInstallerUrl =
            if Environment.isUnix then
                getBashDotNetCliInstallerUrl
            else
                getPowershellDotNetCliInstallerUrl

        let scriptName =
            sprintf "dotnet_install_%s.%s" (md5 (Encoding.ASCII.GetBytes(param.Branch))) ext

        let tempDir =
            match param.CustomDownloadDir with
            | None -> Path.GetTempPath()
            | Some d -> d

        let tempInstallerScript = tempDir @@ scriptName

        // maybe download installer script
        if param.AlwaysDownload || not (File.Exists(tempInstallerScript)) then
            let url = getInstallerUrl param.Branch
            downloadDotNetInstallerFromUrl url tempInstallerScript

        tempInstallerScript


    /// .NET Core SDK architecture
    type CliArchitecture =
        /// this value represents currently running OS architecture
        | Auto
        | X86
        | X64

    /// .NET Core SDK version (used to specify version when installing .NET Core SDK)
    type CliVersion =
        ///  Latest build on the channel (used with the -Channel option).
        | Latest
        /// Latest coherent build on the channel; uses the latest stable package combination (used with Branch name
        /// -Channel options).
        | Coherent
        /// Three-part version in X.Y.Z format representing a specific build version; supersedes the -Channel option.
        /// For example: 2.0.0-preview2-006120.
        | Version of string
        /// Take version from global.json and fail if it is not found.
        | GlobalJson

    /// <summary>
    /// Specifies the source channel for the installation.
    /// </summary>
    module CliChannel =
        /// Long-Term Support channel (most current supported release).
        let LTS = Some "LTS"
        /// Most current release.
        let Current = Some "Current"
        /// Two-part version in X.Y format representing a specific release (for example, 2.0 or 1.0).
        let Version major minor = Some(sprintf "%d.%d" major minor)
        /// Branch name. For example, release/2.0.0, release/2.0.0-preview2, or master (for nightly releases).
        let Branch branchName = Some branchName

    /// <summary>
    /// .NET Core SDK install options
    /// </summary>
    [<NoComparison>]
    [<NoEquality>]
    type CliInstallOptions =
        {
            /// Custom installer obtain (download) options
            InstallerOptions: InstallerOptions -> InstallerOptions

            /// <summary>
            /// Specifies the source channel for the installation. The possible values are:
            /// <list type="number">
            /// <item>
            /// <c>Current</c> - Most current release.
            /// </item>
            /// <item>
            /// <c>LTS</c> - Long-Term Support channel (most current supported release).
            /// </item>
            /// <item>
            /// Two-part version in <c>X.Y</c> format representing a specific release (for example, <c>2.0</c> or
            /// <c>1.0</c>).
            /// </item>
            /// <item>
            /// Branch name. For example, release/2.0.0, release/2.0.0-preview2, or master (for nightly releases).
            /// </item>
            /// </list>
            /// The default value is <c>LTS</c>. For more information on .NET support channels, see the
            /// .NET Support Policy page.
            /// </summary>
            /// <remarks>
            /// Use the <c>CliChannel</c> module, for example <c>CliChannel.Current</c>
            /// </remarks>
            Channel: string option

            /// .NET Core SDK version
            Version: CliVersion

            /// Custom installation directory (for local build installation)
            CustomInstallDir: string option

            /// Always download and run the installer, ignore potentially existing installations.
            ForceInstall: bool

            /// Architecture
            Architecture: CliArchitecture

            /// Include symbols in the installation (Switch does not work yet. Symbols zip is not being uploaded yet)
            DebugSymbols: bool

            /// If set it will not perform installation but instead display what command line to use
            DryRun: bool

            /// Do not update path variable
            NoPath: bool

            /// Command working directory
            WorkingDirectory: string
        }

        /// Parameter default values.
        static member Default =
            { InstallerOptions = id
              Channel = None
              Version = Latest
              CustomInstallDir = None
              ForceInstall = false
              Architecture = Auto
              DebugSymbols = false
              DryRun = false
              NoPath = true
              WorkingDirectory = "." }

    /// <summary>
    /// The a list of well-known versions to install
    /// </summary>
    module Versions =
        /// .NET Core SDK install options preconfigured for preview2 tooling
        let internal Preview2ToolingOptions options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "v1.0.0-preview2" })
                Channel = Some "preview"
                Version = Version "1.0.0-preview2-003121" }

        /// .NET Core SDK install options preconfigured for preview4 tooling
        let internal LatestPreview4ToolingOptions options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "rel/1.0.0-preview4" })
                Channel = None
                Version = Latest }

        /// .NET Core SDK install options preconfigured for preview4 tooling
        let internal Preview4_004233ToolingOptions options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "rel/1.0.0-preview4" })
                Channel = None
                Version = Version "1.0.0-preview4-004233" }

        /// .NET Core SDK install options preconfigured for preview4 tooling
        let internal RC4_004771ToolingOptions options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "rel/1.0.0-rc3" })
                Channel = None
                Version = Version "1.0.0-rc4-004771" }

        /// .NET Core SDK install options preconfigured for preview4 tooling, this is marketized as v1.0.1
        /// release of the .NET Core tools
        let internal RC4_004973ToolingOptions options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "rel/1.0.1" })
                Channel = None
                Version = Version "1.0.3-rc4-004973" }

        let Release_1_0_4 options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "release/2.0.0" })
                Channel = None
                Version = Version "1.0.4" }

        let Release_2_0_0 options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "release/2.0.0" })
                Channel = None
                Version = Version "2.0.0" }

        let Release_2_0_3 options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "release/2.0.0" })
                Channel = None
                Version = Version "2.0.3" }

        let Release_2_1_4 options =
            { options with
                InstallerOptions = (fun io -> { io with Branch = "release/2.1" })
                Channel = None
                Version = Version "2.1.4" }

        let Release_2_1_300_RC1 option =
            { option with
                InstallerOptions = (fun io -> { io with Branch = "release/2.1" })
                Channel = None
                Version = Version "2.1.300-rc1-008673" }

        let Release_2_1_300 option =
            { option with
                InstallerOptions = (fun io -> { io with Branch = "release/2.1" })
                Channel = None
                Version = Version "2.1.300" }

        let Release_2_1_301 option =
            { option with
                InstallerOptions = (fun io -> { io with Branch = "release/2.1" })
                Channel = None
                Version = Version "2.1.301" }

        let Release_2_1_302 option =
            { option with
                InstallerOptions = (fun io -> { io with Branch = "release/2.1" })
                Channel = None
                Version = Version "2.1.302" }

        let Release_2_1_400 option =
            { option with
                InstallerOptions = (fun io -> { io with Branch = "release/2.1" })
                Channel = None
                Version = Version "2.1.400" }

        let Release_2_1_401 option =
            { option with
                InstallerOptions = (fun io -> { io with Branch = "release/2.1" })
                Channel = None
                Version = Version "2.1.401" }

        let Release_2_1_402 option =
            { option with
                InstallerOptions = (fun io -> { io with Branch = "release/2.1" })
                Channel = None
                Version = Version "2.1.402" }

        let FromGlobalJson option =
            { option with
                InstallerOptions = id
                Channel = None
                Version = CliVersion.GlobalJson }

    let private optionToParam option paramFormat =
        match option with
        | Some value -> sprintf paramFormat value
        | None -> ""

    let private boolToFlag value flagParam =
        match value with
        | true -> flagParam
        | false -> ""

    let private buildDotNetCliInstallArgs (param: CliInstallOptions) =
        let versionParamValue =
            match param.Version with
            | Latest -> "latest"
            | Coherent -> "coherent"
            | Version ver -> ver
            | GlobalJson -> getSDKVersionFromGlobalJson ()

        // get channel value from installer branch info
        let channelParamValue =
            match param.Channel with
            | Some ch -> ch
            | None ->
                let installerOptions = InstallerOptions.Default |> param.InstallerOptions
                installerOptions.Branch |> String.replace "/" "-"

        let architectureParamValue =
            match param.Architecture with
            | Auto -> None
            | X86 -> Some "x86"
            | X64 -> Some "x64"

        Arguments.Empty
        |> Arguments.appendIf true "-Verbose"
        |> Arguments.appendNotEmpty "-Channel" channelParamValue
        |> Arguments.appendNotEmpty "-Version" versionParamValue
        |> Arguments.appendOption "-Architecture" architectureParamValue
        |> Arguments.appendNotEmpty "-InstallDir" (defaultArg param.CustomInstallDir defaultUserInstallDir)
        |> Arguments.appendIf param.DebugSymbols "-DebugSymbols"
        |> Arguments.appendIf param.DryRun "-DryRun"
        |> Arguments.appendIf param.NoPath "-NoPath"

    /// dotnet restore verbosity
    type Verbosity =
        | Quiet
        | Minimal
        | Normal
        | Detailed
        | Diagnostic

    /// <summary>
    /// dotnet cli command execution options
    /// </summary>
    type Options =
        {
            /// DotNet cli executable path
            DotNetCliPath: string

            /// Write a global.json with the given version (required to make SDK choose the correct version)
            Version: string option

            /// Command working directory
            WorkingDirectory: string

            /// Process timeout, kills the process after the specified time
            Timeout: TimeSpan option

            /// Custom parameters
            CustomParams: string option

            /// Logging verbosity (<c>--verbosity</c>)
            Verbosity: Verbosity option

            /// Restore logging verbosity (<c>--diagnostics</c>)
            Diagnostics: bool

            /// If true the function will redirect the output of the called process (but will disable colors, false
            /// by default)
            RedirectOutput: bool

            /// If RedirectOutput is true this flag decides if FAKE emits the output into the standard output/error
            /// otherwise the flag is ignored.
            /// True by default.
            PrintRedirectedOutput: bool

            /// Gets the environment variables that apply to this process and its child processes.
            /// NOTE: Recommendation is to not use this Field, but instead use the helper function in the Proc module
            /// (for example <c>Process.setEnvironmentVariable</c>)
            /// NOTE: This field is ignored when UseShellExecute is true.
            Environment: Map<string, string>
        }

        /// <summary>
        /// Create a default setup for executing the <c>dotnet</c> command line.
        /// This function tries to take current <c>global.json</c> into account and tries to find the correct
        /// installation. To overwrite this behavior set <c>DotNetCliPath</c> manually (for example to the first
        /// result of <c>ProcessUtils.findFilesOnPath "dotnet"</c>)
        /// </summary>
        static member Create() =
            { DotNetCliPath =
                let version = tryGetSDKVersionFromGlobalJson ()

                findPossibleDotnetCliPaths None
                |> Seq.tryFind (fun cliPath ->
                    match version with
                    | Some version ->
                        version
                        |> Path.combine "sdk"
                        |> Path.combine (Path.getDirectory cliPath)
                        |> Directory.Exists
                    | None -> true)
                |> Option.defaultWith (fun () -> if Environment.isUnix then "dotnet" else "dotnet.exe")
              WorkingDirectory = Directory.GetCurrentDirectory()
              Timeout = None
              CustomParams = None
              Version = None
              Verbosity = None
              Diagnostics = false
              RedirectOutput = false
              PrintRedirectedOutput = true
              Environment =
                Process.createEnvironmentMap ()
                |> Map.remove "MSBUILD_EXE_PATH"
                |> Map.remove "MSBuildExtensionsPath"
                |> Map.remove "MSBuildLoadMicrosoftTargetsReadOnly"
                |> Map.remove "MSBuildSDKsPath"
                |> Map.remove "DOTNET_HOST_PATH" }

        /// Sets the current environment variables.
        member x.WithEnvironment map = { x with Environment = map }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with RedirectOutput = shouldRedirect }

        /// Sets a value indicating whether the redirected output should be printed to standard-output/error stream.
        member x.WithPrintRedirectedOutput shouldPrint =
            { x with PrintRedirectedOutput = shouldPrint }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = f x

    module Options =
        let inline lift (f: Options -> Options) (x: ^a) =
            let inline withCommon s e =
                (^a: (member WithCommon: (Options -> Options) -> ^a) (s, e))

            withCommon x f

        let inline withEnvironment map x = lift (fun o -> o.WithEnvironment map) x

        let inline withRedirectOutput shouldRedirect x =
            lift (fun o -> o.WithRedirectOutput shouldRedirect) x

        let inline withRedirectedOutput shouldPrint x =
            lift (fun o -> o.WithPrintRedirectedOutput shouldPrint) x

        let inline withWorkingDirectory wd x =
            lift (fun o -> { o with WorkingDirectory = wd }) x

        let inline withTimeout t x =
            lift (fun o -> { o with Timeout = t }) x

        let inline withDiagnostics diag x =
            lift (fun o -> { o with Diagnostics = diag }) x

        let inline withVerbosity verb x =
            lift (fun o -> { o with Verbosity = verb }) x

        let inline withCustomParams args x =
            lift (fun o -> { o with CustomParams = args }) x

        /// Sets custom command-line arguments expressed as a sequence of strings.
        /// This function overwrites and gets overwritten by `withCustomParams`.
        let inline withAdditionalArgs args x =
            withCustomParams
                (args
                 |> Args.toWindowsCommandLine
                 |> (function
                 | "" -> None
                 | x -> Some x))
                x

        let inline withDotNetCliPath path x =
            lift (fun o -> { o with DotNetCliPath = path }) x

    let private argList2 name values =
        values |> List.collect (fun v -> [ "--" + name; v ])

    let private argOption name value =
        match value with
        | true -> [ sprintf "--%s" name ]
        | false -> []

    let private argOptionExplicit name value = [ sprintf "--%s=%A" name value ]

    let private buildCommonArgs (param: Options) =
        [ defaultArg param.CustomParams "" |> Args.fromWindowsCommandLine |> Seq.toList
          param.Verbosity
          |> Option.toList
          |> List.map (fun v -> v.ToString().ToLowerInvariant())
          |> argList2 "verbosity" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)

    let private buildSdkOptionsArgs (param: Options) =
        [ param.Diagnostics |> argOption "--diagnostics" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)

    let internal withGlobalJsonDispose workDir version =
        let globalJsonPath =
            if (Path.GetFullPath workDir).StartsWith(Path.GetFullPath ".") then
                "global.json"
            else
                workDir </> "global.json"

        let writtenJson =
            match version with
            | Some version when Directory.Exists workDir ->
                // make sure to write global.json if we did not read the version from it
                // We need to do this as the SDK will use this file to select the actual version
                // See https://github.com/fsharp/FAKE/pull/1963 and related discussions
                if File.Exists globalJsonPath then
                    false
                else
                    let template = sprintf """{ "sdk": { "version": "%s" } }""" version
                    File.WriteAllText(globalJsonPath, template)
                    true
            | _ -> false

        { new IDisposable with
            member x.Dispose() =
                if writtenJson then
                    File.delete globalJsonPath }

    let internal withGlobalJson workDir version f =
        use d = withGlobalJsonDispose workDir version
        f ()

    let internal buildCommand command args options =
        let sdkOptions = buildSdkOptionsArgs options
        let commonOptions = buildCommonArgs options

        [ sdkOptions
          command // |> Args.fromWindowsCommandLine |> Seq.toList
          commonOptions
          args ] // |> Args.fromWindowsCommandLine |> Seq.toList ]
        |> List.concat

    [<RequireQualifiedAccess>]
    type FirstArgReplacement =
        | UsePreviousFile
        | ReplaceWith of string list

    let internal runRaw (firstArg: FirstArgReplacement) options (c: CreateProcess<'a>) =
        //let timeout = TimeSpan.MaxValue
        let results = System.Collections.Generic.List<ConsoleMessage>()

        let errorF msg =
            if options.PrintRedirectedOutput then
                Trace.traceError msg

            results.Add(ConsoleMessage.CreateError msg)

        let messageF msg =
            if options.PrintRedirectedOutput then
                Trace.trace msg

            results.Add(ConsoleMessage.CreateOut msg)

        let dir = Path.GetDirectoryName options.DotNetCliPath
        let oldPath = options |> Process.getEnvironmentVariable "PATH"

        let newArgs =
            match firstArg with
            | FirstArgReplacement.UsePreviousFile -> Arguments.withPrefix [ c.Command.Executable ] c.Command.Arguments
            | FirstArgReplacement.ReplaceWith args ->
                (Arguments.ofList args).ToStartInfo + " " + c.Command.Arguments.ToStartInfo
                |> Arguments.OfStartInfo

        let cmd = RawCommand(options.DotNetCliPath, newArgs)

        c
        |> CreateProcess.withCommand cmd
        |> (if c.WorkingDirectory.IsNone then
                CreateProcess.withWorkingDirectory options.WorkingDirectory
            else
                id)
        |> (match options.Timeout with
            | Some timeout -> CreateProcess.withTimeout timeout
            | None -> id)
        |> CreateProcess.withEnvironmentMap (EnvMap.ofMap options.Environment)
        |> CreateProcess.setEnvironmentVariable
            "PATH"
            (match oldPath with
             | Some oldPath -> sprintf "%s%c%s" dir Path.PathSeparator oldPath
             | None -> dir)
        |> CreateProcess.appendSimpleFuncs
            (fun _ -> withGlobalJsonDispose options.WorkingDirectory options.Version)
            (fun state p -> ())
            (fun prev state exitCode -> prev)
            (fun s -> s.Dispose())
        |> (if options.RedirectOutput then
                CreateProcess.redirectOutputIfNotRedirected
                >> CreateProcess.withOutputEventsNotNull messageF errorF
            else
                id)
        |> CreateProcess.map (fun prev -> prev, (results |> List.ofSeq))

    let internal run cmdArgs options : ProcessResult =
        CreateProcess.fromCommand (Command.RawCommand(options.DotNetCliPath, Arguments.ofList (List.ofSeq cmdArgs)))
        |> runRaw (FirstArgReplacement.ReplaceWith []) options
        |> CreateProcess.map (fun (r, results) -> ProcessResult.New r.ExitCode results)
        |> Proc.run

    let internal setOptions (buildOptions: Options -> Options) = buildOptions (Options.Create())

    /// <summary>
    /// Execute raw dotnet cli command
    /// </summary>
    ///
    /// <param name="buildOptions">build common execution options</param>
    /// <param name="command">the sdk command to execute <c>test</c>, <c>new</c>, <c>build</c>, ...</param>
    /// <param name="args">command arguments</param>
    let exec (buildOptions: Options -> Options) (command: string) (args: string) =
        let options = setOptions buildOptions

        let cmdArgs =
            buildCommand
                (command |> Args.fromWindowsCommandLine |> Seq.toList)
                (args |> Args.fromWindowsCommandLine |> Seq.toList)
                options

        run cmdArgs options

    /// <summary>
    ///   Execute raw dotnet cli command.
    ///   Similar to 'exec' but takes a string list instead of a single string.
    /// </summary>
    ///
    /// <param name="buildOptions">build common execution options</param>
    /// <param name="command">the sdk command to execute <c>test</c>, <c>new</c>, <c>build</c>, ...</param>
    /// <param name="args">command arguments</param>
    let private execArgsList (buildOptions: Options -> Options) (command: string) (args: string list) =
        let options = setOptions buildOptions

        let cmdArgs =
            buildCommand (command |> Args.fromWindowsCommandLine |> Seq.toList) args options

        run cmdArgs options

    /// <summary>
    /// Replace the current <c>CreateProcess</c> instance to run with dotnet.exe
    /// </summary>
    ///
    /// <param name="buildOptions">build common execution options</param>
    /// <param name="firstArg">the first argument (like t)</param>
    /// <param name="args">command arguments</param>
    let prefixProcess (buildOptions: Options -> Options) (firstArgs: string list) (c: CreateProcess<'a>) =
        let options = setOptions buildOptions

        c
        |> runRaw (FirstArgReplacement.ReplaceWith firstArgs) options
        |> CreateProcess.map fst

    /// <summary>
    /// Setup the environment (<c>PATH</c> and <c>DOTNET_ROOT</c>) in such a way that started processes use the given
    /// dotnet SDK installation. This is useful for example when using fable,
    /// see <a href="https://github.com/fsharp/FAKE/issues/2405">issue #2405</a>
    /// </summary>
    ///
    /// <param name="install">The SDK to use (result of <c>DotNet.install</c>)</param>
    let setupEnv (install: Options -> Options) =
        let options = setOptions install
        let dotnetTool = Path.GetFullPath options.DotNetCliPath
        let dotnetFolder = Path.GetDirectoryName dotnetTool
        let currentPath = Environment.environVar "PATH"

        match currentPath with
        | null
        | "" -> Environment.setEnvironVar "PATH" dotnetFolder
        | _ when not (currentPath.Contains dotnetFolder) ->
            Environment.setEnvironVar "PATH" (dotnetFolder + string Path.PathSeparator + currentPath)
        | _ -> ()

        let currentDotNetRoot = Environment.environVar "DOTNET_ROOT"

        let realFolder =
            if not Environment.isWindows then
#if !FX_NO_POSIX
                // resolve potential symbolic link to the real location
                // https://stackoverflow.com/questions/58326739/how-can-i-find-the-target-of-a-linux-symlink-in-c-sharp
                Mono.Unix.UnixPath.GetRealPath(dotnetTool) |> Path.GetDirectoryName
#else
                eprintf
                    "Setting 'DOTNET_ROOT' to '%s' this might be wrong as we didn't follow the symlink. Please upgrade to netcore."
                    dotnetFolder

                dotnetFolder
#endif
            else
                dotnetFolder

        if
            String.IsNullOrEmpty currentDotNetRoot
            || not (currentDotNetRoot.Contains realFolder)
        then
            Environment.setEnvironVar "DOTNET_ROOT" realFolder

    /// <summary>
    /// dotnet --info command options
    /// </summary>
    type InfoOptions =
        {
            /// Common tool options
            Common: Options
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create().WithRedirectOutput(true).WithPrintRedirectedOutput(false) }

        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = { x with Common = f x.Common }

    /// <summary>
    /// dotnet info result
    /// </summary>
    type InfoResult =
        {
            /// Common tool options
            RID: string
        }

    /// <summary>
    /// Execute dotnet --info command
    /// </summary>
    ///
    /// <param name="setParams">set info command parameters</param>
    let info setParams =
        use __ = Trace.traceTask "DotNet:info" "running dotnet --info"
        let param = InfoOptions.Create() |> setParams
        let args = "--info" // project (buildPackArgs param)
        let result = exec (fun _ -> param.Common) "" args

        let rawOutput =
            result.Results
            |> Seq.map (fun c -> sprintf "%s: %s" (if c.IsError then "stderr: " else "stdout: ") c.Message)
            |> fun s -> String.Join("\n", s)

        if not result.OK then
            failwithf "dotnet --info failed with code '%i': \n%s" result.ExitCode rawOutput

        let rid =
            result.Messages
            |> Seq.tryFind (fun m -> m.Contains "RID:")
            |> Option.map (fun line -> line.Split([| ':' |]).[1].Trim())

        if rid.IsNone then
            failwithf "could not read rid from output: \n%s" rawOutput

        __.MarkSuccess()
        { RID = rid.Value }


    /// <summary>
    /// dotnet --version command options
    /// </summary>
    type VersionOptions =
        {
            /// Common tool options
            Common: Options
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create().WithRedirectOutput true }

        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = { x with Common = f x.Common }

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            x.WithCommon(fun c -> { c with Environment = map })

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }


    /// <summary>
    /// dotnet info result
    /// </summary>
    type VersionResult = string

    /// <summary>
    /// Execute dotnet --version command
    /// </summary>
    ///
    /// <param name="setParams">set version command parameters</param>
    let getVersion setParams =
        use __ = Trace.traceTask "DotNet:version" "running dotnet --version"
        let param = VersionOptions.Create() |> setParams
        let args = "--version"
        let result = exec (fun _ -> param.Common) "" args

        if not result.OK then
            failwithf "dotnet --version failed with code %i" result.ExitCode

        let version = result.Messages |> String.separated "\n" |> String.trim

        if String.isNullOrWhiteSpace version then
            failwithf "could not read version from output: \n%s" (String.Join("\n", result.Messages))

        __.MarkSuccess()
        version

    /// <summary>
    /// Install .NET Core SDK if required
    /// </summary>
    ///
    /// <param name="setParams">set installation options</param>
    let install setParams : Options -> Options =
        let param = CliInstallOptions.Default |> setParams

        let dir = defaultArg param.CustomInstallDir defaultUserInstallDir

        let checkVersion, fromGlobalJson =
            match param.Version with
            | Version version -> Some version, false
            | CliVersion.Coherent -> None, false
            | CliVersion.Latest -> None, false
            | CliVersion.GlobalJson -> Some(getSDKVersionFromGlobalJson ()), true

        let dotnetInstallations =
            if param.ForceInstall then
                Seq.empty
            else
                findPossibleDotnetCliPaths (Some dir)

        let existingDotNet =
            match checkVersion with
            | Some version ->
                let passVersion = if fromGlobalJson then None else Some version

                withGlobalJson param.WorkingDirectory passVersion (fun () ->
                    dotnetInstallations
                    |> Seq.tryFind (fun dotnet ->
                        try
                            let result =
                                getVersion (fun opt ->
                                    opt.WithCommon(fun c ->
                                        { c with
                                            DotNetCliPath = dotnet
                                            Version = None }))

                            result = version
                        with e ->
                            if
                                not (e.Message.Contains "dotnet --info failed with code")
                                || not (e.Message.Contains "global.json")
                            then
                                Trace.traceFAKE
                                    "Retrieving version failed, assuming because it doesn't match global.json, error was: %O"
                                    e

                            false)),
                passVersion
            | None ->
                // Just take first if we found a result
                dotnetInstallations |> Seq.tryHead, None

        match existingDotNet with
        | Some dotnet, passVersion ->
            Trace.traceVerbose "Suitable dotnet installation found, skipping .NET SDK installer."

            (fun opt ->
                { opt with
                    DotNetCliPath = dotnet
                    Version = passVersion })
        | _ ->

            let passVersion = if fromGlobalJson then None else checkVersion
            let installScript = downloadInstaller param.InstallerOptions

            // check if existing processes exists:
            let dotnetExe =
                Path.Combine(dir, (if Environment.isUnix then "dotnet" else "dotnet.exe"))

            if Environment.isWindows && File.Exists(dotnetExe) then
                System.Diagnostics.Process.GetProcesses()
                |> Seq.filter (fun p ->
                    try
                        not p.HasExited
                    with _ ->
                        false)
                |> Seq.filter (fun p ->
                    try
                        Path.GetFullPath(Process.getFileName p).ToLowerInvariant() = Path.GetFullPath(dotnetExe)
                    with _ ->
                        false)
                |> Seq.iter Process.kill

                ()

            let exitCode =
                let args, fileName =
                    let installArgs = buildDotNetCliInstallArgs param

                    if Environment.isUnix then
                        let args = installArgs |> Arguments.withPrefix [ installScript ]
                        args, "bash" // Otherwise we need to set the executable flag!
                    else
                        let command = installArgs |> Arguments.withPrefix [ installScript ]

                        let args =
                            Arguments.Empty
                            |> Arguments.appendNotEmpty "-ExecutionPolicy" "ByPass"
                            |> Arguments.appendIf true "-NoProfile"
                            |> Arguments.appendIf true "-NoLogo"
                            |> Arguments.appendIf true "-NonInteractive"
                            // Note: The & is required when the script path contains spaces, see https://stackoverflow.com/questions/45760457/how-to-run-a-powershell-script-with-white-spaces-in-path-from-command-line
                            // powershell really is a waste of time
                            |> Arguments.appendNotEmpty
                                "-Command"
                                (sprintf "& %s; if (-not $?) { exit -1 };" command.ToWindowsCommandLine)

                        args, "powershell"

                let processResult =
                    CreateProcess.fromRawCommandLine fileName args.ToStartInfo
                    |> CreateProcess.withTimeout TimeSpan.MaxValue
                    |> CreateProcess.withWorkingDirectory (Path.GetDirectoryName fileName)
                    |> Proc.run

                processResult.ExitCode

            if exitCode <> 0 then
                // force download new installer script
                Trace.traceError ".NET Core SDK install failed, trying to re-download installer..."

                downloadInstaller (param.InstallerOptions >> (fun o -> { o with AlwaysDownload = true }))
                |> ignore

                failwithf ".NET Core SDK install failed with code %i" exitCode

            let exe = dir @@ (if Environment.isUnix then "dotnet" else "dotnet.exe")
            Trace.tracefn ".NET Core SDK installed to %s" exe

            (fun opt ->
                { opt with
                    DotNetCliPath = exe
                    Version = passVersion })

    /// <summary>
    /// dotnet restore command options
    /// </summary>
    type MSBuildOptions =
        {
            /// Common tool options
            Common: Options

            /// MSBuild parameters
            MSBuildParams: MSBuild.CliArguments
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create()
              MSBuildParams = MSBuild.CliArguments.Create() }

        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = { x with Common = f x.Common }

        /// Changes the "MSBuildParams" properties according to the given function
        member inline x.WithMSBuildParams f =
            { x with MSBuildParams = f x.MSBuildParams }

    let internal addBinaryLogger disableFakeBinLog args (common: Options) =
        // used for detection
        let callMsBuildExe args =
            let result =
                execArgsList
                    (fun _ ->
                        { RedirectOutput = true
                          PrintRedirectedOutput = true
                          DotNetCliPath = common.DotNetCliPath
                          Version = common.Version
                          Environment = common.Environment
                          WorkingDirectory = common.WorkingDirectory
                          Timeout = None
                          CustomParams = None
                          Verbosity = None
                          Diagnostics = false })
                    "msbuild"
                    args

            if not result.OK then
                failwithf "msbuild failed with exit code '%d'" result.ExitCode

            String.Join("\n", result.Messages)

        MSBuild.addBinaryLogger (common.DotNetCliPath + " msbuild") callMsBuildExe args disableFakeBinLog

    let internal buildAfterArgs args afterArgs =
        [ yield! args
          match afterArgs with
          | Some a ->
              yield "--"
              yield a
          | None -> () ]

    let internal execWithBinLog project common command args msBuildArgs afterArgs =
        let msbuildArgList = MSBuild.fromCliArguments msBuildArgs

        let binLogPath, args =
            addBinaryLogger msBuildArgs.DisableInternalBinLog (args @ msbuildArgList) common

        let args = buildAfterArgs args afterArgs

        let result = execArgsList (fun _ -> common) command args
        MSBuild.handleAfterRun (sprintf "dotnet %s" command) binLogPath result.ExitCode project

    let internal tryExecWithBinLog project common command args msBuildArgs afterArgs =
        let msbuildArgList = MSBuild.fromCliArguments msBuildArgs

        let binLogPath, args =
            addBinaryLogger msBuildArgs.DisableInternalBinLog (args @ msbuildArgList) common

        let args = buildAfterArgs args afterArgs

        let result = execArgsList (fun _ -> common) command args

        try
            MSBuild.handleAfterRun (sprintf "dotnet %s" command) binLogPath result.ExitCode project
            Choice1Of2 result
        with e ->
            Choice2Of2(e, result)

    /// <summary>
    /// Runs a MSBuild project
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildOptions</param>
    /// <param name="project">A string with the path to the project file to build.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// open Fake.DotNet
    ///     let setMsBuildParams (defaults:MSBuild.CliArguments) =
    ///             { defaults with
    ///                 Verbosity = Some(Quiet)
    ///                 Targets = ["Build"]
    ///                 Properties =
    ///                     [
    ///                         "Optimize", "True"
    ///                         "DebugSymbols", "True"
    ///                         "Configuration", "Release"
    ///                     ]
    ///              }
    ///     let setParams (defaults:DotNet.MSBuildOptions) =
    ///             { defaults with
    ///                 MSBuildParams = setMsBuildParams defaults.MSBuildParams
    ///              }
    ///
    ///     DotNet.msbuild setParams "./MySolution.sln"
    /// </code>
    /// </example>
    let msbuild setParams project =
        use __ = Trace.traceTask "DotNet:msbuild" project

        let param = MSBuildOptions.Create() |> setParams
        let args = [ project ]
        execWithBinLog project param.Common "msbuild" args param.MSBuildParams None
        __.MarkSuccess()

    // TODO: Make this API public? change return code?
    let internal msbuildWithResult setParams project =
        //use __ = Trace.traceTask "DotNet:msbuild" project

        let param = MSBuildOptions.Create() |> setParams
        let args = [ project ]

        let r =
            tryExecWithBinLog project param.Common "msbuild" args param.MSBuildParams None
        //__.MarkSuccess()
        r


    /// <summary>
    /// dotnet restore command options
    /// </summary>
    type RestoreOptions =
        {
            /// Common tool options
            Common: Options

            /// The runtime to restore for (seems added in RC4). Maybe a bug, but works.
            Runtime: string option

            /// Nuget feeds to search updates in. Use default if empty.
            Sources: string list

            /// Directory to install packages in (<c>--packages</c>).
            Packages: string list

            /// Path to the nuget configuration file (<c>nuget.config</c>).
            ConfigFile: string option

            /// No cache flag (<c>--no-cache</c>)
            NoCache: bool

            /// Only warning failed sources if there are packages meeting version requirement
            /// (<c>--ignore-failed-sources</c>)
            IgnoreFailedSources: bool

            /// Disables restoring multiple projects in parallel (<c>--disable-parallel</c>)
            DisableParallel: bool

            /// Other msbuild specific parameters
            MSBuildParams: MSBuild.CliArguments
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create()
              Sources = []
              Runtime = None
              Packages = []
              ConfigFile = None
              NoCache = false
              IgnoreFailedSources = false
              DisableParallel = false
              MSBuildParams = MSBuild.CliArguments.Create() }

        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = { x with Common = f x.Common }

    let private buildRestoreArgs (param: RestoreOptions) =
        [ param.Sources |> argList2 "source"
          param.Packages |> argList2 "packages"
          param.ConfigFile |> Option.toList |> argList2 "configfile"
          param.NoCache |> argOption "no-cache"
          param.Runtime |> Option.toList |> argList2 "runtime"
          param.IgnoreFailedSources |> argOption "ignore-failed-sources"
          param.DisableParallel |> argOption "disable-parallel" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)


    /// <summary>
    /// Execute dotnet restore command
    /// </summary>
    ///
    /// <param name="setParams">set restore command parameters</param>
    /// <param name="project">project to restore packages</param>
    let restore setParams project =
        use __ = Trace.traceTask "DotNet:restore" project
        let param = RestoreOptions.Create() |> setParams
        let args = project :: buildRestoreArgs param
        execWithBinLog project param.Common "restore" args param.MSBuildParams None
        __.MarkSuccess()

    /// build configuration
    type BuildConfiguration =
        | Debug
        | Release
        | Custom of string

        /// Convert the build configuration to a string that can be passed to the .NET CLI
        override this.ToString() =
            match this with
            | Debug -> "Debug"
            | Release -> "Release"
            | Custom config -> config

    /// <summary>
    /// THe build configuration for a DotNet application
    /// </summary>
    [<RequireQualifiedAccess>]
    module BuildConfiguration =
        /// Parse a build configuration string
        let fromString (s: string) =
            match s.ToLowerInvariant() with
            | "debug" -> Debug
            | "release" -> Release
            | _ -> Custom s

        /// Get the build configuration from an environment variable with the given name or returns
        /// the default if not value was set
        let fromEnvironVarOrDefault (name: string) (defaultValue: BuildConfiguration) =
            match Environment.environVarOrNone name with
            | Some config -> fromString config
            | None -> defaultValue

    let private buildConfigurationArg (param: BuildConfiguration) =
        argList2 "configuration" [ param.ToString() ]

    /// <summary>
    /// dotnet pack command options
    /// </summary>
    type PackOptions =
        {
            /// Common tool options
            Common: Options

            /// Pack configuration (<c>--configuration</c>)
            Configuration: BuildConfiguration

            /// Version suffix to use
            VersionSuffix: string option

            /// Build base path (<c>--build-base-path</c>)
            BuildBasePath: string option

            /// Output path (<c>--output</c>)
            OutputPath: string option

            /// Don't show copyright messages. (<c>--nologo</c>)
            NoLogo: bool

            /// No build flag (<c>--no-build</c>)
            NoBuild: bool

            /// Doesn't execute an implicit restore when running the command. (<c>--no-restore</c>)
            NoRestore: bool

            /// Other msbuild specific parameters
            MSBuildParams: MSBuild.CliArguments

            /// Includes the debug symbols NuGet packages in addition to the regular NuGet packages in the output
            /// directory (<c>--include-symbols</c>)
            IncludeSymbols: bool
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create()
              Configuration = Release
              VersionSuffix = None
              BuildBasePath = None
              OutputPath = None
              NoLogo = false
              NoBuild = false
              NoRestore = false
              MSBuildParams = MSBuild.CliArguments.Create()
              IncludeSymbols = false }

        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = { x with Common = f x.Common }

    let private buildPackArgs (param: PackOptions) =
        [ buildConfigurationArg param.Configuration
          param.VersionSuffix |> Option.toList |> argList2 "version-suffix"
          param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
          param.OutputPath |> Option.toList |> argList2 "output"
          param.NoLogo |> argOption "nologo"
          param.NoBuild |> argOption "no-build"
          param.NoRestore |> argOption "no-restore"
          param.IncludeSymbols |> argOption "include-symbols" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)


    /// <summary>
    /// Execute dotnet pack command
    /// </summary>
    ///
    /// <param name="setParams">set pack command parameters</param>
    /// <param name="project">project to pack</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    ///  let packConfiguration (defaults:DotNet.PackOptions) =
    ///         { defaults with
    ///               Configuration = DotNet.Debug
    ///               OutputPath = Some "./packages"
    ///               IncludeSymbols = true }
    ///
    ///     DotNet.pack packConfiguration "./MyProject.csproj"
    /// </code>
    /// </example>
    let pack setParams project =
        use __ = Trace.traceTask "DotNet:pack" project
        let param = PackOptions.Create() |> setParams
        let args = project :: buildPackArgs param
        execWithBinLog project param.Common "pack" args param.MSBuildParams None
        __.MarkSuccess()

    /// <summary>
    /// dotnet publish command options
    /// </summary>
    type PublishOptions =
        {
            /// Common tool options
            Common: Options

            /// Pack configuration (<c>--configuration</c>)
            Configuration: BuildConfiguration

            /// Target framework to compile for (<c>--framework</c>)
            Framework: string option

            /// Target runtime to publish for (<c>--runtime</c>)
            Runtime: string option

            /// Build base path (<c>--build-base-path</c>)
            BuildBasePath: string option

            /// Output path (<c>--output</c>)
            OutputPath: string option

            /// Defines what <c>*</c> should be replaced with in version field in project.json
            /// (<c>--version-suffix</c>)
            VersionSuffix: string option

            /// Specifies one or several target manifests to use to trim the set of packages published with the app.
            /// The manifest file is part of the output of the dotnet store command.
            /// This option is available starting with .NET Core 2.0 SDK. (<c>--manifest</c>)
            Manifest: string list option

            /// Publish the .NET Core runtime with your application so the runtime doesn't need to be installed on
            /// the target machine. The default is 'true' if a runtime identifier is specified.
            /// (<c>--self-contained</c>)
            SelfContained: bool option

            /// Don't show copyright messages. (<c>--nologo</c>)
            NoLogo: bool

            /// No build flag (<c>--no-build</c>)
            NoBuild: bool

            /// Doesn't execute an implicit restore when running the command. (<c>--no-restore</c>)
            NoRestore: bool

            /// Force all dependencies to be resolved even if the last restore was successful.
            /// This is equivalent to deleting project.assets.json. (<c>--force</c>)
            Force: bool option

            /// Other msbuild specific parameters
            MSBuildParams: MSBuild.CliArguments
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create()
              Configuration = Release
              Framework = None
              Runtime = None
              BuildBasePath = None
              OutputPath = None
              VersionSuffix = None
              NoLogo = false
              NoBuild = false
              NoRestore = false
              Force = None
              SelfContained = None
              Manifest = None
              MSBuildParams = MSBuild.CliArguments.Create() }

        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = { x with Common = f x.Common }

    let internal buildPublishArgs (param: PublishOptions) =
        [ buildConfigurationArg param.Configuration
          param.Framework |> Option.toList |> argList2 "framework"
          param.Runtime |> Option.toList |> argList2 "runtime"
          param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
          param.OutputPath |> Option.toList |> argList2 "output"
          param.VersionSuffix |> Option.toList |> argList2 "version-suffix"
          param.Manifest |> Option.toList |> List.collect id |> argList2 "manifest"
          param.NoLogo |> argOption "nologo"
          param.NoBuild |> argOption "no-build"
          param.NoRestore |> argOption "no-restore"
          param.SelfContained
          |> Option.map (argOptionExplicit "self-contained")
          |> Option.defaultValue []
          param.Force |> Option.map (argOption "force") |> Option.defaultValue [] ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)


    /// <summary>
    /// Execute dotnet publish command
    /// </summary>
    ///
    /// <param name="setParams">set publish command parameters</param>
    /// <param name="project">project to publish</param>
    let publish setParams project =
        use __ = Trace.traceTask "DotNet:publish" project
        let param = PublishOptions.Create() |> setParams
        let args = project :: buildPublishArgs param
        execWithBinLog project param.Common "publish" args param.MSBuildParams None
        __.MarkSuccess()

    /// <summary>
    /// dotnet build command options
    /// </summary>
    type BuildOptions =
        {
            /// Common tool options
            Common: Options

            /// Pack configuration (<c>--configuration</c>)
            Configuration: BuildConfiguration

            /// Target framework to compile for (<c>--framework</c>)
            Framework: string option

            /// Target runtime to publish for (<c>--runtime</c>)
            Runtime: string option

            /// Build base path (<c>--build-base-path</c>)
            BuildBasePath: string option

            /// Output path (<c>--output</c>)
            OutputPath: string option

            /// Native flag (<c>--native</c>)
            Native: bool

            /// Don't show copyright messages. (<c>--nologo</c>)
            NoLogo: bool

            /// Doesn't execute an implicit restore during build. (<c>--no-restore</c>)
            NoRestore: bool

            /// Other msbuild specific parameters
            MSBuildParams: MSBuild.CliArguments
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create()
              Configuration = Release
              Framework = None
              Runtime = None
              BuildBasePath = None
              OutputPath = None
              Native = false
              NoLogo = false
              NoRestore = false
              MSBuildParams = MSBuild.CliArguments.Create() }

        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = { x with Common = f x.Common }


    let private buildBuildArgs (param: BuildOptions) =
        [ buildConfigurationArg param.Configuration
          param.Framework |> Option.toList |> argList2 "framework"
          param.Runtime |> Option.toList |> argList2 "runtime"
          param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
          param.OutputPath |> Option.toList |> argList2 "output"
          param.Native |> argOption "native"
          param.NoLogo |> argOption "nologo"
          param.NoRestore |> argOption "no-restore" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)


    /// <summary>
    /// Execute dotnet build command
    /// </summary>
    ///
    /// <param name="setParams">set compile command parameters</param>
    /// <param name="project">project to compile</param>
    let build setParams project =
        use __ = Trace.traceTask "DotNet:build" project
        let param = BuildOptions.Create() |> setParams
        let args = project :: buildBuildArgs param
        execWithBinLog project param.Common "build" args param.MSBuildParams None
        __.MarkSuccess()

    /// <summary>
    /// dotnet test command options
    /// </summary>
    type TestOptions =
        {
            /// Common tool options
            Common: Options

            /// Settings to use when running tests (<c>--settings</c>)
            Settings: string option

            /// Lists discovered tests (<c>--list-tests</c>)
            ListTests: bool

            /// Run tests that match the given expression. (<c>--filter</c>)
            /// Examples:
            /// <list type="number">
            /// <item>
            /// Run tests with priority set to 1: <c>--filter "Priority = 1"</c>
            /// </item>
            /// <item>
            /// Run a test with the specified full name:
            /// <c>--filter "FullyQualifiedName=Namespace.ClassName.MethodName"</c>
            /// </item>
            /// <item>
            /// Run tests that contain the specified name: <c>--filter "FullyQualifiedName~Namespace.Class"</c>
            /// </item>
            /// <item>
            /// More info on filtering support: https://aka.ms/vstest-filtering
            /// </item>
            /// </list>
            Filter: string option

            /// Use custom adapters from the given path in the test run. (<c>--test-adapter-path</c>)
            TestAdapterPath: string option

            /// Specify a logger for test results. (<c>--logger</c>)
            Logger: string option

            ///Configuration to use for building the project.  Default for most projects is  "Debug".
            /// (<c>--configuration</c>)
            Configuration: BuildConfiguration

            /// Target framework to publish for. The target framework has to be specified in the project file.
            /// (<c>--framework</c>)
            Framework: string option

            ///  Directory in which to find the binaries to be run (<c>--output</c>)
            Output: string option

            /// Enable verbose logs for test platform. Logs are written to the provided file. (<c>--diag</c>)
            Diag: string option

            /// Don't show copyright messages. (<c>--nologo</c>)
            NoLogo: bool

            ///  Do not build project before testing. (<c>--no-build</c>)
            NoBuild: bool

            /// The directory where the test results are going to be placed. The specified directory will be created
            /// if it does not exist. (<c>--results-directory</c>)
            ResultsDirectory: string option

            /// Enables data collector for the test run. More info here : https://aka.ms/vstest-collect
            /// (<c>--collect</c>)
            Collect: string option

            ///  Does not do an implicit restore when executing the command. (<c>--no-restore</c>)
            NoRestore: bool

            /// Arguments to pass run settings configurations through commandline. Arguments may be specified as
            /// name-value pair of the form <c>[name]=[value]</c> after <c>"-- "</c>. Note the space after <c>--</c>.
            RunSettingsArguments: string option

            /// Runs the tests in blame mode. This option is helpful in isolating the problematic tests causing test
            /// host to crash. It creates an output file in the current directory as Sequence.xml that captures the
            /// order of tests execution before the crash.  (<c>--blame</c>)
            Blame: bool

            /// Other msbuild specific parameters
            MSBuildParams: MSBuild.CliArguments
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create()
              Settings = None
              ListTests = false
              Filter = None
              TestAdapterPath = None
              Logger = None
              Configuration = BuildConfiguration.Debug
              Framework = None
              Output = None
              Diag = None
              NoLogo = false
              NoBuild = false
              ResultsDirectory = None
              Collect = None
              NoRestore = false
              RunSettingsArguments = None
              Blame = false
              MSBuildParams = MSBuild.CliArguments.Create() }

        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = { x with Common = f x.Common }

    let private buildTestArgs (param: TestOptions) =
        [ param.Settings |> Option.toList |> argList2 "settings"
          param.ListTests |> argOption "list-tests"
          param.Filter |> Option.toList |> argList2 "filter"
          param.TestAdapterPath |> Option.toList |> argList2 "test-adapter-path"
          param.Logger |> Option.toList |> argList2 "logger"
          buildConfigurationArg param.Configuration
          param.Framework |> Option.toList |> argList2 "framework"
          param.Output |> Option.toList |> argList2 "output"
          param.Diag |> Option.toList |> argList2 "diag"
          param.NoLogo |> argOption "nologo"
          param.NoBuild |> argOption "no-build"
          param.ResultsDirectory |> Option.toList |> argList2 "results-directory"
          param.Collect |> Option.toList |> argList2 "collect"
          param.NoRestore |> argOption "no-restore"
          param.Blame |> argOption "blame" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)


    /// <summary>
    /// Execute dotnet test command
    /// </summary>
    ///
    /// <param name="setParams">set test command parameters</param>
    /// <param name="project">project to test</param>
    let test setParams project =
        use __ = Trace.traceTask "DotNet:test" project
        let param = TestOptions.Create() |> setParams
        let args = project :: buildTestArgs param
        execWithBinLog project param.Common "test" args param.MSBuildParams param.RunSettingsArguments
        __.MarkSuccess()

    let internal buildNugetPushArgs (param: NuGet.NuGetPushParams) =
        [ param.DisableBuffering |> argOption "disable-buffering"
          param.ApiKey |> Option.toList |> argList2 "api-key"
          param.NoSymbols |> argOption "no-symbols"
          param.NoServiceEndpoint |> argOption "no-service-endpoint"
          param.Source |> Option.toList |> argList2 "source"
          param.SymbolApiKey |> Option.toList |> argList2 "symbol-api-key"
          param.SymbolSource |> Option.toList |> argList2 "symbol-source"
          param.Timeout
          |> Option.map (fun t -> t.TotalSeconds |> int |> string)
          |> Option.toList
          |> argList2 "timeout" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)

    /// <summary>
    /// nuget push parameters for <c>dotnet nuget push</c>
    /// </summary>
    type NuGetPushOptions =
        { Common: Options
          PushParams: NuGet.NuGetPushParams }

        static member Create() =
            { Common = Options.Create()
              PushParams = NuGet.NuGetPushParams.Create() }

        member this.WithCommon(common: Options) = { this with Common = common }
        member this.WithPushParams(options: NuGet.NuGetPushParams) = { this with PushParams = options }

    /// <summary>
    /// Execute dotnet nuget push command
    /// </summary>
    ///
    /// <param name="setParams">set nuget push command parameters</param>
    /// <param name="nupkg">nupkg to publish</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// open Fake.DotNet
    ///     let setNugetPushParams (defaults:NuGet.NuGetPushParams) =
    ///             { defaults with
    ///                 DisableBuffering = true
    ///                 ApiKey = Some "abc123"
    ///              }
    ///     let setParams (defaults:DotNet.NuGetPushOptions) =
    ///             { defaults with
    ///                 PushParams = setNugetPushParams defaults.PushParams
    ///              }
    ///
    ///     DotNet.nugetPush setParams "./My.Package.nupkg"
    /// </code>
    /// </example>
    let rec nugetPush setParams nupkg =
        use __ = Trace.traceTask "DotNet:nuget:push" nupkg
        let param = NuGetPushOptions.Create() |> setParams
        let pushParams = param.PushParams

        pushParams.ApiKey
        |> Option.iter (fun key -> TraceSecrets.register "<ApiKey>" key)

        pushParams.SymbolApiKey
        |> Option.iter (fun key -> TraceSecrets.register "<SymbolApiKey>" key)

        let args = nupkg :: buildNugetPushArgs pushParams
        let result = execArgsList (fun _ -> param.Common) "nuget push" args

        if result.OK then
            __.MarkSuccess()
        elif pushParams.PushTrials > 0 then
            nugetPush (fun _ -> param.WithPushParams { pushParams with PushTrials = pushParams.PushTrials - 1 }) nupkg
        else
            failwithf "dotnet nuget push failed with code %i" result.ExitCode

    /// The languages supported by new command
    type NewLanguage =
        | FSharp
        | CSharp
        | VisualBasic

        /// Convert the list option to string representation
        override this.ToString() =
            match this with
            | FSharp -> "F#"
            | CSharp -> "C#"
            | VisualBasic -> "VB"

    /// <summary>
    /// dotnet new command options
    /// </summary>
    type NewOptions =
        {
            /// Common tool options
            Common: Options

            // Displays a summary of what would happen if the given command line were run if it would result in a
            // template creation.
            DryRun: bool

            // Forces content to be generated even if it would change existing files.
            Force: bool

            // Filters templates based on language and specifies the language of the template to create.
            Language: NewLanguage

            // The name for the created output. If no name is specified, the name of the current directory is used.
            Name: string option

            // Disables checking for template package updates when instantiating a template.
            NoUpdateCheck: bool

            // Location to place the generated output. The default is the current directory.
            Output: string option
        }

        /// Parameter default values.
        static member Create() =
            { Common = Options.Create()
              DryRun = false
              Force = false
              Language = NewLanguage.FSharp
              Name = None
              NoUpdateCheck = false
              Output = None }

    /// <summary>
    /// dotnet new --install options
    /// </summary>
    type TemplateInstallOptions =
        {
            /// Common tool options
            Common: Options
            Install: string
            NugetSource: string option
        }

        /// Parameter default values.
        static member Create(packageOrSourceName) =
            { Common = Options.Create()
              Install = packageOrSourceName
              NugetSource = None }

    /// <summary>
    /// dotnet new --install options
    /// </summary>
    type TemplateUninstallOptions =
        {
            /// Common tool options
            Common: Options
            Uninstall: string
        }

        /// Parameter default values.
        static member Create(packageOrSourceName) =
            { Common = { Options.Create() with RedirectOutput = true }
              Uninstall = packageOrSourceName }

    let internal buildNewArgs (param: NewOptions) =
        [ param.DryRun |> argOption "dry-run"
          param.Force |> argOption "force"
          argList2 "language" [ param.Language.ToString() ]
          param.Name |> Option.toList |> argList2 "name"
          param.NoUpdateCheck |> argOption "no-update-check"
          param.Output |> Option.toList |> argList2 "output" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)

    let internal buildTemplateInstallArgs (param: TemplateInstallOptions) =
        [ argList2 "install" [ param.Install ]
          param.NugetSource |> Option.toList |> argList2 "nuget-source" ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)

    let internal buildTemplateUninstallArgs (param: TemplateUninstallOptions) =
        [ argList2 "uninstall" [ param.Uninstall ] ]
        |> List.concat
        |> List.filter (not << String.IsNullOrEmpty)

    /// <summary>
    /// Execute dotnet new command
    /// </summary>
    ///
    /// <param name="templateName">template short name to create from</param>
    /// <param name="setParams">set version command parameters</param>
    let newFromTemplate templateName setParams =
        use __ = Trace.traceTask "DotNet:new" "dotnet new command"
        let param = NewOptions.Create() |> setParams
        let args = buildNewArgs param
        let result = execArgsList (fun _ -> param.Common) $"new {templateName}" args

        if not result.OK then
            failwithf $"dotnet new failed with code %i{result.ExitCode}"

        __.MarkSuccess()

    /// <summary>
    /// Execute dotnet new <c>--install &lt;PATH|NUGET_ID&gt;</c> command to install a new template
    /// </summary>
    ///
    /// <param name="templateName">template short name to install</param>
    /// <param name="setParams">set version command parameters</param>
    let installTemplate templateName setParams =
        use __ = Trace.traceTask "DotNet:new" "dotnet new --install command"
        let param = TemplateInstallOptions.Create(templateName) |> setParams
        let args = buildTemplateInstallArgs param
        let result = execArgsList (fun _ -> param.Common) "new" args

        if not result.OK then
            failwithf $"dotnet new --install failed with code %i{result.ExitCode}"

        __.MarkSuccess()

    /// <summary>
    /// Execute dotnet new <c>--uninstall &lt;PATH|NUGET_ID&gt;</c> command to uninstall a new template
    /// </summary>
    ///
    /// <param name="templateName">template short name to uninstall</param>
    /// <param name="setParams">set version command parameters</param>
    let uninstallTemplate templateName =
        use __ = Trace.traceTask "DotNet:new" "dotnet new --uninstall command"
        let param = TemplateUninstallOptions.Create(templateName)
        let args = buildTemplateUninstallArgs param
        let result = execArgsList (fun _ -> param.Common) "new" args

        // If the process returns error (exit code != 0) then check to see if a message is
        // that the template was not found.  If this message exists, assume the process
        // completed with success
        let templateIsNotFoundToUninstall =
            result.Results
            |> List.exists (fun (result: ConsoleMessage) ->
                result.Message.Contains $"The template package '{templateName}' is not found.")

        let success = (result.ExitCode = 0) || templateIsNotFoundToUninstall

        match success with
        | true -> ()
        | false -> failwithf $"dotnet new --uninstall failed with code %i{result.ExitCode}"

        __.MarkSuccess()
