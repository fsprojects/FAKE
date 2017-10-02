(* -- Fake Dependencies paket-inline
storage: none
source https://nuget.org/api/v2
source ../../../nuget/dotnetcore

nuget Fake.Core.Target prerelease
nuget FSharp.Core prerelease
-- Fake Dependencies -- *)

printfn "before load"

#load ".fake/reference_fake-targets.fsx/intellisense.fsx"

printfn "test_before open"

open Fake.Core
open Fake.Core.TargetOperators

printfn "test_before targets"
Target.Create "Start" (fun _ -> ())

Target.Create "TestTarget" (fun _ ->
    printfn "Starting Build."
    Trace.traceFAKE "Some Info from FAKE"
    printfn "Ending Build."
)

"Start"
  ==> "TestTarget"

printfn "before run targets"

Target.RunOrDefault "TestTarget"
