module Fake.Testing.ArgumentHelper

open Fake.Core
open Expecto

let checkIfMono (file, args) =
    match Environment.isWindows, Process.monoPath with
    | false, Some s when file = s ->
      Expect.isGreaterThanOrEqual args.Args.Length 3 "Expected mono arguments"
      Expect.equal args.Args.[0] "--debug" "Expected first flag to be '--debug'"
      args.Args.[1], Arguments.OfArgs args.Args.[2..]
    | true, _ -> file, args
    | _ ->
      Trace.traceFAKE "Mono was not found in test!"
      file, args