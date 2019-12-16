module Fake.DotNet.PaketIntegrationTests

open Fake.IO
open Fake.Core
open Fake.Core.IntegrationTests.TestHelpers
open Fake.DotNet
open System.IO
open Expecto

[<Tests>]
let tests =
    testList "Fake.DotNet.PaketIntegrationTests" [
        testCase "findPaketExecutable works for global tools - #2361" <| fun _ ->
            use dir = createTestDir()
            let folder = dir.Dir
            let paketDir = Path.Combine(folder, ".paket")
            Directory.ensure paketDir
            let paketExe = Path.Combine(paketDir, if Environment.isWindows then "paket.exe" else "paket")
            File.WriteAllText(paketExe, "test")
            let store = Path.Combine(folder, ".paket", "store", "paket")
            Directory.ensure store
            let testFile = Path.Combine(folder, ".paket", "store", "paket", "test")
            File.WriteAllText(testFile, "test")


            let result = Fake.DotNet.Paket.findPaketExecutable dir.Dir
            let expected = Path.combine folder (if Environment.isWindows then ".paket/paket.EXE" else ".paket/paket") |> Path.getFullName
            Expect.equal result expected "Expected proper file path"
    ]
