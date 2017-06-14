(* -- Fake Dependencies paket.dependencies
file paket.dependencies
group Main
-- Fake Dependencies -- *)
#load ".fake/use_external_dependencies.fsx/intellisense.fsx"

open Fake.Runtime

printfn "Starting Build."
Trace.traceFAKE "Some Info from FAKE"
printfn "Ending Build."
