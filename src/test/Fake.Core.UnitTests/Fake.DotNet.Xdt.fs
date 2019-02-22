module Fake.DotNet.XdtTests

open System
open System.IO
open System.Text
open Fake.DotNet
open Fake.IO
open Expecto
open Fake.SystemHelper

module TestData =
    let testFilePath file =
        Path.Combine(__SOURCE_DIRECTORY__, "TestFiles", "Fake.DotNet.Xdt.Files", file)

    let replaceNewLines text =
        RegularExpressions.Regex.Replace(text, @"\r\n?|\n", Environment.NewLine)

    let exists file =
        File.Exists(file)

    let require file =
        if not (exists file) then
            invalidArg "file" (sprintf "Unable to read test data from %s"
                                       (Path.GetFullPath(file)))

    let read file =
        require file
        File.ReadAllText(file, Encoding.UTF8)
        |> replaceNewLines

    let copy source dest =
        require source
        File.Copy(source, dest, true)

    let delete file =
        File.Delete(file)

    let withTestDir f =
        let tempFolder = Path.GetTempFileName()
        File.Delete(tempFolder)
        Directory.CreateDirectory(tempFolder)
            |> ignore
        try
            f tempFolder
        finally
            try
                Directory.Delete(tempFolder, true)
            with
            | :? DirectoryNotFoundException -> ()

open Fake.IO.FileSystemOperators

[<Tests>]
let tests =
    let configFile = TestData.testFilePath "web.config"
    let testTransformFile = TestData.testFilePath "web.test.config"
    let expectedTransformed = TestData.testFilePath "web.transformed.config"
    testList "Fake.DotNet.Xdt.Tests" [
        test "when transforming file explicitly" {
            TestData.withTestDir (fun dir ->
                let testFile = dir </> "web.new.config"
                let expected = TestData.read expectedTransformed
                Xdt.transformFile configFile
                                  testTransformFile
                                  testFile
                let actual = TestData.read testFile
                Expect.equal actual expected "Expected the transformed file to match"
            )
        }

        test "when transforming file with config name" {
            TestData.withTestDir (fun dir ->
                let confFile = dir </> "web.transformFile.config"
                let testFile = dir </> "web.transformFile.test.config"
                let expected = TestData.read expectedTransformed
                TestData.copy configFile confFile
                TestData.copy testTransformFile testFile
                Xdt.transformFileWithConfigName "test" confFile
                let actual = TestData.read confFile
                Expect.equal actual expected "Expected the transformed file to match"
            )            
        }

        test "when transforming files with config name" {
            TestData.withTestDir (fun dir ->
                let confFile = dir </> "web.transformFiles.config"
                let testFile = dir </> "web.transformFiles.test.config"
                let expected = TestData.read expectedTransformed
                TestData.copy configFile confFile
                TestData.copy testTransformFile testFile
                let files =
                    GlobbingPattern.create "web.transformFiles.config"
                    |> GlobbingPattern.setBaseDir (dir)
                Xdt.transformFilesWithConfigName "test" files
                let actual = TestData.read confFile
                Expect.equal actual expected "Expected the transformed file to match"
            )
        }
    ]
