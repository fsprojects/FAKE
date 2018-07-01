#if FAKE_DEPENDENCIES
#r "paket:
storage: none
source https://nuget.org/api/v2
source ../../../release/dotnetcore
//source https://ci.appveyor.com/nuget/paket

nuget Fake.Runtime prerelease
nuget FSharp.Core prerelease"
#endif
#load ".fake/reference_fake-runtime.fsx/intellisense.fsx"

open Fake.Runtime

printfn "Starting Build."
Trace.traceFAKE "Some Info from FAKE"
printfn "Ending Build."
