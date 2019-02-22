module Fake.DotNet.FsiTests

open Fake.DotNet
open Expecto

[<Tests>]
let tests =
  let isDefine (s : string) = s.StartsWith "-d:"    

  testList "Fake.DotNet.Fsi.Tests" [
    testCase "Test that default params have no defines" <| fun _ ->
      let cmdList = Fsi.FsiParams.Create() |> Fsi.FsiParams.ToArgsList
      Expect.isFalse (cmdList |> List.exists isDefine) "FsiParams.Create() |> FsiParams.ToArgsList should not specify -d"

    testCase "Test that Define alone adds one -d flag" <| fun _ ->
      let cmdList = { Fsi.FsiParams.Create() with Define = "DEBUG" } |> Fsi.FsiParams.ToArgsList
      Expect.contains cmdList "-d:DEBUG" "Define=\"DEBUG\" should create the -d:DEBUG parameter"
      Expect.hasCountOf cmdList 1u isDefine "Define should create only one -d parameter"

    testCase "Test that Definitions alone adds the -d flags" <| fun _ ->
      let cmdList = { Fsi.FsiParams.Create() with Definitions = ["DEBUG"; "GUBED"] } |> Fsi.FsiParams.ToArgsList
      Expect.contains cmdList "-d:DEBUG" "Definitions = [\"DEBUG\"; \"GUBED\"] should create the -d:DEBUG parameter"
      Expect.contains cmdList "-d:GUBED" "Definitions = [\"DEBUG\"; \"GUBED\"] should create the -d:GUBED parameter"
      Expect.hasCountOf cmdList 2u isDefine "Definitions should create both -d parameters"

    testCase "Test that Definitions can be used together with Define" <| fun _ ->
      let cmdList = { Fsi.FsiParams.Create() with Definitions = ["DEBUG"; "GUBED"]; Define="BEDUG" } |> Fsi.FsiParams.ToArgsList
      Expect.contains cmdList "-d:DEBUG" "Definitions = [\"DEBUG\"; \"GUBED\"] should create the -d:DEBUG parameter"
      Expect.contains cmdList "-d:GUBED" "Definitions = [\"DEBUG\"; \"GUBED\"] should create the -d:GUBED parameter"
      Expect.contains cmdList "-d:BEDUG" "Define=\"BEDUG\" should create the -d:BEDUG parameter"
      Expect.hasCountOf cmdList 3u isDefine "Define and Definitions should all create -d parameters"
    ]
