[<AutoOpen>]
module Fake.Git.Reset

open System
open Fake

let internal addArgs commit file =
    sprintf "%s%s"
      (if String.IsNullOrEmpty commit then "" else " \"" + commit + "\"")
      (if String.IsNullOrEmpty file then "" else " -- \"" + file + "\"")

let soft repositoryDir commit file = "reset --soft" + addArgs commit file |> gitCommand repositoryDir
let mixed repositoryDir commit file = "reset --mixed" + addArgs commit file |> gitCommand repositoryDir
let hard repositoryDir commit file = "reset --hard" + addArgs commit file |> gitCommand repositoryDir

let ResetSoft repositoryDir = soft repositoryDir null null
let ResetMixed repositoryDir = mixed repositoryDir null null
let ResetHard repositoryDir = hard repositoryDir null null