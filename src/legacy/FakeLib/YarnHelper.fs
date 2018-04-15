/// Contains function to run yarn tasks
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Yarn module instead")>]
module Fake.YarnHelper
open Fake
open System
open System.IO
open System.Diagnostics

/// Default paths to Yarn
let private yarnFileName =
    match isWindows with
    | true ->
        System.Environment.GetEnvironmentVariable("PATH")
        |> fun path -> path.Split ';'
        |> Seq.tryFind (fun p -> p.IndexOf("yarn", StringComparison.OrdinalIgnoreCase) >= 0)
        |> fun res ->
            match res with
            | Some yarn when File.Exists (sprintf @"%s\yarn.cmd" yarn) -> (sprintf @"%s\yarn.cmd" yarn)
            | _ -> "./packages/Yarnpkg.js/tools/yarn.cmd"
    | _ ->
        let info = new ProcessStartInfo("which","yarn")
        info.StandardOutputEncoding <- System.Text.Encoding.UTF8
        info.RedirectStandardOutput <- true
        info.UseShellExecute        <- false
        info.CreateNoWindow         <- true
        use proc = Process.Start info
        proc.WaitForExit()
        match proc.ExitCode with
            | 0 when not proc.StandardOutput.EndOfStream ->
              proc.StandardOutput.ReadLine()
            | _ -> "/usr/bin/yarn"

[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Yarn module instead")>]
/// Arguments for the Yarn install command
type InstallArgs =
| Standard
| Flat
| Force
| Har
| NoLockFile
| Production
| PureLockFile

/// The list of supported Yarn commands. The `Custom` alternative
/// can be used for other commands not in the list until they are
/// implemented
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Yarn module instead")>]
type YarnCommand =
| Install of InstallArgs
| Add of string
| Custom of string

/// The Yarn parameter type
[<CLIMutable>]
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Yarn module instead")>]
type YarnParams =
    { Src: string
      YarnFilePath: string
      WorkingDirectory: string
      Command: YarnCommand
      Timeout: TimeSpan }

/// Yarn default parameters
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Yarn module instead")>]
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


[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Yarn module instead")>]
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

[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.Yarn module instead")>]
let Yarn setParams =
    defaultYarnParams |> setParams |> run
