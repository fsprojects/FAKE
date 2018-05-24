module Fake.Core.ContextTests

open System
open Fake.Core
open Expecto
open Expecto.Flip

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

  ]
