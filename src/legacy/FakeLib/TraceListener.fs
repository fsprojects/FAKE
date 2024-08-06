﻿[<AutoOpen>]
/// Defines default listeners for build output traces
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
module Fake.TraceListener

open System

/// Defines Tracing information for TraceListeners
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
type TraceData =
    | StartMessage
    | ImportantMessage of string
    | ErrorMessage of string
    | LogMessage of string * bool
    | TraceMessage of string * bool
    | FinishedMessage
    | OpenTag of string * string
    | CloseTag of string

    member x.NewLine =
        match x with
        | ImportantMessage _
        | ErrorMessage _ -> Some true
        | LogMessage(_, newLine)
        | TraceMessage(_, newLine) -> Some newLine
        | StartMessage
        | FinishedMessage
        | OpenTag _
        | CloseTag _ -> None

    member x.Message =
        match x with
        | ImportantMessage text
        | ErrorMessage text
        | LogMessage(text, _)
        | TraceMessage(text, _) -> Some text
        | StartMessage
        | FinishedMessage
        | OpenTag _
        | CloseTag _ -> None

/// Defines a TraceListener interface
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
type ITraceListener =
    abstract Write: TraceData -> unit

/// A default color map which maps TracePriorities to ConsoleColors
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
let colorMap traceData =
    match traceData with
    | ImportantMessage _ -> ConsoleColor.Yellow
    | ErrorMessage _ -> ConsoleColor.Red
    | LogMessage _ -> ConsoleColor.Gray
    | TraceMessage _ -> ConsoleColor.Green
    | FinishedMessage -> ConsoleColor.White
    | _ -> ConsoleColor.Gray

/// Implements a TraceListener for System.Console.
/// ## Parameters
///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
type ConsoleTraceListener(importantMessagesToStdErr, colorMap, useAnsiColorCodes) =
    let writeTextConsoleColor print color text =
        let curColor = Console.ForegroundColor

        try
            try
                if curColor <> color then
                    Console.ForegroundColor <- color

                print text
            with :? ArgumentException when EnvironmentHelper.isMono ->
                printfn "* Color output has been disabled because of a bug in GNOME Terminal."
                printfn "* Hint: To workaround this bug, please set environment property TERM=xterm-256color"
                reraise ()
        finally
            if curColor <> color then
                Console.ForegroundColor <- curColor

    let writeTextAnsiColor print color text =
        let colorCode =
            function
            | ConsoleColor.Black -> [ 30 ]
            | ConsoleColor.Blue -> [ 34 ]
            | ConsoleColor.Cyan -> [ 36 ]
            | ConsoleColor.Gray -> [ 37; 2 ]
            | ConsoleColor.Green -> [ 32 ]
            | ConsoleColor.Magenta -> [ 35 ]
            | ConsoleColor.Red -> [ 31 ]
            | ConsoleColor.White -> [ 37 ]
            | ConsoleColor.Yellow -> [ 33 ]
            | ConsoleColor.DarkBlue -> [ 34; 2 ]
            | ConsoleColor.DarkCyan -> [ 36; 2 ]
            | ConsoleColor.DarkGray -> [ 37; 2 ]
            | ConsoleColor.DarkGreen -> [ 32; 2 ]
            | ConsoleColor.DarkMagenta -> [ 35; 2 ]
            | ConsoleColor.DarkRed -> [ 31; 2 ]
            | ConsoleColor.DarkYellow -> [ 33; 2 ]
            | _ -> [ 39 ]

        let codeStr = colorCode color |> List.map (sprintf "%i") |> String.concat ";"

        print <| sprintf "\x1b[%sm%s\x1b[0m" codeStr text

    let printer toStdErr newLine =
        match toStdErr, newLine with
        | true, true -> eprintfn "%s"
        | true, false -> eprintf "%s"
        | false, true -> printfn "%s"
        | false, false -> printf "%s"

    interface ITraceListener with
        /// Writes the given message to the Console.
        member this.Write msg =
            let color = colorMap msg

            let writeText text printer =
                if useAnsiColorCodes then
                    writeTextAnsiColor printer color text
                else
                    writeTextConsoleColor printer color text

            match msg with
            | StartMessage -> ()
            | OpenTag _ -> ()
            | CloseTag _ -> ()
            | ImportantMessage text
            | ErrorMessage text -> printer importantMessagesToStdErr true |> writeText text
            | LogMessage(text, newLine)
            | TraceMessage(text, newLine) -> printer false newLine |> writeText text
            | FinishedMessage -> ()

// If we write the stderr on those build servers the build will fail.
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
let importantMessagesToStdErr =
    buildServer <> CCNet
    && buildServer <> AppVeyor
    && buildServer <> TeamCity
    && buildServer <> TeamFoundation

// stdout is piped to the logger, so colours are lost. AppVeyor supports ANSI color codes.
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
let useAnsiColors = buildServer = AppVeyor

/// The default TraceListener for Console.
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
let defaultConsoleTraceListener =
    ConsoleTraceListener(importantMessagesToStdErr, colorMap, useAnsiColors)

/// Specifies if the XmlWriter should close tags automatically
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
let mutable AutoCloseXmlWriter = false

/// Implements a TraceListener which writes NAnt like XML files.
/// ## Parameters
///  - `xmlOutputFile` - Defines the xml output file.
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
type NAntXmlTraceListener(xmlOutputFile) =
    let xmlOutputPath = FullName xmlOutputFile

    let getXmlWriter () =
        new IO.StreamWriter(xmlOutputPath, true, encoding)

    let mutable xmlWriter: IO.StreamWriter = null

    let deleteOldFile () =
        let fi = fileInfo xmlOutputPath

        if fi.Exists then
            fi.IsReadOnly <- false
            fi.Delete()

        if not fi.Directory.Exists then
            fi.Directory.Create()

    let closeWriter () =
        if xmlWriter <> null then
            xmlWriter.Close()
            xmlWriter <- null

    let getXml msg =
        match msg with
        | StartMessage -> "<?xml version=\"1.0\"?>\r\n<buildresults>"
        | ImportantMessage text -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text // TODO: Set Level
        | LogMessage(text, _)
        | TraceMessage(text, _) -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text
        | FinishedMessage -> "</buildresults>"
        | OpenTag(tag, name) -> sprintf "<%s name=\"%s\">" tag name
        | CloseTag tag -> sprintf "</%s>" tag
        | ErrorMessage text ->
            sprintf "<failure><builderror><message level=\"Error\"><![CDATA[%s]]></message></builderror></failure>" text

    interface ITraceListener with
        /// Writes the given message to the xml file.
        member this.Write msg =
            if msg = StartMessage then
                deleteOldFile ()

            if xmlWriter = null then
                xmlWriter <- getXmlWriter ()

            msg |> getXml |> xmlWriter.WriteLine
            xmlWriter.Flush()

            if AutoCloseXmlWriter || msg = FinishedMessage then
                closeWriter ()

/// A List with all registered listeners
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
let listeners = new Collections.Generic.List<ITraceListener>()

/// Allows to register a new Xml listeners
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
let addXmlListener xmlOutputFile =
    listeners.Add(new NAntXmlTraceListener(xmlOutputFile))

// register listeners
listeners.Add defaultConsoleTraceListener

if hasBuildParam "logfile" || buildServer = CCNet then
    addXmlListener xmlOutputFile

/// Allows to post messages to all trace listeners
[<System.Obsolete("please use nuget 'Fake.Core.Trace' and open Fake.Core instead")>]
let postMessage x =
    listeners.ForEach(fun listener -> listener.Write x)
