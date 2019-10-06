module Fake.DotNet.Testing.VSTestTests

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Testing
open Expecto

[<Tests>]
let tests =
  testList "Fake.DotNet.Testing.VSTest.Tests" [
    testCase "Test that we write and delete arguments file" <| fun _ ->
      let cp =
        VSTest.createProcess Path.GetTempFileName (fun param ->
          { param with
              ToolPath = "vstest.exe"
              }) [| "assembly.dll" |]
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        |> ArgumentHelper.checkIfMono
      Expect.equal file "vstest.exe" "Expected vstest.exe"
      Expect.equal (args |> Arguments.toArray).Length 1 "expected a single argument"
      let arg = (args |> Arguments.toArray).[0]
      Expect.stringStarts arg "@" "Expected arg to start with @"
      let argFile = arg.Substring(1)
      
      ( use _state = cp.Hook.PrepareState()
        let contents = File.ReadAllText argFile
        let args = Args.fromWindowsCommandLine contents
        Expect.sequenceEqual args ["assembly.dll"; "/InIsolation"] "Expected arg file to be correct"
        )
      Expect.isFalse (File.Exists argFile) "File should be deleted"

    testCase "Test that we can set Parallel setting" <| fun _ ->
        let cp =
          VSTest.createProcess Path.GetTempFileName (fun param ->
            { param with
                ToolPath = "vstest.console.exe"
                Parallel = true
                }) [| "assembly1.dll"; "assembly2.dll" |]
        let file, args =
          match cp.Command with
          | RawCommand(file, args) -> file, args
          | _ -> failwithf "expected RawCommand"
          |> ArgumentHelper.checkIfMono
        Expect.equal file "vstest.console.exe" "Expected vstest.console.exe"
        Expect.equal (args |> Arguments.toArray).Length 1 "expected a single argument"
        let arg = (args |> Arguments.toArray).[0]
        Expect.stringStarts arg "@" "Expected arg to start with @"
        let argFile = arg.Substring(1)
        
        ( use _state = cp.Hook.PrepareState()
          let contents = File.ReadAllText argFile
          let args = Args.fromWindowsCommandLine contents
          Expect.sequenceEqual args ["assembly1.dll"; "assembly2.dll"; "/Parallel"; "/InIsolation"] "Expected arg file to be correct"
          )
        Expect.isFalse (File.Exists argFile) "File should be deleted"
  ]
