(* -- Fake Dependencies paket-inline
storage: none
source https://nuget.org/api/v2
source ../../../nuget/dotnetcore
//source https://ci.appveyor.com/nuget/paket

nuget Fake.Runtime prerelease
nuget FSharp.Core prerelease
-- Fake Dependencies -- *)
#load ".fake/reference_fake-runtime.fsx/intellisense.fsx"

open Fake.Runtime

printfn "Starting Build."
Trace.traceFAKE "Some Info from FAKE"
printfn "Ending Build."
