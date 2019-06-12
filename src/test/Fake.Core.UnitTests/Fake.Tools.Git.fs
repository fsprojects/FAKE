module Fake.Tools.Git.Tests

open System
open System.IO
open Fake.Core
open Fake.IO.FileSystemOperators
open Expecto

[<Tests>]
let tests =
    let versionTestCases =
        [ "git version 2.3.2 (Apple Git-55)", "2.3.2"
          "git version 2.4.9", "2.4.9"
          "git version 400.44312.9 (Apple Git-60)", "400.44312.9" ]
        |> List.map (fun (v, expected) ->
          testCase (sprintf "Can parse git version output '%s' - #911" v) <| fun _ ->
            let version = Information.extractGitVersion v
            Expect.equal version (SemVer.parse expected) "Version should match")

    testList "Fake.Tools.Git.Tests" [
        yield! versionTestCases

        yield testCase "findGitDir finds .git directory if one exists" <| fun _ ->
            let testDir = Path.GetTempFileName()
            File.Delete testDir
            Directory.CreateDirectory testDir |> ignore
            try
                let gitDir = testDir </> ".git"
                let childDir = testDir </> "child"
                Directory.CreateDirectory gitDir |> ignore
                Directory.CreateDirectory childDir |> ignore

                let result = CommandHelper.findGitDir childDir

                Expect.equal result.FullName gitDir "Found .git directory does not match"
            finally
                Directory.Delete(testDir, true)

        yield testCase "findGitDir throws invalidOp if no .git directory exists" <| fun _ ->
            let testDir = Path.GetTempFileName()
            File.Delete testDir
            Directory.CreateDirectory testDir |> ignore
            try
                let childDir = testDir </> "child"
                Directory.CreateDirectory childDir |> ignore

                try
                    let dir = CommandHelper.findGitDir childDir
                    // This shouldn't happen but on my system it will ;)
                    eprintfn "Found git dir '%s', testcase 'findGitDir throws invalidOp if no .git directory exists' is basically ignored!" dir.FullName
                    let parent = dir.Parent
                    Expect.stringStarts (Path.GetFullPath childDir) parent.FullName "Expected childDir to be a subdirectory of the found dir"
                    Expect.isTrue dir.Exists (sprintf "Expected the result directory '%s' to exist" dir.FullName)
                with :? InvalidOperationException ->
                    // expected
                    ()
            finally
                Directory.Delete(testDir, true)
   ]
