[<AutoOpen>]
module Fake.MailBoxHelper

/// Trace verbose output
let mutable verbose = hasBuildParam "verbose"

open System

type Message = 
    { Target    : TraceMode
      Text      : string
      Color     : ConsoleColor
      Newline   : bool
      Important : bool}

let defaultMessage = 
    { Target    = TraceMode.Console
      Text      = ""
      Color     = ConsoleColor.White
      Newline   = true
      Important = false }

let mutable private xmlWriter = null

let mutable AutoCloseXmlWriter = false

let private openWriter() = xmlWriter <- new IO.StreamWriter(xmlOutputFile,true,Text.Encoding.Default)
let private closeWriter() = 
    if xmlWriter <> null then
        xmlWriter.Close()
        xmlWriter <- null

let buffer = MailboxProcessor.Start (fun inbox ->

    let rec loop () = 
        async {
            let! (msg:Message) = inbox.Receive()
            match msg.Target with
            | Console -> 
                let text = if not verbose then shortenCurrentDirectory msg.Text else msg.Text
                let curColor = Console.ForegroundColor
                Console.ForegroundColor <- msg.Color
                if msg.Important && buildServer <> CCNet then
                    if msg.Newline then eprintfn "%s" text else eprintf "%s" text
                else
                    if msg.Newline then printfn "%s" text else printf "%s" text
                Console.ForegroundColor <- curColor
            | Xml     -> 
                if xmlWriter = null then openWriter()
                xmlWriter.WriteLine msg.Text
                xmlWriter.Flush()
                if AutoCloseXmlWriter || msg.Text = "</buildresults>" then closeWriter()
                
            return! loop ()}

    loop ())

/// Checks if the current message queue is empty
let MessageBoxIsEmpty() = buffer.CurrentQueueLength = 0