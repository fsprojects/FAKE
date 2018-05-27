module Fake.ContextHelper

open Fake.Core
open Expecto

let fakeContextTestCase name f =
    testCase name <| fun arg ->
      use execContext = Fake.Core.Context.FakeExecutionContext.Create false "text.fsx" []
      Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
      f arg
