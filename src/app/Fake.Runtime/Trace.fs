﻿/// This module contains function which allow to trace build output
module Fake.Runtime.Trace

open Fake.Runtime.Environment

open System
open System.IO
open System.Reflection
open System.Threading

type VerboseLevel =
    | Silent // For scripting and pipelining
    | Normal // Regular use
    | Verbose // Verbose FAKE
    | VerbosePaket // Verbose FAKE & Paket

    member x.PrintPaket =
        match x with
        | VerbosePaket -> true
        | _ -> false

    member x.PrintVerbose =
        match x with
        | Verbose
        | VerbosePaket -> true
        | _ -> false

    member x.PrintNormal =
        match x with
        | Normal
        | Verbose
        | VerbosePaket -> true
        | _ -> false

/// Defines Tracing information for TraceListeners
type TraceData =
    | ImportantMessage of string
    | ErrorMessage of string
    | LogMessage of string * bool
    | TraceMessage of string * bool

    member x.NewLine =
        match x with
        | ImportantMessage _
        | ErrorMessage _ -> Some true
        | LogMessage(_, newLine)
        | TraceMessage(_, newLine) -> Some newLine

    member x.Message =
        match x with
        | ImportantMessage text
        | ErrorMessage text
        | LogMessage(text, _)
        | TraceMessage(text, _) -> Some text

/// Defines a TraceListener interface
type ITraceListener =
    abstract Write: TraceData -> unit

/// A default color map which maps TracePriorities to ConsoleColors
let colorMap traceData =
    match traceData with
    | ImportantMessage _ -> ConsoleColor.Yellow
    | ErrorMessage _ -> ConsoleColor.Red
    | LogMessage _ -> ConsoleColor.Gray
    | TraceMessage _ -> ConsoleColor.Green

/// <summary>
/// Implements a TraceListener for System.Console.
/// </summary>
///
/// <param name="importantMessagesToStdErr">Defines whether to trace important messages to StdErr.</param>
/// <param name="colorMap">A function which maps TracePriorities to ConsoleColors.</param>
type ConsoleTraceListener(colorMap) =
    let writeText stdErr color newLine text =
        let curColor = Console.ForegroundColor

        try
            if curColor <> color then
                Console.ForegroundColor <- color

            let printer =
                match stdErr, newLine with
                | false, true -> printfn
                | false, false -> printf
                | true, true -> eprintfn
                | true, false -> eprintf

            printer "%s" text
        finally
            if curColor <> color then
                Console.ForegroundColor <- curColor

    interface ITraceListener with
        /// Writes the given message to the Console.
        member this.Write msg =
            let color = colorMap msg

            match msg with
            | ImportantMessage text
            | ErrorMessage text -> writeText true color true text
            | LogMessage(text, newLine)
            | TraceMessage(text, newLine) -> writeText false color newLine text

/// The default TraceListener for Console.
let defaultConsoleTraceListener = ConsoleTraceListener(colorMap) :> ITraceListener

/// A List with all registered listeners
let listeners = new Collections.Generic.List<ITraceListener>()

// register listeners
listeners.Add defaultConsoleTraceListener

/// Allows to post messages to all trace listeners
let postMessage x =
    listeners.ForEach(fun listener -> listener.Write x)

type FAKEException(msg) =
    inherit System.Exception(msg)

/// Gets the path of the current FAKE instance
#if !CORE_CLR
let fakePath = typeof<FAKEException>.Assembly.Location
#else
let fakePath = typeof<FAKEException>.GetTypeInfo().Assembly.Location
#endif


/// Logs the specified string
let log message =
    LogMessage(message, true) |> postMessage

/// Writes a trace to the command line (in green)
let trace message =
    postMessage (TraceMessage(message, true))

/// Writes a message to the command line (in green)
let tracefn fmt = Printf.ksprintf trace fmt

/// Writes a message to the command line (in green) and without a line break
let tracef fmt =
    Printf.ksprintf (fun text -> postMessage (TraceMessage(text, false))) fmt

/// Writes a trace to the command line (in yellow)
let traceFAKE fmt =
    Printf.ksprintf (fun text -> postMessage (ImportantMessage text)) fmt

/// Traces an error (in red)
let traceError error = postMessage (ErrorMessage error)
