[<AutoOpen>]
module Fake.MailBoxHelper

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

let locker = new obj()

/// Logs the specified string to the console
let internal logMessageToConsole (msg:Message) =   
    let text = toRelativePath msg.Text
    lock locker (fun () -> 
        let curColor = Console.ForegroundColor
        Console.ForegroundColor <- msg.Color
        if msg.Important && buildServer <> CCNet then
            if msg.Newline then eprintfn "%s" text else eprintf "%s" text
        else
            if msg.Newline then printfn "%s" text else printf "%s" text
        Console.ForegroundColor <- curColor)

let private appendXML line = AppendToFile xmlOutputFile [line]

let buffer = MailboxProcessor.Start (fun inbox ->
    let rec loop () = 
        async {
            let! (msg:Message) = inbox.Receive()
            match traceMode with
            | Console -> logMessageToConsole msg
            | Xml     -> appendXML msg.Text

            return! loop ()}

    loop ())

/// Checks if the current message queue is empty
let MessageBoxIsEmpty() = buffer.CurrentQueueLength = 0