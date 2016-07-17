[<AutoOpen>]
/// Defines default listeners for build output traces
module Fake.TraceListener

open System

/// Defines Tracing information for TraceListeners
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

/// A default color map which maps TracePriorities to ConsoleColors
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

// If we write the stderr on those build servers the build will fail.
let importantMessagesToStdErr = buildServer <> CCNet && buildServer <> AppVeyor && buildServer <> TeamCity

/// The default TraceListener for Console.
let defaultConsoleTraceListener =
  ConsoleTraceListener(importantMessagesToStdErr, colorMap)

/// Specifies if the XmlWriter should close tags automatically
let mutable AutoCloseXmlWriter = false

/// Implements a TraceListener which writes NAnt like XML files.
/// ## Parameters
///  - `xmlOutputFile` - Defines the xml output file.
type NAntXmlTraceListener(xmlOutputFile) = 
    let xmlOutputPath = FullName xmlOutputFile
    let getXmlWriter() = new IO.StreamWriter(xmlOutputPath, true, encoding)
    let mutable xmlWriter : IO.StreamWriter = null
    
    let deleteOldFile() = 
        let fi = fileInfo xmlOutputPath
        if fi.Exists then 
            fi.IsReadOnly <- false
            fi.Delete()
        if not fi.Directory.Exists then fi.Directory.Create()
    
    let closeWriter() = 
        if xmlWriter <> null then 
            xmlWriter.Close()
            xmlWriter <- null
    
    let getXml msg = 
        match msg with
        | StartMessage -> "<?xml version=\"1.0\"?>\r\n<buildresults>"
        | ImportantMessage text -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text // TODO: Set Level
        | LogMessage(text, _) | TraceMessage(text, _) -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text
        | FinishedMessage -> "</buildresults>"
        | OpenTag(tag, name) -> sprintf "<%s name=\"%s\">" tag name
        | CloseTag tag -> sprintf "</%s>" tag
        | ErrorMessage text -> 
            sprintf "<failure><builderror><message level=\"Error\"><![CDATA[%s]]></message></builderror></failure>" text
    
    interface ITraceListener with
        /// Writes the given message to the xml file.
        member this.Write msg = 
            if msg = StartMessage then deleteOldFile()
            if xmlWriter = null then xmlWriter <- getXmlWriter()
            msg
            |> getXml
            |> xmlWriter.WriteLine
            xmlWriter.Flush()
            if AutoCloseXmlWriter || msg = FinishedMessage then closeWriter()

/// A List with all registered listeners
let listeners = new Collections.Generic.List<ITraceListener>()

/// Allows to register a new Xml listeners
let addXmlListener xmlOutputFile = listeners.Add(new NAntXmlTraceListener(xmlOutputFile))

// register listeners
listeners.Add defaultConsoleTraceListener
if hasBuildParam "logfile" || buildServer = CCNet then addXmlListener xmlOutputFile

/// Allows to post messages to all trace listeners
let postMessage x = listeners.ForEach(fun listener -> listener.Write x)
