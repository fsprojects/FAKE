module Fake.IO.GlobbingIntegrationTests

open Fake.IO
open System.IO
open Expecto
open Fake.Core.IntegrationTests.TestHelpers
open System.Reflection
open System
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

[<Tests>]
let toolsTests = 

  // Sequenced because otherwise folders conflict with the globbing pattern '!! "./**"' in the impl.
  testSequencedGroup "Find tool paths" <|
    testList "Fake.Core.Globbing.Tools.Tests" [
      testCase "Test try find tool folder in sub path" <| fun _ ->
        use testDir = createTestDirInCurrent()
        let folder = testDir.Dir

        let filepath = folder </> "sometool"
        File.create filepath |> ignore

        Tools.tryFindToolFolderInSubPath "sometool"
        |> Flip.Expect.equal "Expected tools folder to be found" (Some folder)

      testCase "Test cannot find tool folder in sub path" <| fun _ ->
        Tools.tryFindToolFolderInSubPath "SomeMissingTool"
        |> Flip.Expect.isNone "Expected tools folder not to be found"

      testCase "Test find tool folder in sub path" <| fun _ ->
        use testDir = createTestDirInCurrent()
        let folder = testDir.Dir

        let filepath = folder </> "sometool"
        File.create filepath |> ignore

        Tools.findToolFolderInSubPath "sometool" "defaultToolsPath"
        |> Flip.Expect.equal "Expected tools folder to be found" folder

      testCase "Test cannot find tool folder in sub path returns default" <| fun _ ->
        Tools.findToolFolderInSubPath "SomeMissingTool" "defaultpath"
        |> Flip.Expect.equal "Expected default path to be returned" "defaultpath"
    ]
    
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
