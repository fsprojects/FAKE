#if FAKE_DEPENDENCIES
#r "paket: storage: none"
#r "paket: source https://api.nuget.org/v3/index.json"
#r "paket: source ../../../release/dotnetcore"
#r "paket: //source https://ci.appveyor.com/nuget/paket"
#r "paket: nuget Fake.Runtime prerelease"
#r "paket: nuget FSharp.Core prerelease"
#endif

// Issue https://github.com/fsharp/FAKE/issues/2121
System.Environment.CurrentDirectory <- System.IO.Path.GetFullPath "mydir"
failwith "runtime error"