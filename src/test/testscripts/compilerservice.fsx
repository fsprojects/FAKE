#r @"../../../build/FakeLib.dll"
#r @"FSharp.Compiler.Service/bin/Debug/FSharp.Compiler.Service.dll"
open Fake
open Microsoft.FSharp.Compiler.SourceCodeServices

Target "Test" (fun _ ->
  if not (typeof<FSharpChecker>.Assembly.FullName.Contains("42.42.42.42")) then
    failwith "Wrong FSharp.Compiler.Service was loaded..."
)

RunTargetOrDefault "Test"

