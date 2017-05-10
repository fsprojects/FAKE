/// Defines default listeners for build output traces
namespace Fake.Core

open Fake.Core.BuildServer

open System

type KnownTags =
    | Task of name:string
    | Target of name:string
    | Other of typeDef:string * name:string
    member x.Name =
        match x with
        | Task n
        | Target n
        | Other (_, n) -> n
    member x.Type =
        match x with
        | Task _ -> "task"
        | Target _ -> "target"
        | Other (t, _) -> t

/// Defines Tracing information for TraceListeners
type TraceData = 
    | StartMessage
    | ImportantMessage of string
    | ErrorMessage of string
    | LogMessage of string * bool
    | TraceMessage of string * bool
    | FinishedMessage
    | OpenTag of KnownTags * description:string
    | CloseTag of KnownTags
    member x.NewLine =
        match x with
        | ImportantMessage _
        | ErrorMessage _ -> Some true
        | LogMessage (_, newLine)
        | TraceMessage (_, newLine) -> Some newLine
        | StartMessage
        | FinishedMessage
        | OpenTag _
        | CloseTag _ -> None
    member x.Message =
        match x with
        | ImportantMessage text
        | ErrorMessage text
        | LogMessage (text, _)
        | TraceMessage (text, _) -> Some text
        | StartMessage
        | FinishedMessage
        | OpenTag _
        | CloseTag _ -> None

/// Defines a TraceListener interface
type ITraceListener = 
    abstract Write : TraceData -> unit

/// Implements a TraceListener for System.Console.
/// ## Parameters
///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
type ConsoleTraceListener(importantMessagesToStdErr, colorMap) = 
    
    let writeText toStdErr color newLine text = 
        let curColor = Console.ForegroundColor
        try
          if curColor <> color then Console.ForegroundColor <- color
          let printer =
            match toStdErr, newLine with
            | true, true -> eprintfn
            | true, false -> eprintf
            | false, true -> printfn
            | false, false -> printf
          printer "%s" text
        finally
          if curColor <> color then Console.ForegroundColor <- curColor
    
    interface ITraceListener with
        /// Writes the given message to the Console.
        member this.Write msg = 
            let color = colorMap msg
            match msg with
            | StartMessage -> ()
            | OpenTag _ -> ()
            | CloseTag _ -> ()
            | ImportantMessage text | ErrorMessage text ->
                writeText importantMessagesToStdErr color true text
            | LogMessage(text, newLine) | TraceMessage(text, newLine) ->
                writeText false color newLine text
            | FinishedMessage -> ()

module CoreTracing =
    /// A default color map which maps TracePriorities to ConsoleColors
    let colorMap traceData = 
        match traceData with
        | ImportantMessage _ -> ConsoleColor.Yellow
        | ErrorMessage _ -> ConsoleColor.Red
        | LogMessage _ -> ConsoleColor.Gray
        | TraceMessage _ -> ConsoleColor.Green
        | FinishedMessage -> ConsoleColor.White
        | _ -> ConsoleColor.Gray
    // If we write the stderr on those build servers the build will fail.
    let importantMessagesToStdErr = buildServer <> CCNet && buildServer <> AppVeyor && buildServer <> TeamCity

    /// The default TraceListener for Console.
    let defaultConsoleTraceListener =
      ConsoleTraceListener(importantMessagesToStdErr, colorMap)


    /// A List with all registered listeners
    let private listeners = new Collections.Generic.List<ITraceListener>()
    let addListener l = listeners.Add l

    // register listeners
    listeners.Add defaultConsoleTraceListener
    
    /// Allows to post messages to all trace listeners
    let postMessage x = listeners.ForEach(fun listener -> listener.Write x)
