[<AutoOpen>]
module Fake.Git.Reset

open System
open Fake

let internal addArgs commit file =
    sprintf "%s%s"
      (if String.IsNullOrEmpty commit then "" else " \"" + commit + "\"")
      (if String.IsNullOrEmpty file then "" else " -- \"" + file + "\"")

let soft commit file = "reset --soft" + addArgs commit file |> gitCommand

let mixed commit file = "reset --mixed" + addArgs commit file |> gitCommand

let hard commit file = "reset --hard" + addArgs commit file |> gitCommand

let ResetSoft() = soft null null
let ResetMixed() = mixed null null
let ResetHard() = hard null null