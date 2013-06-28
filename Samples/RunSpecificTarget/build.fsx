#r @"..\..\tools\FAKE\tools\FakeLib.dll"
open Fake 

Target "Clean" (fun () ->  trace " --- Cleaning stuff --- ")

Target "Build" (fun () ->  trace " --- Building the app --- ")

Target "Deploy" (fun () -> trace " --- Deploying app --- ")


"Clean"
  ==> "Build"
  ==> "Deploy"

RunTargetOrDefault "Deploy"