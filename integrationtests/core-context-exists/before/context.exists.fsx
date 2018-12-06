#if FAKE_DEPENDENCIES
#r "paket:
storage: none
source https://api.nuget.org/v3/index.json
source ../../../release/dotnetcore
//source https://ci.appveyor.com/nuget/paket

nuget Fake.Core.Context prerelease"
#endif
#load ".fake/context.exists.fsx/intellisense.fsx"

printfn "loading context"
let context = Fake.Core.Context.forceFakeContext()
printfn "got: %A" context
