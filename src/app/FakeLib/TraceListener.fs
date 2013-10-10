[<AutoOpen>]
module Fake.TraceListener

open System

/// Defines Tracing information for TraceListeners
type TraceData =
    | StartMessage
    | ImportantMessage of string
    | ErrorMessage  of string
    | LogMessage of string * bool
    | TraceMessage of string * bool
    | FinishedMessage 
    | OpenTag of string * string
    | CloseTag of string

/// Defines a TraceListener interface
type ITraceListener =
    abstract Write: TraceData -> unit

/// Maps TracePriorities to ConsoleColors
let colorMap = function
    | ImportantMessage _ -> ConsoleColor.Yellow
    | ErrorMessage _     -> ConsoleColor.Red
    | LogMessage _       -> ConsoleColor.Gray
    | TraceMessage _     -> ConsoleColor.Green
    | FinishedMessage    -> ConsoleColor.White
    | _                  -> ConsoleColor.Gray

/// <summary>Implements a TraceListener for System.Console</summary>
/// <param name="importantMessagesToStdErr">Defines whether to trace important messages to StdErr.</param>
/// <param name="colorMap">A function which maps TracePriorities to ConsoleColors.</param>
type ConsoleTraceListener(importantMessagesToStdErr,colorMap) =
    let writeText toStdErr color newLine text =
        let text = if not verbose then shortenCurrentDirectory text else text
        let curColor = Console.ForegroundColor
        if curColor <> color then Console.ForegroundColor <- color
        if toStdErr then 
            if newLine then eprintfn "%s" text else eprintf "%s" text 
        else 
            if newLine then printfn "%s" text else printf "%s" text
        if curColor <> color then Console.ForegroundColor <- curColor

    interface ITraceListener with
        /// Writes the given message to the Console.
        member this.Write msg =
            let color = colorMap msg
            match msg with
            | StartMessage -> ()
            | OpenTag _ -> ()
            | CloseTag _ -> ()                        
            | ImportantMessage text 
            | ErrorMessage text  -> writeText importantMessagesToStdErr color true text
            | LogMessage(text,newLine)
            | TraceMessage(text,newLine) -> writeText false color newLine text
            | FinishedMessage -> ()
            

/// The default TraceListener for Console
let defaultConsoleTraceListener = ConsoleTraceListener(buildServer <> CCNet,colorMap)

let mutable AutoCloseXmlWriter = false

/// <summary>Implements a TraceListener which writes NAnt like XML files.</summary>
/// <param name="xmlOutputFile">Defines the xml output file.</param>
type NAntXmlTraceListener(xmlOutputFile) =
    let getXmlWriter() = new IO.StreamWriter(xmlOutputFile,true,encoding)
    let mutable xmlWriter:IO.StreamWriter = null

    let deleteOldFile() = 
        let fi = fileInfo xmlOutputFile
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
        | LogMessage(text,_)
        | TraceMessage(text,_) -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text
        | FinishedMessage -> "</buildresults>"
        | OpenTag(tag,name) -> sprintf "<%s name=\"%s\">" tag name
        | CloseTag tag -> sprintf "</%s>" tag
        | ErrorMessage text -> sprintf "<failure><builderror><message level=\"Error\"><![CDATA[%s]]></message></builderror></failure>" text

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

let listeners = new Collections.Generic.List<ITraceListener>()
let addXmlListener xmlOutputFile = listeners.Add(new NAntXmlTraceListener(xmlOutputFile))

// register listeners
listeners.Add defaultConsoleTraceListener
if hasBuildParam "logfile" || buildServer = CCNet then addXmlListener xmlOutputFile

let postMessage x = listeners.ForEach (fun listener -> listener.Write x)