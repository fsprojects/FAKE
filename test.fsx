;;
#I @"packages/build/FAKE/tools/"
#r @"FakeLib.dll"
open Fake

Target "Clean" (fun _ ->
    !! "src/*/*/bin"
    ++ "src/*/*/obj"
    |> CleanDirs)
    
let readerParams = new Mono.Cecil.ReaderParameters(AssemblyResolver = null)
printfn "testcecilLoad"