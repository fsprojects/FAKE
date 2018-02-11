module Fake.Tools.Git.Tests

open Fake.Core
open Expecto

[<Tests>]
let tests =
  let versionTestCases = 
    [ "git version 2.3.2 (Apple Git-55)", "2.3.2"
      "git version 2.4.9", "2.4.9"
      "git version 400.44312.9 (Apple Git-60)", "400.44312.9" ]
    |> List.map (fun (v, expected) ->
      testCase (sprintf "Can parse git version output '%s' - #911" v) <| fun _ ->
        let version = Fake.Tools.Git.Information.extractGitVersion v
        Expect.equal version (SemVer.parse expected) "Version should match")

  let otherTestCases =
    [
      //testCase "I fail" <| fun _ ->
      //  Expect.equal false true "example failure"
    ]

  versionTestCases @ otherTestCases
  |> testList "Fake.Tools.Git.Tests" 
