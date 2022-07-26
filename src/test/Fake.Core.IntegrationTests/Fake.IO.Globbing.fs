module Fake.IO.GlobbingIntegrationTests

open Fake.IO
open System.IO
open Expecto
open Fake.Core.IntegrationTests.TestHelpers
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

[<Tests>]
let tests = 
    testList "Fake.Core.Globbing.IntegrationTests" [
        testCase "glob should handle substring directories properly" <| fun _ ->
            use testDir = createTestDir()
            let name = testDir.Dir </> "Name"
            let nameWithSuffix = testDir.Dir </> "NameWithSuffix"
            Directory.CreateDirectory name |> ignore
            Directory.CreateDirectory nameWithSuffix |> ignore
            File.WriteAllText(nameWithSuffix </> "match1.txt", "match1")
            File.WriteAllText(nameWithSuffix </> "match2.txt", "match2")
            File.WriteAllText(nameWithSuffix </> "match3.txt", "match3")

            !! (nameWithSuffix </> "match*.txt")
            |> GlobbingPattern.setBaseDir name
            |> Seq.map (fun f -> Path.GetFileName f)
            |> Seq.sort
            |> Seq.toList
            |> Flip.Expect.equal "Expected equal lists." ["match1.txt"; "match2.txt"; "match3.txt"]
    ]
