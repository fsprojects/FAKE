module Fake.Testing.ArgumentHelper

open Fake.Core
open Expecto

let checkIfMono (file, args) =
    match Environment.isWindows, Process.monoPath with
    | false, Some s when file = s ->
      match args |> CommandLine.toList with
      | debugFlag :: file :: rest ->
          Expect.equal debugFlag "--debug" "Expected first flag to be '--debug'"
          file, Arguments.OfArgs rest
      | a ->
        Expect.isGreaterThanOrEqual a.Length 2 "Expected mono arguments"
        file, args

    | true, _ -> file, args
    | _ ->
      Trace.traceFAKE "Mono was not found in test!"
      file, args