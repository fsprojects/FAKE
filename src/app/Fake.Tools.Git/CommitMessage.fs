namespace Fake.Tools.Git

open Fake.IO
open Fake.Core
open Fake.IO.FileSystemOperators
open System.Text
open System.IO

/// <summary>
/// Contains helper functions which allow to get and set the git commit message.
/// </summary>
[<RequireQualifiedAccess>]
module CommitMessage =

    /// Returns the commit message file.
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let getCommitMessageFileInfos repositoryDir =
        let gitDir = CommandHelper.findGitDir repositoryDir

        [ gitDir.FullName </> "COMMITMESSAGE" |> FileInfo.ofPath
          gitDir.FullName </> "COMMIT_EDITMSG" |> FileInfo.ofPath ]

    /// Gets the commit message
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let getCommitMessage repositoryDir =
        match getCommitMessageFileInfos repositoryDir |> List.filter (fun fi -> fi.Exists) with
        | fi :: _ -> File.readAsString fi.FullName
        | _ -> ""

    /// Sets the commit message
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="text">The commit message text.</param>
    let setMessage repositoryDir text =
        for messageFile in getCommitMessageFileInfos repositoryDir do
            if String.isNullOrEmpty text then
                if messageFile.Exists then
                    messageFile.Delete()
            else
                use stream = File.OpenWrite(messageFile.FullName)
                use textWriter = new StreamWriter(stream, UTF8Encoding(true))
                textWriter.Write text
