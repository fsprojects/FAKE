module Fake.Testing.ArgumentHelper

open Fake.Core
open Expecto

let checkIfMono (file, args) =
    match Environment.isWindows, Process.monoPath with
    | false, Some s when file = s ->
      match args.Args with
      | a when a.Length = 2 ->
          Expect.equal a.[0] "--debug" "Expected first flag to be '--debug'"
          a.[1], Arguments.OfArgs []
      | a when a.Length > 2 ->
          Expect.equal a.[0] "--debug" "Expected first flag to be '--debug'"
          a.[1], Arguments.OfArgs a.[2..]
      | a ->
        Expect.isGreaterThanOrEqual a.Length 2 "Expected mono arguments"
        file, args

    | true, _ -> file, args
    | _ ->
      Trace.traceFAKE "Mono was not found in test!"
      file, args