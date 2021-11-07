#r "paket:
storage: none
source https://api.nuget.org/v3/index.json
source ../../../release/dotnetcore
nuget Fake.Runtime prerelease
nuget FSharp.Core prerelease"

open Fake.Runtime

printfn "Starting Build."
Trace.traceFAKE "Some Info from FAKE"
printfn "Ending Build."
