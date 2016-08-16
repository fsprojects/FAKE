(* -- Fake Dependencies paket-inline
source https://nuget.org/api/v2
source ..\..\..\nuget\dotnetcore

nuget Fake.Core.Targets prerelease
nuget FSharp.Core prerelease
-- Fake Dependencies -- *)

printfn "before load"

#cd ".fake"
#cd __SOURCE_FILE__
#load "loadDependencies.fsx"
#cd __SOURCE_DIRECTORY__

printfn "test_before open"

open Fake.Core
open Fake.Core.Targets
open Fake.Core.TargetOperators

printfn "test_before targets"
Target "Start" (fun _ -> ())

Target "TestTarget" (fun _ ->
    printfn "Starting Build."
    Trace.traceFAKE "Some Info from FAKE"
    printfn "Ending Build."
)

"Start"
  ==> "TestTarget"

printfn "before run targets"

RunTargetOrDefault "TestTarget"
