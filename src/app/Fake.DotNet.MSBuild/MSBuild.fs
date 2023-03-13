namespace Fake.DotNet

open System
open System.IO
open System.Text
open System.Xml.Linq
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

/// <summary>
/// A type to represent MSBuild project files.
/// </summary>
type MSBuildProject = XDocument

/// <summary>
/// An exception type to signal build errors.
/// </summary>
exception MSBuildException of string * list<string> with
    override x.ToString() =
        x.Data0.ToString()
        + Environment.NewLine
        + (String.separated Environment.NewLine x.Data1)

type MSBuildEntry = { Version: string; Paths: string list }

/// MSBuild verbosity option
type MSBuildVerbosity =
    | Quiet
    | Minimal
    | Normal
    | Detailed
    | Diagnostic

/// <summary>
/// MSBuild log option
/// See <a href="https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference?view=vs-2015">
/// msbuild-command-line-reference</a>
/// </summary>
type MSBuildLogParameter =
    /// Determines whether the build log is appended to the log file or overwrites it. When you set the switch,
    /// the build log is appended to the log file. When the switch is not present, the contents of an existing
    /// log file are overwritten. If you include the append switch, no matter whether it is set to true or false,
    /// the log is appended. If you do not include the append switch, the log is overwritten.
    | Append
    /// Show the time that’s spent in tasks, targets, and projects.
    | PerformanceSummary
    /// Show the error and warning summary at the end.
    | Summary
    /// Don't show the error and warning summary at the end.
    | NoSummary
    /// Show only errors.
    | ErrorsOnly
    /// Show only warnings.
    | WarningsOnly
    /// Don't show the list of items and properties that would appear at the start of each project build if the
    /// verbosity level is set to <c>diagnostic</c>.
    | NoItemAndPropertyList
    /// Show <c>TaskCommandLineEvent</c> messages.
    | ShowCommandLine
    /// Show the timestamp as a prefix to any message.
    | ShowTimestamp
    /// Show the event ID for each started event, finished event, and message.
    | ShowEventId
    /// Don't align the text to the size of the console buffer.
    | ForceNoAlign
    /// Use the default console colors for all logging messages.
    | DisableConsoleColor
    /// Undocumented switch to force ansi colors.
    | ForceConsoleColor
    /// Disable the multiprocessor logging style of output when running in non-multiprocessor mode.
    | DisableMPLogging
    /// Enable the multiprocessor logging style even when running in non-multiprocessor mode.
    /// This logging style is on by default.
    | EnableMPLogging
    /// Other currently not supported parameter.
    | LogParameter of string

/// <summary>
/// A type for MSBuild configuration
/// </summary>
type MSBuildFileLoggerConfig =
    { Number: int
      Filename: string option
      Verbosity: MSBuildVerbosity option
      Parameters: MSBuildLogParameter list option }

/// <summary>
/// A type for MSBuild distributed logger configuration
/// </summary>
type MSBuildDistributedLoggerConfig =
    { ClassName: string option
      AssemblyPath: string
      Parameters: (string * string) list option }

type MSBuildLoggerConfig = MSBuildDistributedLoggerConfig

module private MSBuildExeFromVsWhere =
    open BlackFox.VsWhere
    open System.Diagnostics

    let private getAllVsPath () =
        VsInstances.getWithPackage "Microsoft.Component.MSBuild" true
        |> List.map (fun vs -> vs.InstallationPath)

    let private getAllMsBuildPaths vsPath =
        let msBuildDir = Path.Combine(vsPath, "MSBuild")

        if Directory.Exists(msBuildDir) then
            Directory.EnumerateDirectories(msBuildDir)
            |> Seq.map (fun dir -> Path.Combine(dir, "Bin", "MSBuild.exe"))
            |> Seq.choose (fun exe ->
                if File.Exists(exe) then
                    let v = FileVersionInfo.GetVersionInfo(exe)

                    Some
                        {| IsPreRelease = v.IsPreRelease
                           FileMajorPart = v.FileMajorPart
                           Path = Path.GetDirectoryName(exe) |}
                else
                    None)
            |> List.ofSeq
        else
            []

    let private all =
        lazy
            (getAllVsPath ()
             |> List.collect getAllMsBuildPaths
             |> List.sortBy (fun x -> x.IsPreRelease)
             |> List.groupBy (fun x -> x.FileMajorPart)
             |> List.sortByDescending fst
             |> List.map (fun (v, dirs) ->
                 { Version = sprintf "%d.0" v
                   Paths = dirs |> List.map (fun x -> x.Path) }))

    let getOrdered () : MSBuildEntry list = all.Value

