module Fake.Core.ContextTests

open System
open Fake.Core
open Expecto
open Expecto.Flip
open FParsec.ErrorMessage

[<Tests>]
let tests = 
  testList "Fake.Core.Context.Tests" [
    testCase "Test that forceFakeContext works or throws properly" <| fun _ ->
        let c =
           let f = Fake.Core.Context.FakeExecutionContext.Create false "C:\\Testfile" []
           Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake f)
           let myC = Fake.Core.Context.forceFakeContext()
           Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Unknown)
           myC
        try
           Fake.Core.Context.forceFakeContext() |> ignore
           Tests.failtest "Expected exception"
        with :? System.InvalidOperationException as e -> ()
    
    testCase "Ability to set and get fake variables" <| fun _ ->
        Fake.Core.Context.setFakeVar "Test" "TestValue" |> ignore
        let value = Fake.Core.Context.getFakeVar<string> "Test"
        Expect.isSome "FakeVar 'Test' is none" value
        Expect.equal "TestValue" value.Value "FakeVar 'Test' is incorrect"

    testCase "Ability to set and get fake variables with default - when found" <| fun _ ->
        Fake.Core.Context.setFakeVar "Test" "TestValue" |> ignore
        let value = Fake.Core.Context.getFakeVarOrDefault<string> "Test" "DefaultValue"
        Expect.equal "TestValue" value "FakeVar 'Test' is incorrect"

    testCase "Ability to set and get fake variables with default - when not found" <| fun _ ->
        let value = Fake.Core.Context.getFakeVarOrDefault<string> "Test" "DefaultValue"
        Expect.equal "DefaultValue" value "FakeVar 'Test' is not the default"

    testCase "Ability to set and get fake variables with failure - when found" <| fun _ ->
        Fake.Core.Context.setFakeVar "Test" "TestValue" |> ignore
        let value = Fake.Core.Context.getFakeVarOrFail<string> "Test"
        Expect.equal "TestValue" value "FakeVar 'Test' is incorrect"

    testCase "Ability to set and get fake variables with default - when not found" <| fun _ ->
        try
            Fake.Core.Context.getFakeVarOrFail<string> "Test" |> ignore
            Tests.failtest "Expected exception"
        with e ->
            Expect.equal "Unable to find FakeVar 'Test'" e.Message "Incorrect failure message for FakeVar failure case"
  ]
