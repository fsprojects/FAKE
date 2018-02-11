module Fake.IO.ZipTests

open Fake.Core
open Fake.IO
open Expecto

[<Tests>]
let tests = 
  testList "Fake.IO.Zip.Tests" [
    testCase "Test simple Zip.FilesAsSpec" <| fun _ ->
      
      let globExe = // !! "folder/*.exe"
          { Fake.Core.Globbing.ResolvedGlobbingPattern.BaseDirectory = "."
            Fake.Core.Globbing.ResolvedGlobbingPattern.Includes = [ "folder/*.exe" ]
            Fake.Core.Globbing.ResolvedGlobbingPattern.Excludes = []
            Fake.Core.Globbing.ResolvedGlobbingPattern.Results = 
              [ "folder/file1.exe"
                "folder/file2.exe" ] }
      let actual =
          globExe
          |> Zip.FilesAsSpecs "folder"
          |> Zip.MoveToFolder "renamedfolder"
          |> Seq.toList
      
      let expected = [ @"folder/file1.exe", @"renamedfolder\file1.exe";
                       @"folder/file2.exe", @"renamedfolder\file2.exe"]
      Expect.equal actual expected "FilesAsSpecs failed."
      
    testCase "Test simple Zip.FilesAsSpec (2)" <| fun _ ->
      let globSubFolder = // !! "subfolder/*/*.dll"
          { Fake.Core.Globbing.ResolvedGlobbingPattern.BaseDirectory = "."
            Fake.Core.Globbing.ResolvedGlobbingPattern.Includes = [ "subfolder/*/*.dll" ]
            Fake.Core.Globbing.ResolvedGlobbingPattern.Excludes = []
            Fake.Core.Globbing.ResolvedGlobbingPattern.Results = 
              [ "subfolder/1/file1.dll"
                "subfolder/1/file2.dll"
                "subfolder/2/file2.dll" ] } 
      let actual =
          globSubFolder
          |> Zip.FilesAsSpecs "subfolder"
          |> Seq.toList
      
      let expected =
        [ @"subfolder/1/file1.dll", @"1\file1.dll"
          @"subfolder/1/file2.dll", @"1\file2.dll"
          @"subfolder/2/file2.dll", @"2\file2.dll" ]
      Expect.equal actual expected "FilesAsSpecs failed."
  ]    