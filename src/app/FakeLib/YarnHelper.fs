/// Contains function to run yarn tasks
module Fake.YarnHelper
open Fake
open Fake.ProcessHelper
open System
open System.IO
open System.Diagnostics

/// Default paths to Yarn
let private yarnFileName =
    let (filename, arguments, defaultValue) =
        match isWindows with
        | true -> "where", "yarn.cmd", "./packages/Yarnpkg.Yarn/content/bin/yarn.cmd"
        | false -> "which", "yarn", "/usr/bin/yarn"

    let result = ExecProcessAndReturnMessages (fun info ->
                        info.FileName <- filename
                        info.Arguments <- arguments) (TimeSpan.FromMinutes 5.0)
        
    match result.ExitCode with
    | 0 -> result.Messages |> Seq.tryHead |> Option.defaultValue defaultValue
    | _ -> defaultValue


/// Arguments for the Yarn install command
type InstallArgs =
| Standard
| Flat
| Force
| Har
| NoLockFile
| Production
| PureLockfile

/// The list of supported Yarn commands. The `Custom` alternative
/// can be used for other commands not in the list until they are
/// implemented
type YarnCommand =
| Install of InstallArgs
| Add of string
| Custom of string

/// The Yarn parameter type
[<CLIMutable>]
type YarnParams =
    { Src: string
      YarnFilePath: string
      WorkingDirectory: string
      Command: YarnCommand
      Timeout: TimeSpan }

/// Yarn default parameters
let defaultYarnParams =
    { Src = ""
      YarnFilePath = yarnFileName
      Command = Install Standard
      WorkingDirectory = "."
      Timeout = TimeSpan.MaxValue }

let private parseInstallArgs = function
    | Standard -> ""
    | Flat -> " --flat"
    | Force -> " --force"
    | Har -> " --har"
    | NoLockFile -> " --no-lockfile"
    | Production -> " --production"
    | PureLockFile -> " --pure-lockfile"

let private parse = function
    | Install installArgs -> sprintf "install%s" (installArgs |> parseInstallArgs)
    | Add str -> sprintf "add %s" str
    | Custom str -> str

let run yarnParams =
    let yarnPath = Path.GetFullPath(yarnParams.YarnFilePath)
    let arguments = yarnParams.Command |> parse
    let ok =
        execProcess (fun info ->
            info.FileName <- yarnPath
            info.WorkingDirectory <- yarnParams.WorkingDirectory
            info.Arguments <- arguments) yarnParams.Timeout
    if not ok then failwith (sprintf "'yarn %s' task failed" arguments)

/// Runs yarn with the given modification function. Make sure to have yarn installed,
/// you can install yarn with nuget or a regular install. To change which `Yarn` executable
/// to use you can set the `YarnFilePath` parameter with the `setParams` function.
///
/// ## Parameters
///
/// - `setParams` - Function used to overwrite the Yarn default parameters.
///
/// ## Sample
///
///         Target "Web" (fun _ ->
///             Yarn (fun p ->
///                     { p with
///                         Command = Install Standard
///                         WorkingDirectory = "./src/FAKESimple.Web/"
///                     })
///
///             Yarn (fun p ->
///                     { p with
///                         Command = (Run "build")
///                         WorkingDirectory = "./src/FAKESimple.Web/"
///                     })
///         )
let Yarn setParams =
    defaultYarnParams |> setParams |> run
