module Fake.DotNet.XdtTests

open System
open System.IO
open System.Text
open Fake.DotNet
open Fake.IO
open Expecto

module TestData =
    let fileName file =
        Path.Combine(__SOURCE_DIRECTORY__, "TestFiles", "Fake.DotNet.Xdt.Files", file)

    let replaceNewLines text =
        RegularExpressions.Regex.Replace(text, @"\r\n?|\n", Environment.NewLine)

    let exists file =
        File.Exists(fileName file)

    let require file =
        if not (exists file) then
            invalidArg "file" (sprintf "Unable to read test data from %s"
                                       (Path.GetFullPath(fileName file)))

    let read file =
        require file
        File.ReadAllText(fileName file, Encoding.UTF8)
        |> replaceNewLines

    let copy source dest =
        require source
        File.Copy(fileName source, fileName dest, true)

    let delete file =
        File.Delete(fileName file)

[<Tests>]
let tests =
    testList "Fake.DotNet.Xdt.Tests" [
        test "when transforming file explicitly" {
            let testFile = "web.new.config"
            try
                let expected = TestData.read "web.transformed.config"
                Xdt.transformFile (TestData.fileName "web.config")
                                  (TestData.fileName "web.test.config")
                                  (TestData.fileName testFile)
                let actual = TestData.read testFile
                Expect.equal actual expected "Expected the transformed file to match"
            finally
                TestData.delete testFile
        }

        test "when transforming file with config name" {
            let confFile = "web.transformFile.config"
            let testFile = "web.transformFile.test.config"
            try
                let expected = TestData.read "web.transformed.config"
                TestData.copy "web.config" confFile
                TestData.copy "web.test.config" testFile
                Xdt.transformFileWithConfigName "test" (TestData.fileName confFile)
                let actual = TestData.read confFile
                Expect.equal actual expected "Expected the transformed file to match"
            finally
                TestData.delete confFile
                TestData.delete testFile
        }

        test "when transforming files with config name" {
            let confFile = "web.transformFiles.config"
            let testFile = "web.transformFiles.test.config"
            try
                let expected = TestData.read "web.transformed.config"
                TestData.copy "web.config" confFile
                TestData.copy "web.test.config" testFile
                let files =
                    GlobbingPattern.create confFile
                    |> GlobbingPattern.setBaseDir (Path.Combine(__SOURCE_DIRECTORY__, "TestFiles", "Fake.DotNet.Xdt.Files"))
                Xdt.transformFilesWithConfigName "test" files
                let actual = TestData.read confFile
                Expect.equal actual expected "Expected the transformed file to match"
            finally
                TestData.delete confFile
                TestData.delete testFile
        }
    ]
