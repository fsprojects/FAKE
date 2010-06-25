[<AutoOpen>]
module Fake.Git.CommitMessage

open Fake
open System
open System.IO

let getCommitMessageFileName repositoryDir = new FileInfo((findGitDir repositoryDir).FullName + "\\COMMITMESSAGE")

/// Sets the commit message
let setMessage repositoryDir text =
    let messageFile = getCommitMessageFileName repositoryDir
    if String.IsNullOrEmpty text then
        if messageFile.Exists then messageFile.Delete()
    else
        use textWriter = new StreamWriter(messageFile.FullName, false)
        textWriter.Write text