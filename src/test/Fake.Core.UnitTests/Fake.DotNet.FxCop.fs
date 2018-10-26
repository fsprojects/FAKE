module Fake.DotNet.FxCop

open Fake.Core
open Fake.DotNet
open Expecto

[<Tests>]
let tests =
  testList "Fake.DotNet.FxCop.Tests" [
    testCase "Test that we can run a test" <| fun _ ->
      let p = FxCop.Params.Create()
      Expect.isFalse (p.IncludeSummaryReport) "This test should fail"
  ]
