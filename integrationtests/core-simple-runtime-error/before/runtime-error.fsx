#if FAKE_DEPENDENCIES
#r "paket: storage: none"
#r "paket: source https://nuget.org/api/v2"
#r "paket: source ../../../nuget/dotnetcore"
#r "paket: //source https://ci.appveyor.com/nuget/paket"
#r "paket: nuget Fake.Runtime prerelease"
#r "paket: nuget FSharp.Core prerelease"
#endif
failwith "runtime error"