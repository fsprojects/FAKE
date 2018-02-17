module Fake.DotNet.CliTests

open Fake.Core
open Fake.DotNet
open Expecto

[<Tests>]
let tests = 
  testList "Fake.DotNet.Cli.Tests" [
    testCase "Test that we can use Process-Helpers on Cli Paramters" <| fun _ ->
      let cli =
        Cli.DotNetOptions.Create()
        |> Process.setEnvironmentVariable "Somevar" "someval"
      
      Expect.equal cli.Environment.["Somevar"] "someval" "Retrieving the correct environment variable failed."
  ]
  