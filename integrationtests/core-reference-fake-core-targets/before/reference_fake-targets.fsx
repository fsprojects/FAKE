#if FAKE_DEPENDENCIES
#r "paket:
source https://nuget.org/api/v2
source ../../../nuget/dotnetcore

nuget Fake.Core.Target prerelease
nuget FSharp.Core prerelease"
#endif

printfn "before load"

#load ".fake/reference_fake-targets.fsx/intellisense.fsx"

printfn "test_before open"

open Fake.Core
open Fake.Core.TargetOperators

printfn "test_before targets"
Target.create "Start" (fun _ -> ())

Target.create "TestTarget" (fun p ->
    printfn "Starting Build."
    Trace.traceFAKE "Some Info from FAKE"
    printfn "Arguments: %A" p.Context.Arguments
    printfn "Ending Build."
)

"Start"
  ==> "TestTarget"

printfn "before run targets"

Target.runOrDefault "TestTarget"
