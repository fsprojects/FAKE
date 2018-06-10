//#if (dependencies == "inline")
#r "paket:
    source https://api.nuget.org/v3/index.json
    nuget Fake.DotNet.Cli
    nuget Fake.IO.FileSystem
    nuget Fake.Core.Target //"
//#endif
#load ".fake/(build.fsx)/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build id)
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"

Target.runOrDefault "All"
