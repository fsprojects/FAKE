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


type TraceMessage =
    | Die
    | Message of Message
    | ProcessAll of AsyncReplyChannel<unit>


let private buffer = MailboxProcessor.Start (fun inbox ->
    let rec loop () = 
        async {
            let! msg = inbox.Receive()
            match msg with
            | Die -> return ()
            | Message x -> 
                match x.Target with
                | Console -> 
                    let text = if not verbose then shortenCurrentDirectory x.Text else x.Text
                    let curColor = Console.ForegroundColor
                    Console.ForegroundColor <- x.Color
                    if x.Important && buildServer <> CCNet then
                        if x.Newline then eprintfn "%s" text else eprintf "%s" text
                    else
                        if x.Newline then printfn "%s" text else printf "%s" text
                    Console.ForegroundColor <- curColor
                | Xml     -> 
                    if xmlWriter = null then openWriter()
                    xmlWriter.WriteLine x.Text
                    xmlWriter.Flush()
                    if AutoCloseXmlWriter || x.Text = "</buildresults>" then closeWriter()
                
                return! loop ()
            | ProcessAll reply ->
                reply.Reply()
                return! loop () }

    loop ())

let postMessage msg = buffer.Post (Message msg)

/// Checks if the current message queue is empty
let MessageBoxIsEmpty() = buffer.CurrentQueueLength = 0

/// Waits until the message queue is empty
let WaitUntilEverythingIsPrinted () =
    buffer.PostAndReply(fun channel -> ProcessAll channel)