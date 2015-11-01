/// Contains function to run npm tasks
module NpmHelper
open Fake
open System
open System.IO

/// Default paths to Npm
let private npmFileName =
    match isUnix with
    | true -> "/usr/local/bin/npm"
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

/// Runs npm with the given modification function
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the Npm default parameters.
let Npm setParams =
    defaultNpmParams |> setParams |> run
