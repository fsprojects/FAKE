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



// Targets
Target "StopCassini" (fun _ ->
    killProcess "CassiniDev4"
)

Target "StartCassini" (fun _ ->
    { Program          = @".\tools\cassini\CassiniDev4.exe"
      WorkingDirectory = "."
      CommandLine      = ""
      Args             = ["/a:",@".\website"]}
        |> shellExec
        |> ignore
)

"StopCassini"
  ==> "StartCassini"
 
// start build
Run "StartCassini"