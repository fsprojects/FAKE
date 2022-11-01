module Fake.DotNet.PaketTests

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Testing
open Expecto

[<Tests>]
let tests =
    testList
        "Fake.DotNet.Paket.Tests"
        [ testCase "Test restore is not missing, #2411"
          <| fun _ ->
              let cp = Paket.createProcess (Paket.StartType.Restore(Paket.PaketRestoreDefaults()))

              let file, args =
                  match cp.Command with
                  | RawCommand (file, args) -> file, args
                  | _ -> failwithf "expected RawCommand"
                  |> ArgumentHelper.checkIfMono

              let cmd = args |> Arguments.toStartInfo
              Expect.stringContains (file.ToLower()) "paket" "Expected paket"
              Expect.equal cmd "restore" "expected restore argument"
          testCase "Test pack is not missing, #2411"
          <| fun _ ->
              let cp = Paket.createProcess (Paket.StartType.Pack(Paket.PaketPackDefaults()))

              let file, args =
                  match cp.Command with
                  | RawCommand (file, args) -> file, args
                  | _ -> failwithf "expected RawCommand"
                  |> ArgumentHelper.checkIfMono

              let cmd = args |> Arguments.toStartInfo
              Expect.stringContains (file.ToLower()) "paket" "Expected paket"
              Expect.equal cmd "pack ./temp" "expected pack command line"
          testCase "Test push is not missing, #2411"
          <| fun _ ->
              let cp =
                  Paket.createProcess (Paket.StartType.PushFile(Paket.PaketPushDefaults(), "testfile"))

              let file, args =
                  match cp.Command with
                  | RawCommand (file, args) -> file, args
                  | _ -> failwithf "expected RawCommand"
                  |> ArgumentHelper.checkIfMono

              let cmd = args |> Arguments.toStartInfo
              Expect.stringContains (file.ToLower()) "paket" "Expected paket"
              Expect.equal cmd "push testfile" "expected push command line" ]