module private MSBuildExe =
    let knownMSBuildEntries =
        [ { Version = "17.0"
            Paths =
              [ @"\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin"
                @"\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin"
                @"\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin"
                @"\MSBuild\Current\Bin"
                @"\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin" ] }
          { Version = "16.0"
            Paths =
              [ @"\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin"
                @"\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin"
                @"\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin"
                @"\MSBuild\Current\Bin"
                @"\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin" ] }
          { Version = "15.0"
            Paths =
              [ @"\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"
                @"\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin"
                @"\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin"
                @"\MSBuild\15.0\Bin"
                @"\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin" ] }
          { Version = "14.0"
            Paths = [ @"\MSBuild\14.0\Bin" ] }
          { Version = "12.0"
            Paths = [ @"\MSBuild\12.0\Bin"; @"\MSBuild\12.0\Bin\amd64" ] } ]

    let oldMSBuildLocations =
        [ @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\"
          @"c:\Windows\Microsoft.NET\Framework\v4.0.30128\"
          @"c:\Windows\Microsoft.NET\Framework\v3.5\" ]

    let private toDict items =
        items |> Seq.map (fun f -> f.Version, f.Paths) |> Map.ofSeq

    let private getAllKnownPaths =
        (knownMSBuildEntries |> List.collect (fun m -> m.Paths)) @ oldMSBuildLocations

    /// <summary>
    /// Versions of Mono prior to this one have faulty implementations of MSBuild
    /// NOTE: in System.Version 5.0 &gt;= 5.0.0.0 is false while 5.0.0.0 &gt;= 5.0 is true...
    /// </summary>
    let monoVersionToUseMSBuildOn = Version("5.0")

    /// <summary>
    /// Tries to detect the right version of MSBuild.
    /// </summary>
    ///
    /// <list type="number">
    /// <item>
    /// On all OS's, we check a <c>MSBuild</c> environment variable which is either
    /// <list type="number">
    /// <item>
    /// a direct path to a file to use, or
    /// </item>
    /// <item>
    /// a directory that contains a file called
    /// <list type="number">
    /// <item>
    /// <c>msbuild</c> on non-Windows systems with mono &gt;= 5.0.0.0, or
    /// </item>
    /// <item>
    /// <c>xbuild</c> on non-Windows systems with mono &lt; 5.0.0.0,
    /// </item>
    /// <item>
    /// <c>MSBuild.exe</c> on Windows systems, or
    /// </item>
    /// </list>
    /// </item>
    /// <item>
    /// a tool that exists on the current PATH
    /// </item>
    /// </list>
    /// </item>
    /// <item>
    /// In addition, on non-Windows systems we check the current PATH for the following binaries, in this order:
    /// <list type="number">
    /// <item>
    /// Mono &gt;= 5.0.0.0: <c>msbuild</c>, <c>xbuild</c>
    /// </item>
    /// <item>
    /// Mono &lt; 5.0.0.0: <c>xbuild</c>, <c>msbuild</c>
    /// </item>
    /// <item>
    /// This is due to several known issues in the Mono &lt; 5.0 implementation of MSBuild.
    /// </item>
    /// </list>
    /// </item>
    /// <item>
    /// In addition, on Windows systems we
    /// <list type="number">
    /// <item>
    /// try to read the MSBuild tool location from the AppSettings file using a parameter named <c>MSBuild</c>,
    /// and finally
    /// </item>
    /// <item>
    /// if a <c>VisualStudioVersion</c> environment variable is specified, we try to use the specific MSBuild version,
    /// matching that Visual Studio version.
    /// </item>
    /// </list>
    /// </item>
    /// </list>
    let msBuildExe =
        // the value we're given can be a:
        //     * full path to a file or
        //     * just a directory
        // if just a directory we can make it the path to a file by Path-Combining the tool name to the directory.
        let exactPathOrBinaryOnPath tool input =
            if Path.isDirectory input && Directory.Exists input then
                input </> tool
            else
                input

        let which tool = ProcessUtils.tryFindFileOnPath tool
        let msbuildEnvironVar = Environment.environVarOrNone "MSBuild"

        let preferMSBuildOnNetCore =
            if not Environment.isUnix || Environment.isMono then
                false
            else
                match Mono.monoVersion with
                | Some (_, Some (version)) when version >= monoVersionToUseMSBuildOn -> true
                | _ -> false

        let preferMSBuildOnMono =
            match Environment.monoVersion with
            | Some (_, Some (version)) when version >= monoVersionToUseMSBuildOn -> true
            | _ -> false

        let foundExe =
            match Environment.isUnix, preferMSBuildOnNetCore || preferMSBuildOnMono with
            | true, true ->
                let sources =
                    [ msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "msbuild")
                      msbuildEnvironVar |> Option.bind which
                      which "msbuild"
                      which "xbuild" ]

                defaultArg (sources |> List.choose id |> List.tryHead) "msbuild"
            | true, _ ->
                let sources =
                    [ msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "xbuild")
                      msbuildEnvironVar |> Option.bind which
                      which "xbuild"
                      which "msbuild" ]

                defaultArg (sources |> List.choose id |> List.tryHead) "xbuild"
            | false, _ ->

                let configIgnoreMSBuild =
#if !FX_NO_SYSTEM_CONFIGURATION
                    if
                        "true"
                            .Equals(
                                System.Configuration.ConfigurationManager.AppSettings.["IgnoreMSBuild"],
                                StringComparison.OrdinalIgnoreCase
                            )
                    then
                        Some ""
                    else
