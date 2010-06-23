[<AutoOpen>]
module Fake.Git.CommitMessage

open Fake
open System
open System.IO

let messageFile = new FileInfo(getWorkingDirGitDir() + "\\COMMITMESSAGE")

/// Sets the commit message
let setMessage text =
    if String.IsNullOrEmpty text then
        if messageFile.Exists then messageFile.Delete()
    else
        use textWriter = new StreamWriter(messageFile.FullName, false)
        textWriter.Write text