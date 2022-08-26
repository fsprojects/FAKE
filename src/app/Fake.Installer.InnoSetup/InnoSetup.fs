namespace Fake.Installer

/// <namespacedoc>
/// <summary>
/// Installer namespace contains tasks to interact with install and setup tools for applications, like InnoSetup and Wix
/// </summary>
/// </namespacedoc>
/// 
/// <summary>
/// This module contains helper functions to create <a href="http://www.jrsoftware.org/isinfo.php">Inno Setup</a>
/// installers.
/// </summary>
[<RequireQualifiedAccess>]
module InnoSetup =

    open System
    open System.IO
    open Fake.Core
    open Fake.IO
    open System.Text

    /// Resolving InnoSetup installation path.
    let private toolPath =
        let innoExe = "ISCC.exe"

        let toolPath = ProcessUtils.tryFindLocalTool "TOOL" innoExe [ Directory.GetCurrentDirectory() ]

        match toolPath with
        | Some path -> path
        | None ->
            match ProcessUtils.tryFindFileOnPath innoExe with
            | Some inno when File.exists inno -> inno
            | _ -> innoExe

    /// default timeout value for InnoSetup task
    let private timeout = TimeSpan.FromMinutes 5.

    /// Output verbosity
    type QuietMode =
        | Default // Default output when compiling
        | Quiet // Quiet compile (print error messages only)
        | QuietAndProgress // Enable quiet compile while still displaying progress

    /// <summary>
    /// InnoSetup build parameters
    /// </summary>
    type InnoSetupParams =
        {
          /// The tool path - FAKE tries to find ISCC.exe automatically in any sub folder.
          ToolPath: string

          /// Specify the process working directory
          WorkingDirectory: string

          /// Specify a timeout for ISCC. Default: 5 min.
          Timeout: TimeSpan

          /// Enable or disable output (overrides Output)
          EnableOutput: bool option

          /// Output files to specified path (overrides OutputDir)
          OutputFolder: string

          /// Overrides OutputBaseFilename with the specified filename
          OutputBaseFilename: string

          /// Specifies output mode when compiling
          QuietMode: QuietMode

          /// Emulate #define public <name> <value>
          Defines: Map<string, string>

          /// Additional parameters
          AdditionalParameters: string option

          /// Path to inno-script file
          ScriptFile: string }

        /// InnoSetup default parameters
        static member Create() =
            { ToolPath = toolPath
              WorkingDirectory = ""
              Timeout = timeout
              ScriptFile = "innosetup.iss"
              EnableOutput = None
              OutputFolder = ""
              OutputBaseFilename = ""
              QuietMode = Default
              Defines = Map.empty
              AdditionalParameters = None }

    /// Run InnoSetup task
    let private run toolPath workingDirectory timeout command =
        use __ = Trace.traceTask "InnoSetup" command

        let processResult =
            CreateProcess.fromRawCommandLine toolPath command
            |> CreateProcess.withWorkingDirectory workingDirectory
            |> CreateProcess.withTimeout timeout
            |> Proc.run

        if processResult.ExitCode <> 0 then failwithf $"InnoSetup command {command} failed."

        __.MarkSuccess()

    /// Serialize InnoSetup arguments
    let private serializeInnoSetupParams p =
        let appendDefine (key, value) _ sb =
            if String.isNullOrEmpty value then
                StringBuilder.append $"/D%s{key}" sb
            else
                StringBuilder.append $"/D%s{key}=%s{value}" sb

        StringBuilder()
        |> StringBuilder.appendIfSome p.AdditionalParameters id
        |> StringBuilder.appendIfSome p.EnableOutput (fun enableOutput -> if enableOutput then "/O+" else "/O-")
        |> StringBuilder.appendIfNotNullOrEmpty p.OutputFolder "/O"
        |> StringBuilder.appendIfNotNullOrEmpty p.OutputBaseFilename "/F"
        |> StringBuilder.appendWithoutQuotes (
            match p.QuietMode with
            | Quiet -> "/Q"
            | QuietAndProgress -> "/Qp"
            | _ -> ""
        )
        |> StringBuilder.forEach (p.Defines |> Map.toList) appendDefine ""
        |> StringBuilder.append p.ScriptFile
        |> StringBuilder.toText

    /// <summary>
    /// Builds the InnoSetup installer.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default build parameters. See <c>InnoSetupParams.Create()</c></param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// InnoSetup.build (fun p ->
    ///         { p with
    ///             OutputFolder = "build" @@ "installer"
    ///             ScriptFile = "installer" @@ "setup.iss"
    ///         })
    /// </code>
    /// </example>
    let build setParams =
        let p = InnoSetupParams.Create() |> setParams

        p
        |> serializeInnoSetupParams
        |> run p.ToolPath p.WorkingDirectory p.Timeout
