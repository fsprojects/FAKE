module Fake.Core.VariablesTests

open Fake.Core
open Expecto

[<Tests>]
let tests = 
  testList "Fake.Core.Variables.Tests" [   
    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables" <| fun _ ->
        Variables.set "Test" "TestValue" |> ignore
        let value = Variables.get<string> "Test"
        Expect.isSome value "Variable 'Test' is none"
        Expect.equal "TestValue" value.Value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "When type does not match, errors" <| fun _ ->
        Variables.set "Test" "TestValue" |> ignore
        try
            Variables.get<bool> "Test" |> ignore
            Tests.failtest "Expected exception"
        with e ->
            Expect.equal "Cast error on variable 'Test'" e.Message "Incorrect failure message for variable failure case"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with default - when found" <| fun _ ->
        Variables.set "Test" "TestValue" |> ignore
        let value = Variables.getOrDefault<string> "Test" "DefaultValue"
        Expect.equal "TestValue" value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with default - when not found" <| fun _ ->
        let value = Variables.getOrDefault<string> "Test" "DefaultValue"
        Expect.equal "DefaultValue" value "Variable 'Test' is not the default"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with failure - when found" <| fun _ ->
        Variables.set "Test" "TestValue" |> ignore
        let value = Variables.getOrFail<string> "Test"
        Expect.equal "TestValue" value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with failure - when not found" <| fun _ ->
        try
            Variables.getOrFail<string> "Test" |> ignore
            Tests.failtest "Expected exception"
        with e ->
            Expect.equal "Unable to find variable 'Test'" e.Message "Incorrect failure message for variable failure case"
  ]
