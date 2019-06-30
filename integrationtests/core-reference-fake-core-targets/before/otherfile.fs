module OtherFile

open Fake.Core

let createTargets () =

    Target.create "OtherFileTarget" (fun p ->
        printfn "Doing Something."
    )