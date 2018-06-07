module Fake.IO.ZipTests

open Fake.IO
open Fake.IO.FileSystemOperators
open Expecto

[<Tests>]
let tests = 
  testList "Fake.IO.Zip.Tests" [
    testCase "Test simple Zip.FilesAsSpec - #1014" <| fun _ ->
      
      let globExe = // !! "folder/*.exe"
          { Globbing.ResolvedGlobbingPattern.BaseDirectory = "."
            Globbing.ResolvedGlobbingPattern.Includes = [ "folder/*.exe" ]
            Globbing.ResolvedGlobbingPattern.Excludes = []
            Globbing.ResolvedGlobbingPattern.Results = 
              [ "folder/file1.exe"
                "folder/file2.exe" ] }
      let actual =
          globExe
          |> Zip.filesAsSpecs "folder"
          |> Zip.moveToFolder "renamedfolder"
          |> Seq.toList
      
      let expected = [ @"folder/file1.exe", @"renamedfolder"</>"file1.exe";
                       @"folder/file2.exe", @"renamedfolder"</>"file2.exe"]
      Expect.equal actual expected "FilesAsSpecs failed."
      
    testCase "Test simple Zip.FilesAsSpec (2) - #1014" <| fun _ ->
      let globSubFolder = // !! "subfolder/*/*.dll"
          { Globbing.ResolvedGlobbingPattern.BaseDirectory = "."
            Globbing.ResolvedGlobbingPattern.Includes = [ "subfolder/*/*.dll" ]
            Globbing.ResolvedGlobbingPattern.Excludes = []
            Globbing.ResolvedGlobbingPattern.Results = 
              [ "subfolder/1/file1.dll"
                "subfolder/1/file2.dll"
                "subfolder/2/file2.dll" ] } 
      let actual =
          globSubFolder
          |> Zip.filesAsSpecs "subfolder"
          |> Seq.toList
      
      let expected =
        [ @"subfolder/1/file1.dll", @"1"</>"file1.dll"
          @"subfolder/1/file2.dll", @"1"</>"file2.dll"
          @"subfolder/2/file2.dll", @"2"</>"file2.dll" ]
      Expect.equal actual expected "FilesAsSpecs failed."
  ]    
