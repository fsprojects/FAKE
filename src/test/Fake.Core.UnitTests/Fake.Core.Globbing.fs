module Fake.Core.GlobbingTests

open System.IO
open Fake.Core
open Fake.Core.Globbing
open Fake.IO
open Expecto

[<Tests>]
let tests = 
  testList "Fake.Core.Globbing.Tests" [
    testCase "Test IsMatch works on relative Paths - #1029" <| fun _ ->
      let globExe = // !! "folder/*.exe"
          { Fake.Core.Globbing.ResolvedGlobbingPattern.BaseDirectory = Path.GetFullPath "."
            Fake.Core.Globbing.ResolvedGlobbingPattern.Includes = [ "folder/*.exe" ]
            Fake.Core.Globbing.ResolvedGlobbingPattern.Excludes = []
            Fake.Core.Globbing.ResolvedGlobbingPattern.Results = 
              [ "folder/file1.exe"
                "folder/file2.exe" ] }
      Expect.equal (globExe.IsMatch "folder/test.exe") true "Glob should match relative paths"
      Expect.equal (globExe.IsMatch (Path.GetFullPath "folder/test.exe")) true "Glob should match full paths"
  ]