#if FAKE_DEPENDENCIES
#r "paket:
source https://api.nuget.org/v3/index.json
source ../../../release/dotnetcore

nuget Fake.Core.Target prerelease
nuget System.Reactive.Compatibility
nuget FSharp.Core prerelease"
#endif

printfn "before load"

#load ".fake/reference_fake-targets.fsx/intellisense.fsx"

printfn "test_before open"

open Fake.Core

let args : string[] option = Target.getArguments()

match args with
| Some args -> 
  printfn "GlobalArgs: %A" args
| None -> ()

#load "otherfile.fs"
#load "otherscript.fsx"

OtherFile.createTargets()


open Fake.Core.TargetOperators

printfn "test_before targets"
Target.description "Test description"
Target.create "Start" ignore

Target.create "TestTarget" (fun p ->
    printfn "Starting Build %A." {| test = "anon_record_f#-4.6" |}
    Trace.traceFAKE "Some Info from FAKE"
    printfn "Arguments: %A" p.Context.Arguments
    printfn "Ending Build."
)

"Start"
  ==> "TestTarget"

printfn "before run targets"

Target.runOrDefaultWithArguments "TestTarget"
