/// This module contains function which allow to trace build output
module Fake.Tracing.NAntXml
open Fake.Core
open Fake.IO.FileSystem
open Fake.Core.BuildServer

open System
open System.IO
open System.Reflection
open System.Threading
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
    
    let getXml msg = 
        match msg with
        | StartMessage -> "<?xml version=\"1.0\"?>\r\n<buildresults>"
        | ImportantMessage text -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text // TODO: Set Level
        | LogMessage(text, _) | TraceMessage(text, _) -> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" text
        | FinishedMessage -> "</buildresults>"
        | OpenTag(tag, description) -> sprintf "<%s name=\"%s\">" tag.Type tag.Name
        | CloseTag tag -> sprintf "</%s>" tag.Type
        | ErrorMessage text -> 
            sprintf "<failure><builderror><message level=\"Error\"><![CDATA[%s]]></message></builderror></failure>" text
    
    interface ITraceListener with
        /// Writes the given message to the xml file.
        member this.Write msg = 
            if msg = StartMessage then deleteOldFile()
            if isNull xmlWriter then xmlWriter <- getXmlWriter()
            msg
            |> getXml
            |> xmlWriter.WriteLine
            xmlWriter.Flush()
            if AutoCloseXmlWriter || msg = FinishedMessage then closeWriter()

/// Allows to register a new Xml listeners
let addXmlListener encoding xmlOutputFile = CoreTracing.addListener (new NAntXmlTraceListener(encoding, xmlOutputFile))