#endif
                    None

                let findOnVSPathsThenSystemPath =
                    let visualStudioVersion = Environment.environVarOrNone "VisualStudioVersion"

                    // with VS 2022 Visual Studio can also be installed in "Program Files" instead of "Program Files (x86)"
                    // so we need to search both paths for every version of Visual Studio
                    let withProgramFiles paths =
                        (paths |> List.map ((@@) Fake.Core.Environment.ProgramFilesX86))
                        @ (paths |> List.map ((@@) Fake.Core.Environment.ProgramFiles))

                    let vsVersionPaths =
                        let dict = toDict knownMSBuildEntries

                        match
                            Fake.Core.Environment.environVarOrNone "VisualStudioVersion"
                            |> Option.bind dict.TryFind
                        with
                        | Some x -> x |> withProgramFiles
                        | None ->
                            (knownMSBuildEntries |> List.collect (fun x -> x.Paths |> withProgramFiles))
                            @ oldMSBuildLocations

                    let vsWhereVersionPaths =
                        let orderedVersions = MSBuildExeFromVsWhere.getOrdered ()
                        let all = orderedVersions |> List.collect (fun e -> e.Paths)
                        let dict = toDict orderedVersions
                        defaultArg (visualStudioVersion |> Option.bind dict.TryFind) all

                    let fullList = vsWhereVersionPaths @ vsVersionPaths |> List.distinct

                    ProcessUtils.tryFindFile fullList "MSBuild.exe"

                let sources =
                    [ msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "MSBuild.exe")
                      msbuildEnvironVar |> Option.bind which
                      configIgnoreMSBuild
                      findOnVSPathsThenSystemPath ]

                defaultArg (sources |> List.choose id |> List.tryHead) "MSBuild.exe"

        if foundExe.Contains @"\BuildTools\" then
            Trace.traceFAKE
                "If you encounter msbuild errors make sure you have copied the required SDKs, see https://github.com/Microsoft/msbuild/issues/1697"
        elif foundExe.Contains @"\2017\" then
            Trace.logVerbosefn
                "Using msbuild of VS2017 (%s), if you encounter build errors make sure you have installed the necessary workflows!"
                foundExe
        elif foundExe.Contains @"\2019\" then
            Trace.logVerbosefn
                "Using msbuild of VS2019 (%s), if you encounter build errors make sure you have installed the necessary workflows!"
                foundExe
        elif foundExe.Contains @"\2022\" then
            Trace.logVerbosefn
                "Using msbuild of VS2022 (%s), if you encounter build errors make sure you have installed the necessary workflows!"
                foundExe

        foundExe

/// <summary>
/// A type for MSBuild task parameters
/// Please see <a href="https://docs.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2015/msbuild/msbuild-command-line-reference?view=vs-2015">MSBuild command line reference</a>
/// </summary>
type MSBuildParams =
    {
        /// Set the MSBuild executable to use. Defaults to the latest installed MSBuild.
        ToolPath: string

        /// The working directory to execute MSBuild in
        WorkingDirectory: string

        /// The list of targets to use
        Targets: string list

        /// The list of properties to pass to MSBuild
        Properties: (string * string) list

        /// <summary>
        /// corresponds to the msbuild option <c>/m</c>:
        /// <list type="number">
        /// <item>
        /// <c>None</c> will omit the option.
        /// </item>
        /// <item>
        /// <c>Some None</c> will emit <c>/m</c>.
        /// </item>
        /// <item>
        /// <c>Some 2</c> will emit <c>/m:2</c>.
        /// </item>
        /// </list>
        /// </summary>
        MaxCpuCount: int option option

        /// Execute a restore before executing the targets (<c>/restore</c> flag)
        DoRestore: bool

        /// Don't display the startup banner or the copyright message.
        NoLogo: bool

        /// Enable or disable the re-use of MSBuild nodes. You can specify the following values
        NodeReuse: bool

        /// mark if to restore the packages or not
        RestorePackagesFlag: bool

        /// Specifies the version of the Toolset to use to build the project
        ToolsVersion: string option

        /// Specifies the amount of information to display in the build log. Each logger displays events based on
        /// the verbosity level that you set for that logger
        Verbosity: MSBuildVerbosity option

        /// Disable the default console logger, and don't log events to the console.
        NoConsoleLogger: bool

        /// The list of warnings to treat as errors
        WarnAsError: string list option

        /// The list of warning to ignore
        NoWarn: string list option

        /// corresponds to the msbuild option <c>/consoleloggerparameters</c>
        ConsoleLogParameters: MSBuildLogParameter list

        /// Fake attaches a binlog-logger in order to report errors and warnings. You can disable this behavior
        /// with this flag
        DisableInternalBinLog: bool

        /// corresponds to the msbuild option <c>/fl</c>
        FileLoggers: MSBuildFileLoggerConfig list option

        /// corresponds to the msbuild option <c>/bl</c>
        BinaryLoggers: string list option

        /// corresponds to the msbuild option <c>/l</c>
        Loggers: MSBuildLoggerConfig list option

        /// corresponds to the msbuild option <c>/dl</c>
        DistributedLoggers: (MSBuildLoggerConfig * MSBuildLoggerConfig option) list option

        Environment: Map<string, string>
    }

    /// Defines a default for MSBuild task parameters
    static member Create() =
        { ToolPath = MSBuildExe.msBuildExe
          Targets = []
          WorkingDirectory = Directory.GetCurrentDirectory()
          Properties = []
          MaxCpuCount = Some None
          DoRestore = false
          NoLogo = false
          NodeReuse = false
          ToolsVersion = None
          Verbosity = None
          NoConsoleLogger = false
          WarnAsError = None
          NoWarn = None
          RestorePackagesFlag = false
          DisableInternalBinLog = false
          ConsoleLogParameters =
            if BuildServer.ansiColorSupport then
                [ ForceConsoleColor ]
            else
                []
          FileLoggers = None
          BinaryLoggers = None
          DistributedLoggers = None
          Loggers = None
          Environment =
            Process.createEnvironmentMap ()
            |> Map.remove "MSBUILD_EXE_PATH"
            |> Map.remove "MSBuildExtensionsPath"
            |> Map.remove "MSBuildLoadMicrosoftTargetsReadOnly"
            |> Map.remove "MSBuildSDKsPath" }

    /// Sets the current environment variables.
    member x.WithEnvironment map = { x with Environment = map }

