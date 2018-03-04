/// Contains helper functions which allow to get and set the git commit message.
module Fake.Tools.Git.CommitMessage

open Fake.Tools.Git.CommandHelper
open Fake.IO
open Fake.Core
open Fake.IO.FileSystemOperators
open System.Text
open System.IO


/// Returns the commit message file.
let getCommitMessageFileInfos repositoryDir =
    let gitDir = findGitDir repositoryDir
    [gitDir.FullName </> "COMMITMESSAGE" |> FileInfo.ofPath
     gitDir.FullName </> "COMMIT_EDITMSG" |> FileInfo.ofPath ]

/// Gets the commit message
let getCommitMessage repositoryDir =
    match getCommitMessageFileInfos repositoryDir |> List.filter (fun fi -> fi.Exists) with
    | fi::_ -> File.readAsString fi.FullName
    | _ -> ""

/// Sets the commit message
let setMessage repositoryDir text =
    for messageFile in getCommitMessageFileInfos repositoryDir do
        if String.isNullOrEmpty text then
            if messageFile.Exists then messageFile.Delete()
        else
            use stream = File.OpenWrite(messageFile.FullName)
            use textWriter = new StreamWriter(stream, new UTF8Encoding(true))
            textWriter.Write text
