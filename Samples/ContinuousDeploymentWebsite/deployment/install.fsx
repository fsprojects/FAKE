// include Fake libs
#I @"tools\FAKE\"
#r "FakeLib.dll"

open Fake

open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Threading
open System.Collections.Generic

/// Runs the given process and returns the exit code
let StartProcess infoAction =
    use p = new Process()
    p.StartInfo.UseShellExecute <- false
    infoAction p.StartInfo
    p.Start() |> ignore

// Targets
Target "StopCassini" (fun _ ->
    killProcess "CassiniDev4"
)

Target "StartCassini" (fun _ ->
    let args = "/a:" + @".\website"

    StartProcess
        (fun info ->  
            info.FileName <- @".\tools\cassini\CassiniDev4.exe"
            info.WorkingDirectory <- null
            info.Arguments <- args)
)

"StopCassini"
  ==> "StartCassini"
 
// start build
Run "StartCassini"