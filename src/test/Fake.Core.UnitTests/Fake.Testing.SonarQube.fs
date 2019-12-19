module Fake.Testing.SonarQubeTests

open Fake.Core
open Fake.Testing
open Expecto

[<Tests>]
let tests =
  testList "Fake.Testing.SonarQube.Tests" [
    testCase "Test that new argument generation  with default parameters" <| fun _ ->
      let cmd =
        SonarQube.getSonarQubeCallParams SonarQube.Begin { SonarQube.SonarQubeDefaults with Organization = Some "test space" }
        |> Arguments.toStartInfo

      Expect.equal cmd
        (sprintf "begin /v:\"1.0\" /o:\"test space\"") "expected proper command line for sonarqube runner"

  ]
