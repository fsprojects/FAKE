namespace Fake.JavaScript

open Fake.Core
open Fake.IO
open System
open System.IO

/// <summary>
/// Helpers for running the yarn tool
/// </summary>
///
/// <example>
/// <code lang="fsharp">
/// Yarn.install (fun o ->
///         { o with
///             WorkingDirectory = "./src/FAKESimple.Web/"
///         })
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module Yarn =

    /// Default paths to Yarn
    let private yarnFileName =
        ProcessUtils.tryFindFileOnPath "yarn"
        |> function
            | Some yarn when File.Exists yarn -> yarn
            | _ ->
                match Environment.isWindows with
                | true -> "./packages/Yarnpkg.js/tools/yarn.cmd"
                | _ -> "/usr/bin/yarn"

    /// Arguments for the Yarn install command
    type InstallArgs =
        | Standard
        | Flat
        | Force
        | Har
        | NoLockFile
        | Production
        | PureLockFile
        | FrozenLockFile

    /// The list of supported Yarn commands. The `Custom` alternative
    /// can be used for other commands not in the list until they are
    /// implemented
    type YarnCommand =
        | Install of InstallArgs
        | Custom of string

    /// The Yarn parameter type
    type YarnParams =
        { Src: string
          YarnFilePath: string
          WorkingDirectory: string
          Timeout: TimeSpan }

    /// Yarn default parameters
    let defaultYarnParams =
        { Src = ""
          YarnFilePath = yarnFileName
          WorkingDirectory = "."
          Timeout = TimeSpan.MaxValue }

    let private parseInstallArgs =
        function
        | Standard -> ""
        | Flat -> " --flat"
        | Force -> " --force"
        | Har -> " --har"
        | NoLockFile -> " --no-lockfile"
        | Production -> " --production"
        | PureLockFile -> " --pure-lockfile"
        | FrozenLockFile -> " --frozen-lockfile"

    let private parse =
        function
        | Install installArgs -> sprintf "install%s" (installArgs |> parseInstallArgs)
        | Custom str -> str

    let private run yarnParams command =
        let yarnPath = Path.GetFullPath(yarnParams.YarnFilePath)
        let arguments = command |> parse

        arguments
        |> CreateProcess.fromRawCommandLine yarnPath
        |> CreateProcess.withWorkingDirectory yarnParams.WorkingDirectory
        |> CreateProcess.withTimeout yarnParams.Timeout
        |> CreateProcess.ensureExitCodeWithMessage (sprintf "'yarn %s' task failed" arguments)
        |> Proc.run
        |> ignore

    let private yarn setParams = defaultYarnParams |> setParams |> run

    /// <summary>
    /// Run <c>yarn install</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.install (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let install setParams = yarn setParams <| Install Standard

    /// <summary>
    /// Run <c>yarn install --production</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.installProduction (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let installProduction setParams = yarn setParams <| Install Production

    /// <summary>
    /// Run <c>yarn install --force</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.installForced (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let installForced setParams = yarn setParams <| Install Force

    /// <summary>
    /// Run <c>yarn install --flat</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.installFlat (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let installFlat setParams = yarn setParams <| Install Flat

    /// <summary>
    /// Run <c>yarn install --har</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.installHar (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let installHar setParams = yarn setParams <| Install Har

    /// <summary>
    /// Run <c>yarn install --no-lockfile</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.installNoLock (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let installNoLock setParams = yarn setParams <| Install NoLockFile

    /// <summary>
    /// Run <c>yarn install --pure-lockfile</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.installPureLock (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let installPureLock setParams = yarn setParams <| Install PureLockFile

    /// <summary>
    /// Run <c>yarn install --frozen-lockfile</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.installFrozenLockFile (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let installFrozenLockFile setParams =
        yarn setParams <| Install FrozenLockFile

    /// <summary>
    /// Run <c>yarn &lt;command&gt;</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Yarn.exec "someCommand" (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let exec command setParams = yarn setParams <| Custom command
