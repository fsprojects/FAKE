module Fake.DotNet.Testing.NUnitTests

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Expecto

[<Tests>]
let tests =
  testList "Fake.DotNet.Testing.NUnit.Tests" [
    testCase "Test that we write and delete arguments file" <| fun _ ->
      let cp =
        NUnit3.createProcess (fun param ->
          { param with
              ToolPath = "mynunit.exe"
              }) [| "assembly.dll" |]
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
      let file, args =
        match Environment.isWindows, Process.monoPath with
        | false, Some s when file = s ->
          Expect.equal args.Args.Length 3 "Expected mono arguments"
          Expect.equal args.Args.[0] "--debug" "Expected --debug flag"
          args.Args.[1], Arguments.OfArgs args.Args.[2..]
        | true, _ -> file, args
        | _ ->
          Trace.traceFAKE "Mono was not found in test!"
          file, args
      Expect.equal file "mynunit.exe" "Expected mynunit.exe"
      Expect.equal args.Args.Length 1 "expected a single argument"
      let arg = args.Args.[0]
      Expect.stringStarts arg "@" "Expected arg to start with @"
      let argFile = arg.Substring(1)
      
      ( use state = cp.Hook.PrepareState()
        let contents = File.ReadAllText argFile
        let args = Args.fromWindowsCommandLine contents
        Expect.sequenceEqual args ["--noheader"; "assembly.dll"] "Expected arg file to be correct"
        )
      Expect.isFalse (File.Exists argFile) "File should be deleted"
  ]
