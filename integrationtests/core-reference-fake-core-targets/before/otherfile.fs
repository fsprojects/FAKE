module OtherFile

open Fake.Core

let createTargets () =

    Target.create "OtherFileTarget" (fun _ ->
        printfn "Doing Something."
    )