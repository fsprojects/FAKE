/// Contains function to run bower tasks
module Fake.BowerHelper
open Fake
open System
open System.IO

/// Default paths to Bower
let private bowerFileName =
    match isUnix with
    | true -> "/usr/local/bin/bower"
    | _ -> "./packages/Bower.js/tools/bower.cmd"

/// Arguments for the Bower install command
type InstallArgs =
| Standard
| Forced

/// The list of support Bower commands. The `Custom` alternative
/// can be used for other commands not in the list until they are
/// implemented
type BowerCommand =
| Install of InstallArgs
| Run of string
| Custom of string

/// The Bower parameter type
type BowerParams =
    { Src: string
      BowerFilePath: string
      WorkingDirectory: string
      Command: BowerCommand
      Timeout: TimeSpan }

/// Bower default parameters
let defaultBowerParams =
    { Src = ""
      BowerFilePath = bowerFileName
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

let run bowerParams =
    let bowerPath = Path.GetFullPath(bowerParams.BowerFilePath)
    let arguments = bowerParams.Command |> parse
    let ok =
        execProcess (fun info ->
            info.FileName <- bowerPath
            info.WorkingDirectory <- bowerParams.WorkingDirectory
            info.Arguments <- arguments) bowerParams.Timeout
    if not ok then failwith (sprintf "'bower %s' task failed" arguments)

/// Runs bower with the given modification function. Make sure to have bower installed,
/// you can install bower with nuget or a regular install. To change which `Bower` executable
/// to use you can set the `BowerFilePath` parameter with the `setParams` function.
///
/// ## Parameters
///
/// - `setParams` - Function used to overwrite the Bower default parameters.
///
/// ## Sample
///
///         Target "Web" (fun _ ->
///             Bower (fun p ->
///                       { p with
///                           Command = Install Standard
///                           WorkingDirectory = "./src/FakeSimple.Web/"
///                       })
///
///             Bower (fun p ->
///                       { p with
///                           Command = (Run "build")
///                           WorkingDirectory = "./src/FAKESimple.Web/"
///                       })
///         )
let Bower setParams =
    defaultBowerParams |> setParams |> run
