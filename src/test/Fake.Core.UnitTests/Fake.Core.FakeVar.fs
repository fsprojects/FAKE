module Fake.Core.FakeVarTests

open Fake.Core
open Expecto

[<Tests>]
let tests = 
  testList "Fake.Core.FakeVar.Tests" [   
    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables" <| fun _ ->
        FakeVar.set "Test" "TestValue"
        let value = FakeVar.get<string> "Test"
        Expect.isSome value "Variable 'Test' is none"
        Expect.equal "TestValue" value.Value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to remove fake variables after set" <| fun _ ->
        FakeVar.set "Test" "TestValue"
        FakeVar.remove "Test"
        let value = FakeVar.get<string> "Test"
        Expect.isNone value "Variable 'Test' is some"

    Fake.ContextHelper.fakeContextTestCase "When type does not match, errors" <| fun _ ->
        FakeVar.set "Test" "TestValue"
        try
            FakeVar.get<bool> "Test" |> ignore
            Tests.failtest "Expected exception"
        with e ->
            Expect.equal "Cast error on variable 'Test'" e.Message "Incorrect failure message for variable failure case"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with default - when found" <| fun _ ->
        FakeVar.set "Test" "TestValue"
        let value = FakeVar.getOrDefault<string> "Test" "DefaultValue"
        Expect.equal "TestValue" value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with default - when not found" <| fun _ ->
        let value = FakeVar.getOrDefault<string> "Test" "DefaultValue"
        Expect.equal "DefaultValue" value "Variable 'Test' is not the default"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with failure - when found" <| fun _ ->
        FakeVar.set "Test" "TestValue"
        let value = FakeVar.getOrFail<string> "Test"
        Expect.equal "TestValue" value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to set and get fake variables with failure - when not found" <| fun _ ->
        try
            FakeVar.getOrFail<string> "Test" |> ignore
            Tests.failtest "Expected exception"
        with e ->
            Expect.equal "Unable to find variable 'Test'" e.Message "Incorrect failure message for variable failure case"   

    Fake.ContextHelper.fakeContextTestCase "Ability to define variable" <| fun _ ->
        let myGet, _, mySet = FakeVar.define<string> "Test"
        mySet "TestValue"
        let value = myGet()
        Expect.isSome value "Variable 'Test' is none"
        Expect.equal "TestValue" value.Value "Variable 'Test' is incorrect"

    Fake.ContextHelper.fakeContextTestCase "Ability to define variable allowing non context" <| fun _ ->
        let myGet, _, mySet = FakeVar.defineAllowNoContext<string> "Test"
        mySet "TestValue"
        let value = myGet()
        Expect.isSome value "Variable 'Test' is none"
        Expect.equal "TestValue" value.Value "Variable 'Test' is incorrect"

    testCase "Ability to define variable with no context" <| fun _ ->
        let myGet, _, mySet = FakeVar.defineAllowNoContext<string> "Test"
        mySet "TestValue"
        let value = myGet()
        Expect.isSome value "Variable 'Test' is none"
        Expect.equal "TestValue" value.Value "Variable 'Test' is incorrect"

    testCase "Ability to define variable with no context when context required" <| fun _ ->
        let myGet, _, _ = FakeVar.define<string> "Test"
        try
            let result = myGet()
            Tests.failtest (sprintf "Expected exception, but got '%A'" result)
        with e ->
            Expect.equal "Cannot retrieve 'Test' as we have no fake context" e.Message "Incorrect failure message for variable failure case"
  ]
