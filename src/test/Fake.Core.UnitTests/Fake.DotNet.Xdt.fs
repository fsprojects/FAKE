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
    testSequenced <| testList "Fake.DotNet.Xdt.Tests" [
        test "when transforming file explicitly" {
            try
                let expected = TestData.read "web.transformed.config"
                Xdt.transformFile (TestData.fileName "web.config")
                                  (TestData.fileName "web.test.config")
                                  (TestData.fileName "web.new.config")
                let actual = TestData.read "web.new.config"
                Expect.equal actual expected "Expected the transformed file to match"
            finally
                TestData.delete "web.new.config"
        }

        test "when transforming file with config name" {
            try
                let expected = TestData.read "web.transformed.config"
                Xdt.transformFileWithConfigName "test" (TestData.fileName "web.config")
                let actual = TestData.read "web.config"
                Expect.equal actual expected "Expected the transformed file to match"
            finally
                TestData.copy "web.original.config" "web.config"
        }

        test "when transforming files with config name" {
            try
                let expected = TestData.read "web.transformed.config"
                let files =
                    TestData.fileName "web.config"
                    |> GlobbingPattern.create
                    |> GlobbingPattern.setBaseDir (Path.Combine(__SOURCE_DIRECTORY__, "TestFiles", "Fake.DotNet.Xdt.Files"))
                Xdt.transformFilesWithConfigName "test" files
                let actual = TestData.read "web.config"
                Expect.equal actual expected "Expected the transformed file to match"
            finally
                TestData.copy "web.original.config" "web.config"
        }
    ]
