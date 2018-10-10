module Fake.ContextHelper

open Fake.Core
open Expecto

let fakeContextTestCase name f =
    testCase name <| fun arg ->
      use execContext = Fake.Core.Context.FakeExecutionContext.Create false (sprintf "text.fsx - %s" name) []
      Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
      try f arg
      finally 
        Fake.Core.Context.removeExecutionContext()