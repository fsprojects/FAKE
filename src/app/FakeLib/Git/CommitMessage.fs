[<AutoOpen>]
module Fake.Git.CommitMessage

open Fake
open System
open System.IO

let getCommitMessageFileInfo repositoryDir = 
    (findGitDir repositoryDir).FullName + "\\COMMITMESSAGE"
      |> fileInfo

/// Gets the commit message
let getCommitMessage repositoryDir = 
    let fi = getCommitMessageFileInfo repositoryDir
    if fi.Exists then ReadFileAsString fi.FullName else ""       

/// Sets the commit message
let setMessage repositoryDir text =
    let messageFile = getCommitMessageFileInfo repositoryDir
    if isNullOrEmpty text then
        if messageFile.Exists then messageFile.Delete()
    else
        use textWriter = new StreamWriter(messageFile.FullName, false)
        textWriter.Write text