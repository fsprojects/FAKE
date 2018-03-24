module Fake.Core.GlobbingTests

open System.IO
open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Expecto
open Fake.IO.Globbing.Glob
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
      Expect.equal (globExe.IsMatch "folder/test.exe") true "Glob should match relative paths"
      Expect.equal (globExe.IsMatch (Path.GetFullPath "folder/test.exe")) true "Glob should match full paths"
    testCase "It should resolve multiple directories" <| fun _ ->
        let fileIncludes = getFileIncludeWithKnownBaseDir [@"test1\bin\*.dll"; @"test2\bin\*.dll"]
        let dirIncludes = GlobbingPattern.getBaseDirectoryIncludes(fileIncludes)
        Expect.equal 2 dirIncludes.Length "Should have 2 dirs"
        Expect.contains dirIncludes (normalizePath(@"C:\Project\test1\bin")) "Should contain first folder"
        Expect.contains dirIncludes (normalizePath(@"C:\Project\test2\bin")) "Should contain second folder"

    testCase "should only take the most root path when multiple directories share a root" <| fun _ ->
        let fileIncludes = getFileIncludeWithKnownBaseDir [@"tests\**\test1\bin\*.dll"; @"tests\test2\bin\*.dll"]
        let dirIncludes = GlobbingPattern.getBaseDirectoryIncludes(fileIncludes)
        Expect.equal 1 dirIncludes.Length "Should have only 1 directory"
        Expect.contains dirIncludes (normalizePath(@"C:\Project\tests")) "Should contain tests folder"
  ]
