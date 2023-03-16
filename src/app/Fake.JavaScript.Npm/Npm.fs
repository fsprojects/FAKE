namespace Fake.JavaScript

open Fake.Core
open Fake.IO
open Fake.Testing.Common
open System
open System.IO

/// <namespacedoc>
/// <summary>
/// JavaScript namespace contains tasks to interact with JavaScript tools, like NPM, Yarn and TypeScript compiler
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Helpers to run the npm tool.
/// </summary>
///
/// <example>
/// <code lang="fsharp">
/// Npm.install (fun o ->
///         { o with
///             WorkingDirectory = "./src/FAKESimple.Web/"
///         })
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module Npm =

    /// Default paths to Npm
    let private npmFileName =
        ProcessUtils.tryFindFileOnPath "npm"
        |> function
            | Some npm when File.Exists npm -> npm
            | _ ->
                match Environment.isWindows with
                | true -> "./packages/Npm.js/tools/npm.cmd"
                | _ -> "/usr/bin/npm"

    /// Arguments for the Npm install command
    type InstallArgs =
        | Standard
        | Forced

    /// The list of supported Npm commands.
    type NpmCommand =
        | Install of InstallArgs
        | Run of string
        | RunSilent of string
        | RunTest of string
        | Test
        | CleanInstall
        | Custom of string

    /// The Npm parameter type
    type NpmParams =
        { Src: string
          NpmFilePath: string
          WorkingDirectory: string
          Timeout: TimeSpan }

    /// <summary>
    /// Npm default parameters
    /// </summary>
    let defaultNpmParams =
        { Src = ""
          NpmFilePath = npmFileName
          WorkingDirectory = "."
          Timeout = TimeSpan.MaxValue }

    let private parseInstallArgs =
        function
        | Standard -> ""
        | Forced -> " --force"

    let private parse =
        function
        | Install installArgs -> sprintf "install %s" (installArgs |> parseInstallArgs)
        | Run str -> sprintf "run %s" str
        | RunSilent str -> sprintf "run --silent %s" str
        | RunTest str -> sprintf "run --silent %s" str
        | Custom str -> str
        | Test -> "test --silent"
        | CleanInstall -> "ci"

    /// Runs the given process and returns the process result.
    let private execute npmParams command =
        let result = ref None
        let npmPath = Path.GetFullPath(npmParams.NpmFilePath)
        let args = command |> parse

        try
            let processResult =
                CreateProcess.fromRawCommandLine npmPath args
                |> CreateProcess.withWorkingDirectory npmParams.WorkingDirectory
                |> CreateProcess.withTimeout npmParams.Timeout
                |> Proc.run

            if processResult.ExitCode <> 0 then
                result.Value <- Some(sprintf "exit code: %d" processResult.ExitCode)
        with exn ->
            let message = ref exn.Message

            if not (isNull exn.InnerException) then
                message.Value <- message.Value + Environment.NewLine + exn.InnerException.Message

            result.Value <- Some(message.Value)

        match result.Value with
        | None -> ()
        | Some msg ->
            match command with
            | RunTest _ -> raise (FailedTestsException("Test(s) Failed"))
            | Test -> raise (FailedTestsException("Test(s) Failed"))
            | _ -> failwith msg


    let private npm setParams =
        defaultNpmParams |> setParams |> execute


    /// <summary>
    /// Run <c>npm install --force</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Npm.installForced (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let installForced setParams = npm setParams <| Install Forced

    /// <summary>
    /// Run <c>npm install</c>
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Npm.install (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let install setParams = npm setParams <| Install Standard

    /// <summary>
    /// Run <c>npm run &lt;command&gt;</c>
    /// </summary>
    ///
    /// <param name="command">Command to run</param>
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Npm.run "someCommand" (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let run command setParams = npm setParams <| Run command

    /// <summary>
    /// Run <c>npm run --silent &lt;command&gt;</c>. Suppresses npm error output.
    /// See <a href="https://github.com/npm/npm/issues/8821">npm:8821</a>.
    /// </summary>
    ///
    /// <param name="command">Command to run</param>
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Npm.runSilent "someCommand" (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let runSilent command setParams = npm setParams <| RunSilent command

    /// <summary>
    /// Run <c>npm run --silent &lt;command&gt;</c>. Suppresses npm error output and will raise an
    /// <c>FailedTestsException</c> exception after the script execution instead of failing, useful for CI.
    /// See <a href="https://github.com/npm/npm/issues/8821">npm:8821</a>.
    /// </summary>
    ///
    /// <param name="command">Command to run</param>
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Npm.runTest "test" (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let runTest command setParams = npm setParams <| RunTest command

    /// <summary>
    /// Run <c>npm test --silent</c>. Suppresses npm error output and will raise an <c>FailedTestsException</c>
    /// exception after the script execution instead of failing, useful for CI.
    /// See <a href="https://github.com/npm/npm/issues/8821">npm:8821</a>.
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Npm.test (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let test setParams = npm setParams Test

    /// <summary>
    /// Run <c>npm ci</c>.
    /// </summary>
    ///
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Npm.cleanInstall (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let cleanInstall setParams = npm setParams CleanInstall

    /// <summary>
    /// Run <c>npm &lt;command&gt;</c>. Used to run any command.
    /// </summary>
    ///
    /// <param name="command">Command to run</param>
    /// <param name="setParams">Set command parameters</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Npm.exec "--v" (fun o ->
    ///         { o with
    ///             WorkingDirectory = "./src/FAKESimple.Web/"
    ///         })
    /// </code>
    /// </example>
    let exec command setParams = npm setParams <| Custom command
