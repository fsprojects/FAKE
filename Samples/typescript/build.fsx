#I @"../../tools/FAKE/tools/"
#r @"FakeLib.dll"

open Fake
open System
open TypeScript

Target "CompileTypeScript" (fun _ ->
  !! "**/*.ts"
    |> TypeScriptCompiler (fun p -> { p with TimeOut = TimeSpan.MaxValue }) 
)

RunTargetOrDefault "CompileTypeScript"