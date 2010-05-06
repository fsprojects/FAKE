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

/// Logs the specified string to the console
let private logMessageToConsole important newLine s =   
    let r = toRelativePath s
    if important && buildServer <> CCNet then
        if newLine then eprintfn "%s" r else eprintf "%s" r
    else
        if newLine then printfn "%s" r else printf "%s" r

let private appendXML line = AppendToFile xmlOutputFile [line]

let buffer = MailboxProcessor.Start (fun inbox ->
    let rec loop () = 
        async {
            let! (msg:Message) = inbox.Receive()
            match traceMode with
            | Console ->
                let curColor = Console.ForegroundColor
                Console.ForegroundColor <- msg.Color
                logMessageToConsole msg.Important msg.Newline msg.Text
                Console.ForegroundColor <- curColor
            | Xml     -> appendXML msg.Text

            return! loop ()}

    loop ())