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

/// Return a string entered by the user followed by enter. The input is echoed to the screen.
let getUserInput prompt =
    printf "%s" prompt
    let s = readString true
    s

/// Return a string entered by the user followed by enter. The input is not echoed to the screen.
let getUserPassword prompt =
    printf "%s" prompt
    let s = readString false
    printfn ""
    s

