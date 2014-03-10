[<AutoOpen>]
/// Contains code to configure FAKE for AppVeyor integration
module Fake.AppVeyor

open Fake.MSBuildHelper

let private sendToAppVeyor args =
    ExecProcess (fun info -> 
                info.FileName <- "appveyor"
                info.Arguments <- args) (System.TimeSpan.MaxValue)
    |> ignore

let private add msg category =
    sprintf "AddMessage %s -Category %s" (quoteIfNeeded msg) (quoteIfNeeded category)
    |> sendToAppVeyor

let private addNoCategory msg =
    sprintf "AddMessage %s" (quoteIfNeeded msg)
    |> sendToAppVeyor

// Add trace listener to track messages
if buildServer = BuildServer.AppVeyor then
    listeners.Add({new ITraceListener with
        member this.Write msg =
            match msg with
            | ErrorMessage x -> add x "Error"
            | ImportantMessage x -> add x "Warning"
            | LogMessage (x, _) -> add x "Information"
            | TraceMessage (x, _) -> if not enableProcessTracing then addNoCategory x
            | StartMessage | FinishedMessage
            | OpenTag (_, _) | CloseTag _ -> ()})

// Add MSBuildLogger to track build messages
if buildServer = BuildServer.AppVeyor then
    MSBuildLoggers <- @"""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll""" :: MSBuildLoggers
