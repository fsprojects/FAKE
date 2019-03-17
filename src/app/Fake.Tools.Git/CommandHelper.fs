/// Contains helpers which allow to interact with [git](http://git-scm.com/) via the command line.
module Fake.Tools.Git.CommandHelper

open System
open System.IO
open Fake.Core
open Fake.Core.String.Operators
open Fake.IO


/// Specifies a global timeout for git.exe - default is *no timeout*
let mutable gitTimeOut = TimeSpan.MaxValue

let private GitPath = @"[ProgramFiles]\Git\cmd\;[ProgramFilesX86]\Git\cmd\;[ProgramFiles]\Git\bin\;[ProgramFilesX86]\Git\bin\;"

/// Tries to locate the git.exe via the eviroment variable "GIT".
let gitPath =
    if Environment.isUnix then
        "git"
    else
        let ev = Environment.environVar "GIT"
        if not (String.isNullOrEmpty ev) then ev else Process.findPath "GitPath" GitPath "git.exe"

let inline private setInfo gitPath repositoryDir command (info:ProcStartInfo) =
    { info with
        FileName = gitPath
        WorkingDirectory = repositoryDir
        Arguments = command }

/// Runs git.exe with the given command in the given repository directory.
let runGitCommand repositoryDir command =
    let processResult =
        Process.execWithResult (setInfo gitPath repositoryDir command) gitTimeOut

    processResult.OK,processResult.Messages,String.toLines processResult.Errors

/// [omit]
let runGitCommandf fmt = Printf.ksprintf runGitCommand fmt

/// [omit]
let getGitResult repositoryDir command =
    let _,msg,_ = runGitCommand repositoryDir command
    msg

/// Fires the given git command ind the given repository directory and returns immediatly.
let fireAndForgetGitCommand repositoryDir command =
    Process.fireAndForget (setInfo gitPath repositoryDir command)

/// Runs the given git command, waits for its completion and returns whether it succeeded.
let directRunGitCommand repositoryDir command =
    Process.directExec (setInfo gitPath repositoryDir command)

/// Runs the given git command, waits for its completion and fails when it didn't succeeded.
let directRunGitCommandAndFail repositoryDir command =
    directRunGitCommand repositoryDir command
    |> fun ok -> if not ok then failwith "Command failed."

/// Runs the given git command, waits for its completion.
let gitCommand repositoryDir command =
    let ok,msg,error = runGitCommand repositoryDir command

    if not ok then failwith error else
    msg |> Seq.iter (Trace.logfn "%s")

/// [omit]
let gitCommandf repositoryDir fmt = Printf.ksprintf (gitCommand repositoryDir) fmt

/// Runs the given git command, waits for its completion.
/// This version doesn't throw an exception if an error occurs. It just traces the error.
let showGitCommand repositoryDir command =
    let _,msg,errors = runGitCommand repositoryDir command
    msg |> Seq.iter (Trace.logfn "%s")
    if errors <> "" then
      Trace.traceError <| sprintf "Errors: %s" errors

/// Runs the git command and returns the first line of the result.
let runSimpleGitCommand repositoryDir command =
    try
        let _,msg,errors = runGitCommand repositoryDir command

        let errorText = String.toLines msg + Environment.NewLine + errors
        if errorText.Contains "fatal: " then
            failwith errorText

        if msg.Length = 0 then "" else
        msg |> Seq.iter (Trace.logfn "%s")
        msg.[0]
    with
    | exn -> failwithf "Could not run \"git %s\".\r\nError: %s" command exn.Message

/// [omit]
let fixPath (path:string) =
    let path = path.Trim()
    if "\\\\" <* path then path.Trim() else path.Replace('\\', '/').Trim()

/// Searches for a .git directory in the specified directory or any parent directory.
/// <exception href="System.InvalidOperationException">Thrown when no .git directory is found.</exception>
let findGitDir repositoryDir =
    let rec findGitDir (dirInfo:DirectoryInfo) =
        let gitDir = dirInfo.FullName + Path.directorySeparator + ".git" |> DirectoryInfo.ofPath
        if gitDir.Exists then gitDir
        elif isNull dirInfo.Parent then
            invalidOp "Not a git repository: no .git directory found in the specified directory or any parent directory."
        else
            findGitDir dirInfo.Parent

    if String.isNullOrEmpty repositoryDir then "." else repositoryDir
    |> DirectoryInfo.ofPath
    |> findGitDir
