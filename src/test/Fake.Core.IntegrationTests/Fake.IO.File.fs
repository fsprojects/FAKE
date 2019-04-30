module Fake.IO.FileIntegrationTests

open Fake.IO
open System.IO
open Expecto

type TestDir =
    { Dir : string }
    interface System.IDisposable with
        member x.Dispose() =
            try
                Directory.Delete(x.Dir, true)
            with e ->
                eprintf "Failed to delete '%s': %O" x.Dir e
                ()

let createTestDir () =
    let testFile = Path.combine (Path.GetTempPath ()) (Path.GetRandomFileName ())
    Directory.CreateDirectory(testFile)
        |> ignore<DirectoryInfo>
    { Dir = testFile }

[<Tests>]
let tests =
    testList "Fake.IO.FileSystemIntegraionTests" [
        testCase "Files created using File.create can be used immediately - #2183" <| fun _ ->
            let testFile = Path.combine (Path.GetTempPath ()) (Path.GetRandomFileName ())

            File.create testFile
            File.replaceContent testFile "Test"

            let actualContent = File.readAsString testFile

            Expect.equal actualContent "Test" "Unexpected content in test file"
        testCase "Shell.mv works as expected when dst doesnt exist - #2293" <| fun _ ->
            use d = createTestDir()
            let f1 = Path.Combine(d.Dir, "1")
            let f2 = Path.Combine(d.Dir, "2")
            File.WriteAllText(f1, "test")

            Shell.mv f1 f2
            Expect.isFalse (File.Exists f1) "f1 should no longer exists"
            Expect.isTrue (File.Exists f2) "f2 should exists"
            Expect.equal (File.ReadAllText(f2)) "test" "Unexpected content in test file"
        testCase "Shell.mv works as expected when dst is a file - #2293" <| fun _ ->
            use d = createTestDir()
            let f1 = Path.Combine(d.Dir, "1")
            let f2 = Path.Combine(d.Dir, "2")
            File.WriteAllText(f1, "test")
            File.WriteAllText(f2, "other")

            Shell.mv f1 f2
            Expect.isFalse (File.Exists f1) "f1 should no longer exists"
            Expect.isTrue (File.Exists f2) "f2 should exists"
            Expect.equal (File.ReadAllText(f2)) "test" "Unexpected content in test file"
        testCase "Shell.mv works as expected when dst is a dir - #2293" <| fun _ ->
            use d = createTestDir()
            let f1 = Path.Combine(d.Dir, "1")
            let d2 = Path.Combine(d.Dir, "2")
            Directory.CreateDirectory(d2) |> ignore<DirectoryInfo>
            File.WriteAllText(f1, "test")

            Shell.mv f1 d2
            let f2 = Path.Combine(d2, "1")
            Expect.isFalse (File.Exists f1) "f1 should no longer exists"
            Expect.isTrue (File.Exists f2) "f2 should exists"
            Expect.equal (File.ReadAllText(f2)) "test" "Unexpected content in test file"

    ]
