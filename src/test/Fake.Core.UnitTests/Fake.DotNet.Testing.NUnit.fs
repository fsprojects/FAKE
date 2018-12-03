module Fake.DotNet.Testing.NUnitTests

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Testing
open Expecto

[<Tests>]
let tests =
  testList "Fake.DotNet.Testing.NUnit.Tests" [
    testCase "Test that we write and delete arguments file" <| fun _ ->
      let cp =
        NUnit3.createProcess Path.GetTempFileName (fun param ->
          { param with
              ToolPath = "mynunit.exe"
              }) [| "assembly.dll" |]
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        |> ArgumentHelper.checkIfMono
      Expect.equal file "mynunit.exe" "Expected mynunit.exe"
      Expect.equal (args |> Arguments.toArray).Length 1 "expected a single argument"
      let arg = (args |> Arguments.toArray).[0]
      Expect.stringStarts arg "@" "Expected arg to start with @"
      let argFile = arg.Substring(1)
      
      ( use state = cp.Hook.PrepareState()
        let contents = File.ReadAllText argFile
        let args = Args.fromWindowsCommandLine contents
        Expect.sequenceEqual args ["--noheader"; "assembly.dll"] "Expected arg file to be correct"
        )
      Expect.isFalse (File.Exists argFile) "File should be deleted"
      
    testCase "Test that we support file-paths with space - #2180" <| fun _ ->
      let d = Directory.CreateDirectory "some path"
      let cp =
        NUnit3.createProcess (fun _ -> "some path/with spaces.txt") (fun param ->
          { param with
              ToolPath = "mynunit.exe"
              }) [| "assembly.dll" |]
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        |> ArgumentHelper.checkIfMono
      Expect.equal file "mynunit.exe" "Expected mynunit.exe"
      Expect.equal (args |> Arguments.toArray).Length 1 "expected a single argument"
      Expect.equal (args |> Arguments.toArray).[0] "@some path/with spaces.txt"
      let argFile = (args |> Arguments.toArray).[0].Substring(1)
      
      ( use state = cp.Hook.PrepareState()
        let contents = File.ReadAllText argFile
        let args = Args.fromWindowsCommandLine contents
        Expect.sequenceEqual args ["--noheader"; "assembly.dll"] "Expected arg file to be correct"
        )
      Expect.isFalse (File.Exists argFile) "File should be deleted"
      d.Delete()
  ]
