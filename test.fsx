;;
#I @"packages/build/FAKE/tools/"
#r @"FakeLib.dll"

Target "Clean" (fun _ ->
    !! "src/*/*/bin"
    ++ "src/*/*/obj"
    |> CleanDirs

    CleanDirs [buildDir; testDir; docsDir; apidocsDir; nugetDir; reportDir])
    
let readerParams = new Mono.Cecil.ReaderParameters(AssemblyResolver = null)
printfn "testcecilLoad"