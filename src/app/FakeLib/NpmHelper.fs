/// Contains function to run npm tasks
module Fake.NpmHelper
open Fake
open System
open System.IO
open System.Diagnostics

/// Default paths to Npm
let private npmFileName =
    match isUnix with
    | true -> 
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
    | _ -> "./packages/Npm.js/tools/npm.cmd"

/// Arguments for the Npm install command
type InstallArgs =
| Standard
| Forced

/// The list of supported Npm commands. The `Custom` alternative
/// can be used for other commands not in the list until they are
/// implemented
type NpmCommand =
| Install of InstallArgs
| Run of string
| Custom of string

/// The Npm parameter type
type NpmParams = 
    { Src: string
      NpmFilePath: string
      WorkingDirectory: string
      Command: NpmCommand
      Timeout: TimeSpan }

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
    | Install installArgs -> sprintf "install%s" (installArgs |> parseInstallArgs)
    | Run str -> sprintf "run %s" str
    | Custom str -> str

let run npmParams =
    let npmPath = Path.GetFullPath(npmParams.NpmFilePath)
    let arguments = npmParams.Command |> parse
    let ok = 
        execProcess (fun info ->
            info.FileName <- npmPath
            info.WorkingDirectory <- npmParams.WorkingDirectory
            info.Arguments <- arguments) npmParams.Timeout
    if not ok then failwith (sprintf "'npm %s' task failed" arguments)

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
let Npm setParams =
    defaultNpmParams |> setParams |> run
