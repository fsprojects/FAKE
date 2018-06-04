module Fake.Core.VariablesTests

open Fake.Core.Variables
open Expecto

[<Tests>]
let tests = 
  testList "Fake.Core.Variables.Tests" [   
    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables" <| fun _ ->
        set "Test" "TestValue" |> ignore
        let value = get<string> "Test"
        Expect.isSome value "Variable 'Test' is none"
        Expect.equal "TestValue" value.Value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with default - when found" <| fun _ ->
        set "Test" "TestValue" |> ignore
        let value = getOrDefault<string> "Test" "DefaultValue"
        Expect.equal "TestValue" value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with default - when not found" <| fun _ ->
        let value = getOrDefault<string> "Test" "DefaultValue"
        Expect.equal "DefaultValue" value "Variable 'Test' is not the default"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with failure - when found" <| fun _ ->
        set "Test" "TestValue" |> ignore
        let value = getOrFail<string> "Test"
        Expect.equal "TestValue" value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with failure - when not found" <| fun _ ->
        try
            getOrFail<string> "Test" |> ignore
            Tests.failtest "Expected exception"
        with e ->
            Expect.equal "Unable to find variable 'Test'" e.Message "Incorrect failure message for variable failure case"
  ]
