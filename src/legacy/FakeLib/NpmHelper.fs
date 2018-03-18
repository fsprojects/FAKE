/// Contains function to run npm tasks
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Npm module instead")>]
module Fake.NpmHelper
open Fake
open System
open System.IO
open System.Diagnostics

/// Default paths to Npm
let private npmFileName =
    match isWindows with
    | true -> 
        System.Environment.GetEnvironmentVariable("PATH")
        |> fun path -> path.Split ';'
        |> Seq.tryFind (fun p -> p.Contains "nodejs")
        |> fun res ->
            match res with
            | Some npm when File.Exists (sprintf @"%s\npm.cmd" npm) -> (sprintf @"%s\npm.cmd" npm)
            | _ -> "./packages/Npm.js/tools/npm.cmd"
    | _ -> 
        let info = new ProcessStartInfo("which","npm")
        info.StandardOutputEncoding <- System.Text.Encoding.UTF8
        info.RedirectStandardOutput <- true
        info.UseShellExecute        <- false
        info.CreateNoWindow         <- true
        use proc = Process.Start info
        proc.WaitForExit()
        match proc.ExitCode with
            | 0 when not proc.StandardOutput.EndOfStream ->
              proc.StandardOutput.ReadLine()
            | _ -> "/usr/bin/npm"
        


[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Npm module instead")>]
/// Arguments for the Npm install command
type InstallArgs =
| Standard
| Forced

[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Npm module instead")>]
/// The list of supported Npm commands.
type NpmCommand =
/// Run `npm install`
| Install of InstallArgs
/// Run `npm run <string>`
| Run of string
/// Run `npm run --silent <string>`. Suppresses npm error output. See [npm:8821](https://github.com/npm/npm/issues/8821).
| RunSilent of string
/// Run `npm run --silent <string>`. Suppresses npm error output and will raise an FailedTestsException exception after the script execution instead of failing, useful for CI. See [npm:8821](https://github.com/npm/npm/issues/8821).
| RunTest of string
/// Run `npm test --silent`. Suppresses npm error output and will raise an FailedTestsException exception after the script execution instead of failing, useful for CI. See [npm:8821](https://github.com/npm/npm/issues/8821).
| Test
/// Run `npm <string>`. Can be used for running not implemented commands.
| Custom of string

[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Npm module instead")>]
/// The Npm parameter type
[<CLIMutable>]
type NpmParams = 
    { Src: string
      NpmFilePath: string
      WorkingDirectory: string
      Command: NpmCommand
      Timeout: TimeSpan }

[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Npm module instead")>]
/// Npm default parameters
let defaultNpmParams = 
    { Src = ""
      NpmFilePath = npmFileName
      Command = Install Standard
      WorkingDirectory = "."
      Timeout = TimeSpan.MaxValue }

let private parseInstallArgs = function
    | Standard -> ""
    | Forced -> " --force"

let private parse = function
    | Install installArgs -> sprintf "install %s" (installArgs |> parseInstallArgs)
    | Run str -> sprintf "run %s" str
    | RunSilent str -> sprintf "run --silent %s" str
    | RunTest str -> sprintf "run --silent %s" str
    | Custom str -> str
    | Test -> "test --silent"


[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Npm module instead")>]
/// Runs the given process and returns the process result.
let run npmParams =
    let result = ref None
    let npmPath = Path.GetFullPath(npmParams.NpmFilePath)
    let args = npmParams.Command |> parse
    try 
        let exitCode = 
            ExecProcess (fun info -> 
                info.WorkingDirectory <- npmParams.WorkingDirectory
                info.FileName <- npmPath
                info.Arguments <- args) npmParams.Timeout
        if exitCode <> 0 then result := Some(sprintf "exit code: %d" exitCode)
    with exn ->
        let message = ref exn.Message
        if exn.InnerException <> null then message := !message + Environment.NewLine + exn.InnerException.Message
        result := Some(!message)
    match !result with
    | None -> ()
    | Some msg ->
        match npmParams.Command with
        | RunTest str -> raise (UnitTestCommon.FailedTestsException("Test(s) Failed"))
        | Test -> raise (UnitTestCommon.FailedTestsException("Test(s) Failed"))
        | _ -> failwith msg

/// Runs npm with the given modification function. Make sure to have npm installed,
/// you can install npm with nuget or a regular install. To change which `Npm` executable
/// to use you can set the `NpmFilePath` parameter with the `setParams` function.
///
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the Npm default parameters.
///
/// ## Sample
///
///        Target "Web" (fun _ ->
///            Npm (fun p ->
///                   { p with
///                       Command = Install Standard
///                       WorkingDirectory = "./src/FAKESimple.Web/"
///                   })
///
///            Npm (fun p ->
///                   { p with
///                       Command = (Run "build")
///                       WorkingDirectory = "./src/FAKESimple.Web/"
///                   })
///        )

[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Npm module instead")>]
let Npm setParams = defaultNpmParams |> setParams |> run
