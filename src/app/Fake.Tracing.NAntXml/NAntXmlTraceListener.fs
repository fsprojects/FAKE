/// This module contains function which allow to trace build output
module Fake.Tracing.NAntXml
open Fake.Core
open Fake.IO

open System
open System.IO
open System.Text

/// Specifies if the XmlWriter should close tags automatically
let mutable AutoCloseXmlWriter = false

/// Implements a TraceListener which writes NAnt like XML files.
/// ## Parameters
///  - `xmlOutputFile` - Defines the xml output file.
type NAntXmlTraceListener(encoding : Encoding, xmlOutputFile) =
    
    let getXmlWriter() =
        let file = File.Open(xmlOutputFile, FileMode.Append)
        new IO.StreamWriter(file, encoding)
    let mutable xmlWriter : IO.StreamWriter = null
    
    let deleteOldFile() = 
        let fi = FileInfo.ofPath xmlOutputFile
        if fi.Exists then 
            fi.IsReadOnly <- false
            fi.Delete()
        if not fi.Directory.Exists then fi.Directory.Create()
    
    let closeWriter() = 
        if not (isNull xmlWriter) then 
            xmlWriter.Dispose()
            xmlWriter <- null
    
    let writeXml (xml:string) =
        if isNull xmlWriter then xmlWriter <- getXmlWriter()
        xml
        |> xmlWriter.WriteLine
        xmlWriter.Flush()
        if AutoCloseXmlWriter then closeWriter()
    
    do 
        deleteOldFile()
        writeXml "<?xml version=\"1.0\"?>\r\n<buildresults>"

    let getXml msg = 
        match msg with
        | TraceData.ImportantMessage text -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text // TODO: Set Level
        | TraceData.LogMessage(text, _) | TraceData.TraceMessage(text, _) -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text
        | TraceData.OpenTag(tag, _) -> sprintf "<%s name=\"%s\">" tag.Type tag.Name
        | TraceData.CloseTag (tag, _, _) -> sprintf "</%s>" tag.Type
        | TraceData.ErrorMessage text -> 
            sprintf "<failure><builderror><message level=\"Error\"><![CDATA[%s]]></message></builderror></failure>" text
        | TraceData.TestOutput _
        | TraceData.TestStatus _
        | TraceData.ImportData _
        | TraceData.BuildNumber _
        | TraceData.BuildState _ -> ""

    interface System.IDisposable with
        member __.Dispose () =
            writeXml "</buildresults>"
            closeWriter()

    // All Fake variables get disposed at the end of the build script.
    // As this listeners will be added to the "Fake.Core.Trace.TraceListeners" variable
    // and we enumerate all collections it should work fine.
    interface ITraceListener with
        /// Writes the given message to the xml file.
        member __.Write msg =
            let xml = getXml msg
            if not (String.IsNullOrWhiteSpace xml) then
                 writeXml xml

/// Allows to register a new Xml listeners
let addXmlListener encoding xmlOutputFile = CoreTracing.addListener (new NAntXmlTraceListener(encoding, xmlOutputFile))
