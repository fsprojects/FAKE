module Fake.Core.GlobbingTests

open System
open System.IO
open System.Reflection
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Expecto

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
      Expect.equal true (globExe.IsMatch "folder/test.exe") "Glob should match relative paths"
      Expect.equal true (globExe.IsMatch (Path.GetFullPath "folder/test.exe")) "Glob should match full paths"
    testCase "It should resolve multiple directories" <| fun _ ->
        let fileIncludes = getFileIncludeWithKnownBaseDir [@"test1\bin\*.dll"; @"test2\bin\*.dll"]
        let dirIncludes = GlobbingPattern.getBaseDirectoryIncludes(fileIncludes)
        Expect.equal dirIncludes.Length 2 "Should have 2 dirs"
        Expect.contains dirIncludes (Glob.normalizePath(@"C:\Project\test1\bin"))  "Should contain first folder"
        Expect.contains dirIncludes (Glob.normalizePath(@"C:\Project\test2\bin")) "Should contain second folder"

    testCase "should only take the most root path when multiple directories share a root" <| fun _ ->
        let fileIncludes = getFileIncludeWithKnownBaseDir [@"tests\**\test1\bin\*.dll"; @"tests\test2\bin\*.dll"]
        let dirIncludes = GlobbingPattern.getBaseDirectoryIncludes(fileIncludes)
        Expect.equal dirIncludes.Length 1 "Should have only 1 directory"
        Expect.contains  dirIncludes (Glob.normalizePath(@"C:\Project\tests")) "Should contain tests folder"

    testCase "base directory includes should include two when one's name include the other #2230 " <| fun _ ->
        let fileIncludes = getFileIncludeWithKnownBaseDir [@"test1\*.dll"; @"test\*.dll"]
        let dirIncludes = GlobbingPattern.getBaseDirectoryIncludes(fileIncludes)
        Expect.equal dirIncludes.Length 2 "Should have only 2 directory"

        
  ]
