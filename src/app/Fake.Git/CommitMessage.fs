[<AutoOpen>]
module Fake.Git.CommitMessage

open Fake
open System
open System.IO

let getCommitMessageFileInfo repositoryDir = 
    (findGitDir repositoryDir).FullName + "\\COMMITMESSAGE"
      |> fileInfo

/// Sets the commit message
let setMessage repositoryDir text =
    let messageFile = getCommitMessageFileInfo repositoryDir
    if isNullOrEmpty text then
        if messageFile.Exists then messageFile.Delete()
    else
        use textWriter = new StreamWriter(messageFile.FullName, false)
        textWriter.Write text