/// <summary>
/// Contains tasks which allow to use MSBuild (or xBuild on Linux/Unix) to build .NET project files or solution files.
/// </summary>
[<RequireQualifiedAccess>]
module MSBuild =
    /// <summary>
    /// A type for MSBuild task parameters
    /// </summary>
    type CliArguments =
        {
            /// The list of targets to use
            Targets: string list

            /// Set or override the specified project-level properties
            Properties: (string * string) list

            /// <summary>
            /// corresponds to the msbuild option <c>/m</c>:
            /// <list type="number">
            /// <item>
            /// <c>None</c> will omit the option.
            /// </item>
            /// <item>
            /// <c>Some None</c> will emit <c>/m</c>.
            /// </item>
            /// <item>
            /// <c>Some 2</c> will emit <c>/m:2</c>.
            /// </item>
            /// </list>
            /// </summary>
            MaxCpuCount: int option option

            /// Execute a restore before executing the targets (<c>/restore</c> flag)
            DoRestore: bool

            /// Don't display the startup banner or the copyright message.
            NoLogo: bool

            /// Enable or disable the re-use of MSBuild nodes. You can specify the following values
            NodeReuse: bool

            /// Specifies the version of the Toolset to use to build the project
            ToolsVersion: string option

            /// Specifies the amount of information to display in the build log. Each logger displays events based on
            /// the verbosity level that you set for that logger
            Verbosity: MSBuildVerbosity option

            /// Disable the default console logger, and don't log events to the console.
            NoConsoleLogger: bool

            /// The list of warnings to treat as errors
            WarnAsError: string list option

            /// The list of warning to ignore
            NoWarn: string list option

            /// Fake attaches a binlog-logger in order to report errors and warnings. You can disable this behavior
            /// with this flag
            DisableInternalBinLog: bool

            /// corresponds to the msbuild option <c>/fl</c>
            FileLoggers: MSBuildFileLoggerConfig list option

            /// corresponds to the msbuild option <c>/bl</c>
            BinaryLoggers: string list option

            /// corresponds to the msbuild option <c>/consoleloggerparameters</c>
            ConsoleLogParameters: MSBuildLogParameter list

            /// corresponds to the msbuild option <c>/l</c>
            Loggers: MSBuildLoggerConfig list option

            /// corresponds to the msbuild option <c>/dl</c>
            DistributedLoggers: (MSBuildLoggerConfig * MSBuildLoggerConfig option) list option
        }

        static member Create() : CliArguments =
            { Targets = []
              Properties = []
              MaxCpuCount = None
              DoRestore = false
              NoLogo = false
              NodeReuse = false
              ToolsVersion = None
              Verbosity = None
              NoConsoleLogger = false
              WarnAsError = None
              NoWarn = None
              DisableInternalBinLog = false
              ConsoleLogParameters =
                if BuildServer.ansiColorSupport then
                    [ ForceConsoleColor ]
                else
                    []
              FileLoggers = None
              BinaryLoggers = None
              DistributedLoggers = None
              Loggers = None }

    let internal asCliArguments (x: MSBuildParams) : CliArguments =
        { Targets = x.Targets
          Properties = ("RestorePackages", x.RestorePackagesFlag.ToString()) :: x.Properties
          MaxCpuCount = x.MaxCpuCount
          NoLogo = x.NoLogo
          NodeReuse = x.NodeReuse
          DoRestore = x.DoRestore
          ToolsVersion = x.ToolsVersion
          Verbosity = x.Verbosity
          NoConsoleLogger = x.NoConsoleLogger
          WarnAsError = x.WarnAsError
          NoWarn = x.NoWarn
          DisableInternalBinLog = x.DisableInternalBinLog
          ConsoleLogParameters = x.ConsoleLogParameters
          FileLoggers = x.FileLoggers
          Loggers = x.Loggers
          BinaryLoggers = x.BinaryLoggers
          DistributedLoggers = x.DistributedLoggers }

    let internal withCliArguments (oldObj: MSBuildParams) (x: CliArguments) =
        { oldObj with
            Targets = x.Targets
            Properties = x.Properties
            MaxCpuCount = x.MaxCpuCount
            DoRestore = x.DoRestore
            NoLogo = x.NoLogo
            NodeReuse = x.NodeReuse
            RestorePackagesFlag =
                x.Properties
                |> Seq.tryFind (fun (p, _) -> p = "RestorePackages")
                |> (function
                | Some (_, v) -> Boolean.Parse v
                | None -> false)
            ToolsVersion = x.ToolsVersion
            Verbosity = x.Verbosity
            NoConsoleLogger = x.NoConsoleLogger
            WarnAsError = x.WarnAsError
            NoWarn = x.NoWarn
            Loggers = x.Loggers
            DisableInternalBinLog = x.DisableInternalBinLog
            ConsoleLogParameters = x.ConsoleLogParameters
            FileLoggers = x.FileLoggers
            BinaryLoggers = x.BinaryLoggers
            DistributedLoggers = x.DistributedLoggers }

    type MSBuildParams with

        member internal x.CliArguments = asCliArguments x
        member internal oldObj.WithCliArguments(x: CliArguments) = withCliArguments oldObj x

    /// <summary>
    /// Exposing MSBuild executable
    /// </summary>
    /// [omit]
    let msBuildExe = MSBuildExe.msBuildExe

    /// [omit]
    let msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003"

    /// [omit]
    let xName name = XName.Get(name, msbuildNamespace)

    /// [omit]
    let loadProject (projectFileName: string) : MSBuildProject =
        MSBuildProject.Load(projectFileName, LoadOptions.PreserveWhitespace)

    // See: http://msdn.microsoft.com/en-us/library/ms228186.aspx
    let internal unescapeMSBuildSpecialChars s =
        let replExpr = Text.RegularExpressions.Regex("%..")

        replExpr.Replace(
            s,
            Text.RegularExpressions.MatchEvaluator(fun _match ->
                match _match.Value with
                | "%24" -> "$"
                | "%25" -> "%"
                | "%27" -> "'"
                | "%40" -> "@"
                | "%3B" -> ";"
                | "%3F" -> "?"
                | "%2A" -> "*"
                | _ -> _match.Value)
        )

    let internal getReferenceElements elementName projectFileName (doc: XDocument) =
        let fi = FileInfo.ofPath projectFileName

        doc
            .Descendants(xName "Project")
            .Descendants(xName "ItemGroup")
            .Descendants(xName elementName)
        |> Seq.map (fun e ->
            let a = e.Attribute(XName.Get "Include")

            let value =
                a.Value |> unescapeMSBuildSpecialChars |> Path.convertWindowsToCurrentPath

            let fileName =
                if
                    value.StartsWith(".." + Path.directorySeparator)
                    || (not <| value.Contains Path.directorySeparator)
                then
                    fi.Directory.FullName @@ value
                else
                    value

            a, fileName |> Path.getFullName)

    let internal quoteString str = sprintf "\"%s\"" str

    let rec private getProjectReferences (projectFileName: string) =
        match projectFileName.EndsWith ".sln" with
        | true -> Set.empty
        | false ->
            let doc = loadProject projectFileName

            let references =
                getReferenceElements "ProjectReference" projectFileName doc
                |> Seq.map snd
                |> Seq.filter File.Exists

            references
            |> Seq.collect getProjectReferences
            |> Seq.append references
            |> Set.ofSeq

    let internal fromCliArguments (p: CliArguments) =
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
            | t -> Some("t", t |> Seq.map (String.replace "." "_") |> String.separated ";")

        // see https://github.com/fsharp/FAKE/issues/2112
        let escapePropertyValue (v: string) =
            // https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-special-characters?view=vs-2017
            v
                .Replace("%", "%25")
                .Replace("\\", "%5C")
                .Replace("\"", "%22")
                .Replace(";", "%3B")
                .Replace(",", "%2C")
                .Replace("$", "%24")
                .Replace("@", "%40")
                .Replace("'", "%27")
                .Replace("?", "%3F")
                .Replace("*", "%2A")

        let properties =
            p.Properties
            |> List.map (fun (k, v) -> Some("p", sprintf "%s=%s" k (escapePropertyValue v)))

        let maxCpu =
            match p.MaxCpuCount with
            | None -> None
            | Some x ->
                Some(
                    "m",
                    match x with
                    | Some v -> v.ToString()
                    | _ -> ""
                )

        let restoreFlag = if p.DoRestore then Some("restore", "") else None

        let noLogo = if p.NoLogo then Some("nologo", "") else None

        let nodeReuse = if p.NodeReuse then None else Some("nodeReuse", "False")

        let tools =
            match p.ToolsVersion with
            | None -> None
            | Some t -> Some("tv", t)

        let verbosity =
            match p.Verbosity with
            | None -> None
            | Some v -> Some("v", verbosityName v)

        let noConsoleLogger = if p.NoConsoleLogger then Some("noconlog", "") else None

        let warnAsError =
            match p.WarnAsError with
            | None -> None
            | Some w -> Some("warnaserror", w |> String.concat ";")

        let nowarn =
            match p.NoWarn with
            | None -> None
            | Some w -> Some("nowarn", w |> String.concat ";")

        let loggerParams paramList =
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
                | ForceConsoleColor -> "ForceConsoleColor"
                | DisableMPLogging -> "DisableMPLogging"
                | EnableMPLogging -> "EnableMPLogging"
                | LogParameter o -> o

            paramList |> List.map (logParams >> (sprintf "%s")) |> String.concat ";"

        let consoleLogParams =
            match p.ConsoleLogParameters with
            | [] -> None
            | ps -> Some("clp", loggerParams ps)

        let fileLoggers =
            let serializeLogger fl =
                sprintf
                    "%s%s%s"
                    (match fl.Filename with
                     | None -> ""
                     | Some f -> sprintf "LogFile=%s;" f)
                    (match fl.Verbosity with
                     | None -> ""
                     | Some v -> sprintf "Verbosity=%s;" (verbosityName v))
                    (match fl.Parameters with
                     | None -> ""
                     | Some ps -> loggerParams ps)

            match p.FileLoggers with
            | None -> []
            | Some fls -> fls |> List.map (fun fl -> Some("flp" + (string fl.Number), serializeLogger fl))

        let binaryLoggers =
            match p.BinaryLoggers with
            | None -> []
            | Some bls -> bls |> List.map (fun bl -> Some("bl", bl))

        let serializeLogger (dLogger: MSBuildLoggerConfig) =
            sprintf
                "%s%s%s"
                (match dLogger.ClassName with
                 | None -> ""
                 | Some name -> sprintf "%s," name)
                (sprintf "%s" dLogger.AssemblyPath)
                (match dLogger.Parameters with
                 | None -> ""
                 | Some vars ->
                     vars
                     |> List.fold (fun acc (k, v) -> sprintf "%s%s=%s;" acc k v) ""
                     |> sprintf ";%s")

        let loggers =
            match p.Loggers with
            | None -> []
            | Some ls -> ls |> List.map (fun l -> Some("l", serializeLogger l))

        let distributedFileLoggers =
            let createLoggerString cl fl =
                match fl with
                | None -> serializeLogger cl
                | Some l -> sprintf "%s*%s" (serializeLogger cl) (serializeLogger l)

            match p.DistributedLoggers with
            | None -> []
            | Some dfls -> dfls |> List.map (fun (cl, fl) -> Some("dl", createLoggerString cl fl))

        [ yield restoreFlag
          yield targets
          if not Environment.isUnix then
              yield maxCpu
              yield noLogo
              yield nodeReuse
          yield tools
          yield verbosity
          yield noConsoleLogger
          yield warnAsError
          yield nowarn
          yield consoleLogParams
          yield! fileLoggers
          yield! binaryLoggers
          yield! loggers
          yield! distributedFileLoggers
          yield! properties ]
        |> Seq.choose id
        |> Seq.map (fun (k, v) -> "/" + k + (if String.isNullOrEmpty v then "" else ":" + v))
        |> Seq.toList

    /// [omit]
    let buildArgs (setParams: MSBuildParams -> MSBuildParams) =
        let p = MSBuildParams.Create() |> setParams
        p, fromCliArguments p.CliArguments


    let internal getVersion =
        let cache = System.Collections.Concurrent.ConcurrentDictionary<string, Version>()

        fun (exePath: string) (callMsbuildExe: string list -> string) ->
            let getFromCall () =
                try
                    let result =
                        match Environment.isUnix with
                        | true -> callMsbuildExe ["--version"; "--nologo"]
                        | false -> callMsbuildExe ["/version"; "/nologo"]

                    let line =
                        if result.Contains "DOTNET_CLI_TELEMETRY_OPTOUT" then
                            result.Split('\n') |> Seq.filter (String.IsNullOrWhiteSpace >> not) |> Seq.last
                        else
                            result

                    Version.Parse(line)
                with e ->
                    Trace.traceFAKE "Could not detect msbuild version from '%s': %O" exePath e
                    Version(13, 0, 0, 0)

            cache.GetOrAdd(exePath, System.Func<string, _>(fun _ -> getFromCall ()))

    let private versionToUseBinLog = Version("15.3")
    let private versionToUseStructuredLogger = Version("14.0")

    let internal addBinaryLogger
        (exePath: string)
        (callMsbuildExe: string list -> string)
        (args: string list)
        (disableFakeBinLogger: bool)
        =
