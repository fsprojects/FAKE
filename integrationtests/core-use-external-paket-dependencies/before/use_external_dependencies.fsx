#r "paket: groupref Main //"
#load ".fake/use_external_dependencies.fsx/intellisense.fsx"

open Fake.Runtime

printfn "Starting Build."
Trace.traceFAKE "Some Info from FAKE"
printfn "Ending Build."
