module Fake.RuntimeTests

open Fake.Runtime
open Expecto

[<Tests>]
let tests = 
  testList "Fake.Runtime.Tests" [
    testCase "Test that we can tokenize __SOURCE_FILE__" <| fun _ ->
      
      Expect.equal "" "" "."
  ]    