#if !NO_MSBUILD_BINLOG
        if disableFakeBinLogger then
            None, args
        else
            let argList = args |> Seq.toList
            let path = Path.GetTempFileName()
            File.Delete(path)
            let path = path + ".binlog"
            //let path = Path.GetFullPath <| sprintf "fake-msbuild-%s.binlog" (System.Guid.NewGuid().ToString())
            let v = getVersion exePath callMsbuildExe

            if v >= versionToUseBinLog then
                Some path, (argList @ [ "/bl:" + path ])
            elif v >= versionToUseStructuredLogger then
                let assemblyPath =
                    let currentPath = MSBuildBinLog.structuredLogAssemblyPath
                    let libFolder = Path.GetDirectoryName(Path.GetDirectoryName currentPath)

                    if exePath.EndsWith " msbuild" then
                        currentPath
                    else
                        Path.Combine(libFolder, "net46", "StructuredLogger.dll")

                Some path,
                (argList @ [ sprintf "/logger:BinaryLogger,%s;%s" assemblyPath path ])
            else
                Trace.traceFAKE
                    "msbuild version '%O' doesn't support binary logger, please set the msbuild argument 'DisableInternalBinLog' to 'true' to disable this warning."
                    v

                None, args
#else
        None, args
#endif

    let internal handleAfterRun command binLogPath exitCode project =
        let msgs =
