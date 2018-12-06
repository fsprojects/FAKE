/// Analyse a binlog and emit proper CI messages
module Fake.DotNet.MSBuildBinLog

open System
open Microsoft.Build.Framework
open Microsoft.Build.Logging.StructuredLogger
open System.Collections.Generic
open Fake.Core

let internal structuredLogAssemblyPath =
    typeof<BinLogReader>.Assembly.Location

let internal emitMessages (msgs:ConsoleMessage list) =
    let knownErrors = HashSet<string>()
    let traceError msg =
        if knownErrors.Add(msg) then
            Trace.traceError msg
    let knownWarnings = HashSet<string>()
    let traceWarning msg =
        if knownWarnings.Add(msg) then
            Trace.traceFAKE "%s" msg
    for msg in msgs do
        if not msg.IsError then
            traceWarning msg.Message
    for msg in msgs do
        if msg.IsError then
            traceError msg.Message

let getErrorsAndWarnings (binLogFilePath:string) =
    let binLogReader = new BinLogReader()
    use stream = new System.IO.FileStream(binLogFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)
    binLogReader.ReadRecords(stream)
    |> Seq.choose (fun record ->    
        if isNull record then None else
        if isNull record.Args then None else
        let buildEventArgs = record.Args
        match buildEventArgs with
        | :? BuildErrorEventArgs as a ->
            let msg =
                sprintf "%s: %s(%d,%d): error %s: %s"
                    a.SenderName
                    a.File
                    a.LineNumber
                    a.ColumnNumber
                    a.Code
                    a.Message
            { IsError = true
              Message = msg
              Timestamp = DateTimeOffset a.Timestamp } |> Some
        | :? BuildWarningEventArgs as a ->
            let msg = 
                sprintf "%s: %s(%d,%d): warning %s: %s"
                    a.SenderName
                    a.File
                    a.LineNumber
                    a.ColumnNumber
                    a.Code
                    a.Message
            { IsError = false
              Message = msg
              Timestamp = DateTimeOffset a.Timestamp } |> Some
        | _ -> None)
    |> Seq.toList   
