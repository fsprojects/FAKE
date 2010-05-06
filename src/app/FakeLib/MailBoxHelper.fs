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
    if important && buildServer <> CCNet then
        if newLine then
            eprintfn "%s" (toRelativePath s)
        else 
            eprintf "%s" (toRelativePath s)
    else
        if newLine then
            printfn "%s" (toRelativePath s)
        else 
            printf "%s" (toRelativePath s)

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