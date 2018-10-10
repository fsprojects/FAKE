module Fake.DotNet.Testing.SpecFlowTests

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Testing
open Expecto

[<Tests>]
let tests =
  testList "Fake.DotNet.Testing.SpecFlow.Tests" [
    testCase "Test that new argument generation works" <| fun _ ->
      let p, cp =
        SpecFlowNext.createProcess (fun param ->
          { param with
              ToolPath = "specflow"
              SubCommand = SpecFlowNext.MsTestExecutionReport
              })

      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        //|> ArgumentHelper.checkIfMono
      let expectedPath =
        if Path.Combine("specflow", "specflow.exe")
      Expect.equal file expectedPath "Expected specflow.exe"
      Expect.equal cp.Command.CommandLine 
        (sprintf "%s MsTestExecutionReport" expectedPath) "expected proper command line"
  ]