#if !NO_MSBUILD_BINLOG
            match binLogPath with
            | Some f ->
                if File.Exists f then
                    let r = MSBuildBinLog.getErrorsAndWarnings f

                    try
                        File.Delete(f)
                    with e ->
                        Trace.traceFAKE "Could not delete '%s': %O" f e

                    r
                else
                    Trace.traceFAKE
                        "msbuild has not created the binlog file as expected, no warnings or errors are reported using native CI capabilities. Use 'DisableInternalBinLog' to 'true' to disable this warning."

                    []
            | None ->
#endif
            []

#if !NO_MSBUILD_BINLOG
        MSBuildBinLog.emitMessages msgs
#endif
        if exitCode <> 0 then
            let errors =
                msgs |> List.choose (fun m -> if m.IsError then Some m.Message else None)

            let errorMessage =
                sprintf "'%s %s' failed with exit code %d." command project exitCode

            raise (MSBuildException(errorMessage, errors))

    // used for detection
    let private callMsBuildExe msBuildParams args =

        let results = System.Collections.Generic.List<string>()

        let errorF msg = results.Add msg

        let messageF msg = results.Add msg

        let processResult =
            CreateProcess.fromRawCommand msBuildParams.ToolPath args
            |> CreateProcess.withTimeout TimeSpan.MaxValue
            |> CreateProcess.withEnvironment (msBuildParams.Environment |> Map.toList)
            |> CreateProcess.redirectOutput
            |> CreateProcess.withOutputEventsNotNull messageF errorF
            |> Proc.run

        if processResult.ExitCode <> 0 then
            failwithf "msbuild failed with exit code '%d'" processResult.ExitCode

        String.Join("\n", results)

    /// <summary>
    /// Run MSBuild and collect output results and return it.
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="project">A string with the path to the project file to build.</param>
    let buildWithRedirect setParams project =
        let msBuildParams, argsString = buildArgs setParams

        let args = project :: argsString

        let binlogPath, args =
            addBinaryLogger
                msBuildParams.ToolPath
                (callMsBuildExe msBuildParams)
                args
                msBuildParams.DisableInternalBinLog

        let wd =
            if msBuildParams.WorkingDirectory = Directory.GetCurrentDirectory() then
                ""
            else
                sprintf "%s>" msBuildParams.WorkingDirectory

        Trace.tracefn "%s%s %s" wd msBuildParams.ToolPath (Args.toWindowsCommandLine args)

        let results = System.Collections.Generic.List<ConsoleMessage>()

        let errorF msg =
            results.Add(ConsoleMessage.CreateError msg)

        let messageF msg =
            results.Add(ConsoleMessage.CreateOut msg)

        let processResult =
            CreateProcess.fromRawCommand msBuildParams.ToolPath args
            |> CreateProcess.withTimeout TimeSpan.MaxValue
            |> CreateProcess.withEnvironment (msBuildParams.Environment |> Map.toList)
            |> CreateProcess.withWorkingDirectory msBuildParams.WorkingDirectory
            |> CreateProcess.redirectOutput
            |> CreateProcess.withOutputEventsNotNull messageF errorF
            |> Proc.run

        try
            handleAfterRun "msbuild" binlogPath processResult.ExitCode project
            processResult.ExitCode, results
        with e ->
            processResult.ExitCode, results

    /// <summary>
    /// Runs a MSBuild project
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="project">A string with the path to the project file to build.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// open Fake.DotNet
    ///     let buildMode = Environment.environVarOrDefault "buildMode" "Release"
    ///     let setParams (defaults:MSBuildParams) =
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
    ///     MSBuild.build setParams "./MySolution.sln"
    /// </code>
    /// </example>
    let build setParams project =
        use __ = Trace.traceTask "MSBuild" project
        let msBuildParams, argsString = buildArgs setParams

        let args = project :: argsString

        let binlogPath, args =
            addBinaryLogger
                msBuildParams.ToolPath
                (callMsBuildExe msBuildParams)
                args
                msBuildParams.DisableInternalBinLog

        let wd =
            if msBuildParams.WorkingDirectory = Directory.GetCurrentDirectory() then
                ""
            else
                sprintf "%s>" msBuildParams.WorkingDirectory

        Trace.tracefn "%s%s %s" wd msBuildParams.ToolPath (Args.toWindowsCommandLine args)

        let processResult =
            CreateProcess.fromRawCommand msBuildParams.ToolPath args
            |> CreateProcess.withWorkingDirectory msBuildParams.WorkingDirectory
            |> CreateProcess.withTimeout TimeSpan.MaxValue
            |> CreateProcess.withEnvironment (msBuildParams.Environment |> Map.toList)
            |> Proc.run

        handleAfterRun "msbuild" binlogPath processResult.ExitCode project
        __.MarkSuccess()

    /// <summary>
    /// Builds the given project files and collects the output files.
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="outputPath">If it is null or empty then the project settings are used.</param>
    /// <param name="targets">A string with the target names which should be run by MSBuild.</param>
    /// <param name="properties">A list with tuples of property name and property values.</param>
    /// <param name="projects">A list of project or solution files.</param>
    let runWithProperties
        (setParams: MSBuildParams -> MSBuildParams)
        outputPath
        (targets: string)
        (properties: string -> (string * string) list)
        projects
        =
        let projects = projects |> Seq.toList

        let output =
            match String.liftString outputPath with
            | Some path -> Some(Path.getFullName path)
            | None -> None

        let properties =
            match output with
            | Some path ->
                (fun project ->
                    let outputPath = (path |> String.trimSeparator) + Path.directorySeparator
                    ("OutputPath", outputPath) :: (properties project))
            | None -> properties

        let dependencies = projects |> List.map getProjectReferences |> Set.unionMany

        let setBuildParam project defaultParams =
            let projectParams = setParams defaultParams
            let targets = targets |> String.split ';' |> List.filter String.isNotNullOrEmpty

            { projectParams with
                Targets = projectParams.Targets @ targets
                Properties = projectParams.Properties @ properties project }

        projects
        |> List.filter (fun project -> not <| Set.contains project dependencies)
        |> List.iter (fun project -> build (setBuildParam project) project)

        // it makes no sense to output the root dir content here since it does not contain the build output
        match output with
        | Some path -> !!(path @@ "/**/*.*") |> Seq.toList
        | None -> []

    /// <summary>
    /// Builds the given project files or solution files and collects the output files.
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="outputPath">If it is null or empty then the project settings are used.</param>
    /// <param name="targets">A string with the target names which should be run by MSBuild.</param>
    /// <param name="properties">A list with tuples of property name and property values.</param>
    /// <param name="projects">A list of project or solution files.</param>
    let run setParams outputPath targets properties projects =
        runWithProperties setParams outputPath targets (fun _ -> properties) projects

    /// <summary>
    /// Builds the given project files or solution files and collects the output files.
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="outputPath">If it is null or empty then the project settings are used.</param>
    /// <param name="targets">A string with the target names which should be run by MSBuild.</param>
    /// <param name="projects">A list of project or solution files.</param>
    let runDebug setParams outputPath targets projects =
        run setParams outputPath targets [ "Configuration", "Debug" ] projects

    /// <summary>
    /// Builds the given project files or solution files and collects the output files.
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="outputPath">If it is null or empty then the project settings are used.</param>
    /// <param name="targets">A string with the target names which should be run by MSBuild.</param>
    /// <param name="projects">A list of project or solution files.</param>
    let runRelease setParams outputPath targets projects =
        run setParams outputPath targets [ "Configuration", "Release" ] projects

    /// <summary>
    /// Builds the given project files or solution files in release mode to the default outputs.
    /// </summary>
    ///
    /// <param name="targets">A string with the target names which should be run by MSBuild.</param>
    /// <param name="projects">A list of project or solution files.</param>
    let runWithDefaults targets projects =
        run id null targets [ "Configuration", "Release" ] projects

    /// <summary>
    /// Builds the given project files or solution files in release mode and collects the output files.
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="outputPath">If it is null or empty then the project settings are used.</param>
    /// <param name="properties">A list with tuples of property name and property values.</param>
    /// <param name="targets">A string with the target names which should be run by MSBuild.</param>
    /// <param name="projects">A list of project or solution files.</param>
    let runReleaseExt setParams outputPath properties targets projects =
        let properties = ("Configuration", "Release") :: properties
        run setParams outputPath targets properties projects

    /// <summary>
    /// Builds the given web project file in the specified configuration and copies it to the given outputPath.
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="configuration">MSBuild configuration.</param>
    /// <param name="projectFile">The project file path.</param>
    let buildWebsiteConfig setParams (outputPath: string) configuration projectFile =
        use __ = Trace.traceTask "BuildWebsite" projectFile
        let projectName = Path.GetFileNameWithoutExtension projectFile

        let slashes (dir: string) =
            dir.Replace("\\", "/").TrimEnd('/') |> Seq.filter ((=) '/') |> Seq.length

        let currentDir = (DirectoryInfo.ofPath ".").FullName
        let projectDir = (FileInfo.ofPath projectFile).Directory.FullName

        let diff = slashes projectDir - slashes currentDir

        let prefix =
            if Path.IsPathRooted outputPath then
                ""
            else
                (String.replicate diff "../")

        run setParams null "Build" [ "Configuration", configuration ] [ projectFile ]
        |> ignore

        run
            setParams
            null
            "_CopyWebApplication;_BuiltWebOutputGroupOutput"
            [ "Configuration", configuration
              "OutDir", prefix + outputPath
              "WebProjectOutputDir", prefix + outputPath + "/" + projectName ]
            [ projectFile ]
        |> ignore

        !!(projectDir + "/bin/*.*")
        |> Shell.copy (outputPath + "/" + projectName + "/bin/")

        __.MarkSuccess()

    /// <summary>
    /// Builds the given web project file with debug configuration and copies it to the given outputPath.
    /// </summary>
    ///
    /// <param name="outputPath">The output path.</param>
    /// <param name="projectFile">The project file path.</param>
    let buildWebsite outputPath projectFile =
        buildWebsiteConfig id outputPath "Debug" projectFile

    /// <summary>
    /// Builds the given web project files in specified configuration and copies them to the given outputPath.
    /// </summary>
    ///
    /// <param name="setParams">A function that overwrites the default MSBuildParams</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="configuration">MSBuild configuration.</param>
    /// <param name="projectFiles">The project file paths.</param>
    let buildWebsitesConfig setParams outputPath configuration projectFiles =
        Seq.iter (buildWebsiteConfig setParams outputPath configuration) projectFiles

    /// <summary>
    /// Builds the given web project files with debug configuration and copies them to the given websiteDir.
    /// </summary>
    ///
    /// <param name="outputPath">The output path.</param>
    /// <param name="projectFiles">The project file paths.</param>
    let buildWebsites outputPath projectFiles =
        buildWebsitesConfig outputPath "Debug" projectFiles

[<AutoOpen>]
module internal MSBuildParamExtensions =
    type MSBuildParams with

        member internal x.CliArguments = MSBuild.asCliArguments x
        member internal oldObj.WithCliArguments(x: MSBuild.CliArguments) = MSBuild.withCliArguments oldObj x
