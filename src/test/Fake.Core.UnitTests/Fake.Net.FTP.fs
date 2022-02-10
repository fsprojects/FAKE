module Fake.Net.FTPTests

open Expecto
open Fake.Net

[<Tests>]
let tests =
  let directoryNameTestCases =
        [ "invalid<name", false
          "invalid>name", false
          "invalid:name", false
          "invalid/name", false
          "invalid\\name", false
          "invalid\"name", false
          "invalid|name", false
          "invalid?name", false
          "invalid*name", false
          "invalidname ", false
          "invalidname.", false
          "CON", false
          "LPT4", false
          "nul", false
          "valid-name", true]
        |> List.map (fun (directoryName, expected) ->
          testCase $"Validate directory name '%s{directoryName}'" <| fun _ ->
            let isValid = FTP.isValidDirectoryName directoryName
            Expect.equal isValid expected "expected proper directory name validation")

  testList "Fake.Net.FTP.Tests" [
        yield! directoryNameTestCases
  ]
