module Fake.Testing.ArgumentHelper

open Fake.Core
open Expecto

let checkIfMono (file, args) =
    match Environment.isWindows, Process.monoPath with
    | false, Some s when file = s ->
      match args |> Arguments.toList with
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

let checkIfDotNet (file:string, args) =
    let lower = file.ToLowerInvariant()
    if not (lower.EndsWith "dotnet.exe") && not (lower.EndsWith "dotnet") then
      Expect.isTrue false (sprintf "Expected dotnet.exe but got %s" file)
    
    match args |> Arguments.toList with
    | firstArg :: rest ->
        file, firstArg, Arguments.OfArgs rest
    | a ->
      Expect.isGreaterThanOrEqual a.Length 1 "Expected dotnet arguments"
      "dotnet", file, args
  