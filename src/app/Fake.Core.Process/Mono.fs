module Fake.Core.Mono

open Fake.Core
open System
open Fake.Core.Process

let monoPath, monoVersion =
    match Process.tryFindTool "MONO" "mono" with
    | Some path ->
        let success, messages =
            try Process.ExecProcessRedirected (fun proc ->
                proc.FileName <- path
                proc.Arguments <- "--version") (TimeSpan.FromMinutes 1.)
            with e ->
                false,
                [{ ConsoleMessage.IsError = true; ConsoleMessage.Message = e.ToString(); ConsoleMessage.Timestamp = DateTimeOffset.Now }]
                |> List.toSeq
        let out =
            let outStr = String.Join("\n", messages |> Seq.map (fun m -> m.Message))
            sprintf "Success: %b, Out: %s" success outStr
        let ver =
            match success, messages |> Seq.tryHead with
            | true, Some firstLine ->
                Some (out, Environment.Internal.parseMonoDisplayName firstLine.Message)
            | _ ->
                Some (out, None)
        Some path, ver
    | None -> None, None
