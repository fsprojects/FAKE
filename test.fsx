#r @"packages/Mono.Cecil/lib/net40/Mono.Cecil.dll"

let readerParams = new Mono.Cecil.ReaderParameters(AssemblyResolver = null)
printfn "echo world"