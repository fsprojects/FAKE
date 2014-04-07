#I @"../../tools/FAKE/tools/"
#r @"FakeLib.dll"

open Fake
open System
open TypeScript

Target "CompileTypeScript" (fun _ ->
  !! "**/*.ts"
    |> TypeScriptCompiler (fun p -> { p with OutputDir = "./out/" }) 
)

RunTargetOrDefault "CompileTypeScript"