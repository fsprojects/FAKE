[<AutoOpen>]
/// Contains helpers which allow to interact with [git](http://git-scm.com/) via the command line.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
module Fake.Git.CommandHelper
#nowarn "44"
open System
open System.Diagnostics
open System.IO
open System.Threading  
open System.Text
open System.Collections.Generic
open Fake

/// Specifies a global timeout for git.exe - default is *no timeout*
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let mutable gitTimeOut = TimeSpan.MaxValue

let private GitPath = @"[ProgramFiles]\Git\cmd\;[ProgramFilesX86]\Git\cmd\;[ProgramFiles]\Git\bin\;[ProgramFilesX86]\Git\bin\;"

/// Tries to locate the git.exe via the eviroment variable "GIT".
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let gitPath = 
    if isUnix then
        "git"
    else
        let ev = environVar "GIT"
        if not (isNullOrEmpty ev) then ev else findPath "GitPath" GitPath "git.exe"   

/// Runs git.exe with the given command in the given repository directory.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let runGitCommand repositoryDir command = 
    let processResult = 
        ExecProcessAndReturnMessages (fun info ->  
          info.FileName <- gitPath
          info.WorkingDirectory <- repositoryDir
          info.Arguments <- command) gitTimeOut

    processResult.OK,processResult.Messages,toLines processResult.Errors

/// [omit]
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let runGitCommandf fmt = Printf.ksprintf runGitCommand fmt

/// [omit]
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let getGitResult repositoryDir command = 
    let _,msg,_ = runGitCommand repositoryDir command
    msg

/// Fires the given git command ind the given repository directory and returns immediatly.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let fireAndForgetGitCommand repositoryDir command = 
    fireAndForget (fun info ->  
      info.FileName <- gitPath
      info.WorkingDirectory <- repositoryDir
      info.Arguments <- command)

/// Runs the given git command, waits for its completion and returns whether it succeeded.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let directRunGitCommand repositoryDir command = 
    directExec (fun info ->  
      info.FileName <- gitPath
      info.WorkingDirectory <- repositoryDir
      info.Arguments <- command)

/// Runs the given git command, waits for its completion and fails when it didn't succeeded.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let directRunGitCommandAndFail repositoryDir command = 
    directRunGitCommand repositoryDir command
    |> fun ok -> if not ok then failwith "Command failed."

/// Runs the given git command, waits for its completion.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let gitCommand repositoryDir command =
    let ok,msg,error = runGitCommand repositoryDir command

    if not ok then failwith error else 
    msg |> Seq.iter (logfn "%s")

/// [omit]
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let gitCommandf repositoryDir fmt = Printf.ksprintf (gitCommand repositoryDir) fmt

/// Runs the given git command, waits for its completion.
/// This version doesn't throw an exception if an error occurs. It just traces the error.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let showGitCommand repositoryDir command =
    let ok,msg,errors = runGitCommand repositoryDir command
    msg |> Seq.iter (logfn "%s")
    if errors <> "" then
      traceError <| sprintf "Errors: %s" errors

/// Runs the git command and returns the first line of the result.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let runSimpleGitCommand repositoryDir command =
    try
        let ok,msg,errors = runGitCommand repositoryDir command
               
        let errorText = toLines msg + Environment.NewLine + errors
        if errorText.Contains "fatal: " then
            failwith errorText

        if msg.Count = 0 then "" else
        msg |> Seq.iter (logfn "%s")
        msg.[0]
    with 
    | exn -> failwithf "Could not run \"git %s\".\r\nError: %s" command exn.Message

/// [omit]
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let fixPath (path:string) =
    let path = path.Trim()
    if "\\\\" <* path then path.Trim() else path.Replace('\\', '/').Trim()

/// Searches the .git directory recursivly up to the root.
[<System.Obsolete("Use Fake.Tools.Git.CommandHelper instead")>]
let findGitDir repositoryDir =
    let rec findGitDir (dirInfo:DirectoryInfo) =
        let gitDir = dirInfo.FullName + directorySeparator + ".git" |> directoryInfo
        if gitDir.Exists then gitDir else findGitDir dirInfo.Parent
 

    if isNullOrEmpty repositoryDir then "." else repositoryDir
      |> directoryInfo
      |> findGitDir
