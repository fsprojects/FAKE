(* -- Fake Dependencies paket.dependencies
file paket.dependencies
group Main
-- Fake Dependencies -- *)
#cd ".fake"
#cd __SOURCE_FILE__
#load "loadDependencies.fsx"
#cd __SOURCE_DIRECTORY__


open Fake.Runtime

printfn "Starting Build."
Trace.traceFAKE "Some Info from FAKE"
printfn "Ending Build."