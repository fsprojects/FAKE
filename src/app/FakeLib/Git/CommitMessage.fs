[<AutoOpen>]
/// Contains helper functions which allow to get and set the git commit message.
module Fake.Git.CommitMessage

open Fake
open System
open System.Text
open System.IO

/// Returns the commit message file.
let getCommitMessageFileInfo repositoryDir =
    let gitDir = findGitDir repositoryDir
    let oldgitFileInfo = gitDir.FullName </> "COMMITMESSAGE" |> fileInfo
    if oldgitFileInfo.Exists then oldgitFileInfo
    else gitDir.FullName </> "COMMIT_EDITMSG" |> fileInfo

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
        use textWriter = new StreamWriter(messageFile.FullName, false, new UTF8Encoding(true))
        textWriter.Write text