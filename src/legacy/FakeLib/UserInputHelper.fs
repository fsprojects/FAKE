[<AutoOpen>]
/// This module contains functions which allow to interactively input values
module Fake.UserInputHelper

open System

let internal erasePreviousChar () =
    try
        let left = if Console.CursorLeft <> 0 then Console.CursorLeft-1 else Console.BufferWidth-1
        let top = if Console.CursorLeft <> 0 then Console.CursorTop else Console.CursorTop-1

        Console.SetCursorPosition(left, top)
        Console.Write(' ')
        Console.SetCursorPosition(left, top)    
    with
    | :? IO.IOException ->
        // Console is dumb, might be redirected. We don't care,
        // if it isn't a screen the visual feedback isn't required
        ()

let internal readString (echo: bool) : string =
    let rec loop cs =
        let key = Console.ReadKey(true)
        match (key.Key, cs) with
        | (ConsoleKey.Backspace, []) -> loop []
        | (ConsoleKey.Backspace, _::cs) -> 
            erasePreviousChar ()
            loop cs
        | (ConsoleKey.Enter, _) -> cs
        | _ ->
            if echo then Console.Write(key.KeyChar) else Console.Write('*')
            loop (key.KeyChar :: cs)
    
    loop []
    |> List.rev
    |> Array.ofList
    |> fun cs -> new String(cs)

let internal color (color: ConsoleColor) (code : unit -> _) =
    let before = Console.ForegroundColor
    try
      Console.ForegroundColor <- color
      code ()
    finally
      Console.ForegroundColor <- before
    
/// Return a string entered by the user followed by enter. The input is echoed to the screen.
let getUserInput prompt =
    color ConsoleColor.White (fun _ -> printf "%s" prompt)
    let s = readString true
    printfn ""
    s

/// Return a string entered by the user followed by enter. The input is replaced by '*' on the screen.
let getUserPassword prompt =
    color ConsoleColor.White (fun _ -> printf "%s" prompt)
    let s = readString false
    printfn ""
    s
