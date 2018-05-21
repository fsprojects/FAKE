module Fake.Core.GlobbingTests

open System.IO
open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Expecto
open Expecto.Flip
open Fake.IO.Globbing.Glob
open System.ComponentModel
open System.ComponentModel
open System.IO
let getFileIncludeWithKnownBaseDir includes : LazyGlobbingPattern=
    { Fake.IO.Globbing.LazyGlobbingPattern.BaseDirectory = @"C:\Project"
      Fake.IO.Globbing.LazyGlobbingPattern.Includes = includes
      Fake.IO.Globbing.LazyGlobbingPattern.Excludes = [] } 

[<Tests>]
let tests = 
  testList "Fake.Core.Globbing.Tests" [
    testCase "Test IsMatch works on relative Paths - #1029" <| fun _ ->
      let globExe = // !! "folder/*.exe"
          { Fake.IO.Globbing.ResolvedGlobbingPattern.BaseDirectory = Path.GetFullPath "."
            Fake.IO.Globbing.ResolvedGlobbingPattern.Includes = [ "folder/*.exe" ]
            Fake.IO.Globbing.ResolvedGlobbingPattern.Excludes = []
            Fake.IO.Globbing.ResolvedGlobbingPattern.Results = 
              [ "folder/file1.exe"
                "folder/file2.exe" ] }
      Expect.equal "Glob should match relative paths" true (globExe.IsMatch "folder/test.exe")
      Expect.equal "Glob should match full paths" true (globExe.IsMatch (Path.GetFullPath "folder/test.exe"))
    testCase "It should resolve multiple directories" <| fun _ ->
        let fileIncludes = getFileIncludeWithKnownBaseDir [@"test1\bin\*.dll"; @"test2\bin\*.dll"]
        let dirIncludes = GlobbingPattern.getBaseDirectoryIncludes(fileIncludes)
        Expect.equal "Should have 2 dirs" dirIncludes.Length 2
        Expect.contains "Should contain first folder" (normalizePath(@"C:\Project\test1\bin")) dirIncludes
        Expect.contains "Should contain second folder" (normalizePath(@"C:\Project\test2\bin")) dirIncludes

    testCase "should only take the most root path when multiple directories share a root" <| fun _ ->
        let fileIncludes = getFileIncludeWithKnownBaseDir [@"tests\**\test1\bin\*.dll"; @"tests\test2\bin\*.dll"]
        let dirIncludes = GlobbingPattern.getBaseDirectoryIncludes(fileIncludes)
        Expect.equal "Should have only 1 directory" dirIncludes.Length 1
        Expect.contains "Should contain tests folder" (normalizePath(@"C:\Project\tests")) dirIncludes

    testCase "glob should handle substring directories properly" <| fun _ ->
        let testDir = Path.GetTempFileName()
        File.Delete testDir
        Directory.CreateDirectory testDir |> ignore
        try
          let name = testDir </> "Name"
          let nameWithSuffix = testDir </> "NameWithSuffix"
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
          |> Expect.equal "Expected equal lists." ["match1.txt"; "match2.txt"; "match3.txt"]
        finally
          Directory.Delete(testDir, true)
  ]
