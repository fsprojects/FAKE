(* -- Fake Dependencies paket-inline
source https://nuget.org/api/v2
source ..\..\..\nuget\dotnetcore

nuget Fake.Runtime prerelease
nuget FSharp.Core prerelease
-- Fake Dependencies -- *)
#cd ".fake"
#cd __SOURCE_FILE__
#load "loadDependencies.fsx"
#cd __SOURCE_DIRECTORY__


open Fake.Runtime

printfn "Starting Build."
Trace.traceFAKE "Some Info from FAKE"
printfn "Ending Build."