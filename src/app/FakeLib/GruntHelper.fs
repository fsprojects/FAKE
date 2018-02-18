/// Contains function to run Grunt tasks
module Fake.GruntHelper
open Fake
open System
open System.IO
open System.Diagnostics

/// Default paths to Grunt
let private gruntFileName =
    match isWindows with
    | true -> 
        System.Environment.GetEnvironmentVariable("USERPROFILE")
        |> fun profilePath -> profilePath @@ "AppData" @@ "Roaming" @@ "npm" @@ "grunt.cmd"
        |> fun gruntCmdPath ->
            match File.Exists gruntCmdPath with
            | true -> gruntCmdPath
            | _ -> "node node_modules/grunt-cli/bin/grunt"
    | _ -> 
        let info = new ProcessStartInfo("which","grunt")
        info.StandardOutputEncoding <- System.Text.Encoding.UTF8
        info.RedirectStandardOutput <- true
        info.UseShellExecute        <- false
        info.CreateNoWindow         <- true
        use proc = Process.Start info
        proc.WaitForExit()
        match proc.ExitCode with
            | 0 when not proc.StandardOutput.EndOfStream ->
              proc.StandardOutput.ReadLine()
            | _ -> "/usr/bin/grunt"
        
/// The Grunt parameter type
[<CLIMutable>]
type GruntParams = 
    { GruntFilePath: string
      WorkingDirectory: string
      Command: string
      Timeout: TimeSpan }

/// Grunt default parameters
let defaultGruntParams = 
    { GruntFilePath = gruntFileName
      Command = ""
      WorkingDirectory = "."
      Timeout = TimeSpan.MaxValue }

/// Runs the given process and returns the process result.
let run gruntParams =
    let result = ref None
    try 
        let exitCode = 
            ExecProcess (fun info -> 
                info.WorkingDirectory <- gruntParams.WorkingDirectory
                info.FileName <- gruntParams.GruntFilePath
                info.Arguments <- gruntParams.Command) gruntParams.Timeout
        if exitCode <> 0 then result := Some(sprintf "Grunt exit code: %d" exitCode)
    with exn ->
        let message = ref exn.Message
        if exn.InnerException <> null then message := !message + Environment.NewLine + exn.InnerException.Message
        result := Some(!message)
    match !result with
    | None -> ()
    | Some msg -> failwith msg

/// Runs Grunt with the given modification function. Make sure to have grunt-cli installed.
/// To change which `grunt` executable to use you can set the `GruntFilePath` parameter with the `setParams` function.
///
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the Grunt default parameters.
///
/// ## Sample
///
///        Target "Web" (fun _ ->
///            Grunt (fun p ->
///                   { p with
///                       Command = "build"
///                       WorkingDirectory = "./src/FAKESimple.Web/"
///                   })
///
///            Grunt (fun p ->
///                   { p with
///                       Command = "--setversion=1.2.3"
///                       WorkingDirectory = "./src/FAKESimple.Web/"
///                   })
///        )
let Grunt setParams = defaultGruntParams |> setParams |> run
