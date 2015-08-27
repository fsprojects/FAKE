[<AutoOpen>]
/// This module contains functions which allow to interactively input values
module Fake.UserInputHelper

open System

let internal readString (echo: bool) : string =
    let rec loop cs =
        let key = Console.ReadKey(not echo)
        match key.Key with
        | ConsoleKey.Backspace -> match cs with
                                  | [] -> loop []
                                  | _::cs -> loop cs
        | ConsoleKey.Enter -> cs
        | _ -> loop (key.KeyChar :: cs)

    loop []
    |> List.rev
    |> Array.ofList
    |> fun cs -> new String(cs)

let internal color (color: ConsoleColor) (code : unit -> _) =
    let before = Console.ForegroundColor
    Console.ForegroundColor <- color
    let result = code ()
    Console.ForegroundColor <- before
    result
    
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
