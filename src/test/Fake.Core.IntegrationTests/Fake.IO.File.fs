module Fake.IO.FileIntegrationTests

open Fake.IO
open System.IO
open Expecto

[<Tests>]
let tests =
    testList "Fake.IO.FileIntegraionTests" [
        testCase "Files created using File.create can be used immediately - #2183" <| fun _ ->
            let testFile = Path.combine (Path.GetTempPath ()) (Path.GetRandomFileName ())

            File.create testFile
            File.replaceContent testFile "Test"

            let actualContent = File.readAsString testFile

            Expect.equal actualContent "Test" "Unexpected content in test file"
    ]
