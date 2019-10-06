module Fake.IO.FileIntegrationTests

open Fake.IO
open System.IO
open Expecto
open Fake.Core.IntegrationTests.TestHelpers
open Fake.Core

[<Tests>]
let tests =
    testList "Fake.IO.FileIntegrationTests" [
        testCase "File.getVersion throws InvalidOperationException #2378" <| fun _ ->
          if Environment.isWindows then // On non-windows the API returns managed assembly info, see https://github.com/dotnet/corefx/blob/5fb98a118bb19a91e8ffb5c17ff5e7c00a4c05ee/src/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L20-L28
            let testFile = getTestFile "NoVersionTestFile.dll"
            Expect.throwsT<System.InvalidOperationException> (fun () ->
                    File.getVersion testFile
                        |> ignore<string>
                ) "Expected InvalidOperationException for missing file version"
        testCase "File.tryGetVersion works when component is missing #2378" <| fun _ ->
          if Environment.isWindows then // On non-windows the API returns managed assembly info, see https://github.com/dotnet/corefx/blob/5fb98a118bb19a91e8ffb5c17ff5e7c00a4c05ee/src/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L20-L28
            let testFile = getTestFile "NoVersionTestFile.dll"
            Expect.equal (File.tryGetVersion testFile) None "Expected None for missing file version"

        testCase "Files created using File.create can be used immediately - #2183" <| fun _ ->
            let testFile = Path.combine (Path.GetTempPath ()) (Path.GetRandomFileName ())

            File.create testFile
            File.replaceContent testFile "Test"

            let actualContent = File.readAsString testFile

            Expect.equal actualContent "Test" "Unexpected content in test file"
        testCase "Shell.mv works as expected when src is a file and dst doesnt exist - #2293" <| fun _ ->
            use d = createTestDir()
            let f1 = Path.Combine(d.Dir, "1")
            let f2 = Path.Combine(d.Dir, "2")
            File.WriteAllText(f1, "test")

            Shell.mv f1 f2
            Expect.isFalse (File.Exists f1) "f1 should no longer exists"
            Expect.isTrue (File.Exists f2) "f2 should exist"
            Expect.equal (File.ReadAllText(f2)) "test" "Unexpected content in test file"
        testCase "Shell.mv works as expected when src is a file and dst is a file - #2293" <| fun _ ->
            use d = createTestDir()
            let f1 = Path.Combine(d.Dir, "1")
            let f2 = Path.Combine(d.Dir, "2")
            File.WriteAllText(f1, "test")
            File.WriteAllText(f2, "other")

            Shell.mv f1 f2
            Expect.isFalse (File.Exists f1) "f1 should no longer exists"
            Expect.isTrue (File.Exists f2) "f2 should exist"
            Expect.equal (File.ReadAllText(f2)) "test" "Unexpected content in test file"
        testCase "Shell.mv works as expected when src is a file dst is a dir - #2293" <| fun _ ->
            use d = createTestDir()
            let f1 = Path.Combine(d.Dir, "1")
            let d2 = Path.Combine(d.Dir, "2")
            Directory.CreateDirectory(d2) |> ignore<DirectoryInfo>
            File.WriteAllText(f1, "test")

            Shell.mv f1 d2
            let f2 = Path.Combine(d2, "1")
            Expect.isFalse (File.Exists f1) "f1 should no longer exists"
            Expect.isTrue (File.Exists f2) "f2 should exist"
            Expect.equal (File.ReadAllText(f2)) "test" "Unexpected content in test file"
        testCase "Shell.mv works as expected when src is a dir and dst doesnt exist - #2293" <| fun _ ->
            use d = createTestDir()
            let d1 = Path.Combine(d.Dir, "1")
            let d2 = Path.Combine(d.Dir, "2")
            let f1 = Path.Combine(d1, "1")
            let f2 = Path.Combine(d2, "1")
            Directory.CreateDirectory(d1) |> ignore<DirectoryInfo>
            File.WriteAllText(f1, "test")

            Shell.mv d1 d2
            Expect.isFalse (Directory.Exists d1) "d1 should no longer exists"
            Expect.isTrue (Directory.Exists d2) "d2 should exist"
            Expect.isFalse (File.Exists f1) "f1 should no longer exists"
            Expect.isTrue (File.Exists f2) "f2 should exist"
            Expect.equal (File.ReadAllText(f2)) "test" "Unexpected content in test file"
        testCase "Shell.mv throws as expected when src is a dir and dst is a file - #2293" <| fun _ ->
            use d = createTestDir()
            let d1 = Path.Combine(d.Dir, "1")
            let f2 = Path.Combine(d.Dir, "2")
            let f1 = Path.Combine(d1, "1")
            Directory.CreateDirectory(d1) |> ignore<DirectoryInfo>
            File.WriteAllText(f1, "test")
            File.WriteAllText(f2, "other")

            Expect.throws (fun () -> Shell.mv d1 f2) "moving dir to file should throw"
        testCase "Shell.mv works as expected when src is a dir dst is a dir - #2293" <| fun _ ->
            use d = createTestDir()
            let d1 = Path.Combine(d.Dir, "1")
            let d2 = Path.Combine(d.Dir, "2")
            let f1 = Path.Combine(d1, "1")
            let f2 = Path.Combine(Path.Combine(d2, "1"), "1")
            Directory.CreateDirectory(d1) |> ignore<DirectoryInfo>
            Directory.CreateDirectory(d2) |> ignore<DirectoryInfo>
            File.WriteAllText(f1, "test")

            Shell.mv d1 d2
            Expect.isFalse (Directory.Exists d1) "d1 should no longer exists"
            Expect.isTrue (Directory.Exists d2) "d2 should exist"
            Expect.isFalse (File.Exists f1) "f1 should no longer exists"
            Expect.isTrue (File.Exists f2) "f2 should exist"
            Expect.equal (File.ReadAllText(f2)) "test" "Unexpected content in test file"
    ]
