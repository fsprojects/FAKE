(* -- Fake Dependencies paket-inline
source https://nuget.org/api/v2
source ../../../nuget/dotnetcore

nuget Fake.Core.Context prerelease
-- Fake Dependencies -- *)
#cd ".fake"
#cd __SOURCE_FILE__
#load "loadDependencies.fsx"
#cd __SOURCE_DIRECTORY__

printfn "loading context"
let context = Fake.Core.Context.forceFakeContext()
printfn "got: %A" context
