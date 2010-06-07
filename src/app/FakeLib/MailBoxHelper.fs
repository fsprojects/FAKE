[<AutoOpen>]
module Fake.MailBoxHelper

/// Trace verbose output
let mutable verbose = hasBuildParam "verbose"

open System

type Message = 
    { Text      : string
      Color     : ConsoleColor
      Newline   : bool
      Important : bool}

let defaultMessage = 
    { Text      = ""
      Color     = ConsoleColor.White
      Newline   = true
      Important = false }

let buffer = MailboxProcessor.Start (fun inbox ->
    let rec loop () = 
        async {
            let! (msg:Message) = inbox.Receive()
            match traceMode with
            | Console -> 
                let text = if not verbose then toRelativePath msg.Text else msg.Text
                let curColor = Console.ForegroundColor
                Console.ForegroundColor <- msg.Color
                if msg.Important && buildServer <> CCNet then
                    if msg.Newline then eprintfn "%s" text else eprintf "%s" text
                else
                    if msg.Newline then printfn "%s" text else printf "%s" text
                Console.ForegroundColor <- curColor
            | Xml     -> AppendToFile xmlOutputFile [msg.Text]

            return! loop ()}

    loop ())

/// Checks if the current message queue is empty
let MessageBoxIsEmpty() = buffer.CurrentQueueLength = 0