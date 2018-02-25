module Fake.IO.FileSystem.ChangeWatcherTests

open Fake.IO.FileSystem
open Fake.DotNet
open Expecto

[<Tests>]
let tests = 
  testList "Fake.IO.FileSystem.ChangeWatcher.Tests" [
    testCase "It should watch multiple directories" <| fun _ ->
        let fileIncludes:LazyGlobbingPattern = {
            BaseDirectory =@"C:\Project"
            Includes = [@"test1\bin\*.dll"; @"test2\bin\*.dll"]
            Excludes = [] } 
        let dirsToWatch = ChangeWatcher.calcDirsToWatch(fileIncludes)
        Expect.equal 2 dirsToWatch.Length "Should have 2 dirs to watch"
        Expect.contains dirsToWatch Fake.EnvironmentHelper.normalizePath(@"C:\Project\test1\bin") "Should contain first folder"
        Expect.contains dirsToWatch Fake.EnvironmentHelper.normalizePath(@"C:\Project\test2\bin") "Should contain second folder"

    testCase "should only take the most root path when multiple directories share a root" <| fun _ ->
        let includes:LazyGlobbingPattern = {
            BaseDirectory =@"C:\Project"
            Includes = [@"tests\**\test1\bin\*.dll"; @"tests\test2\bin\*.dll"]
            Excludes = [] } 
        let dirsToWatch = ChangeWatcher.calcDirsToWatch(fileIncludes)
        Expect.equal 1 dirsToWatch.Length "Should have 1 dir to watch"
        Expect.contains dirsToWatch Fake.EnvironmentHelper.normalizePath(@"C:\Project\tests") "Should contain tests folder"
  ]
  