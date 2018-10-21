#if FAKE_DEPENDENCIES
#r "paket:
source https://api.nuget.org/v3/index.json
source ../../../release/dotnetcore
//source https://ci.appveyor.com/nuget/paket

nuget Fake.Runtime prerelease
nuget FSharp.Core prerelease"
#endif
open klajsdhgfasjkhd

printfn asd
Trace.traceFAKE "Some Info from FAKE"
printfn "Ending Build."