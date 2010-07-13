[<AutoOpen>]
module Fake.Git.CommandHelper

open System
open System.Diagnostics
open System.IO
open System.Threading  
open System.Text
open System.Collections.Generic
open Fake

let mutable gitTimeOut = TimeSpan.MaxValue

let gitPath = 
    let ev = environVar "GIT"
    if not (isNullOrEmpty ev) then ev else findPath "GitPath" "git.exe"   

let runGitCommand repositoryDir command = 
    let ok,msg,errors = 
        ExecProcessAndReturnMessages (fun info ->  
          info.FileName <- gitPath
          info.WorkingDirectory <- repositoryDir
          info.Arguments <- command) gitTimeOut
    ok,msg,toLines errors

let runGitCommandf fmt = Printf.ksprintf runGitCommand fmt

let getGitResult repositoryDir command = 
    let _,msg,_ = runGitCommand repositoryDir command
    msg

/// Runs the given process and returns the exit code
let directExec infoAction =
    use p = new Process()
    p.StartInfo.UseShellExecute <- false
    infoAction p.StartInfo
  
    try
        p.Start() |> ignore
    with
    | exn -> failwithf "Start of process %s failed. %s" p.StartInfo.FileName exn.Message
  
    p.WaitForExit()
    
    p.ExitCode = 0

let directRunGitCommand repositoryDir command = 
    directExec (fun info ->  
      info.FileName <- gitPath
      info.WorkingDirectory <- repositoryDir
      info.Arguments <- command)

let gitCommand repositoryDir command =
    let ok,msg,error = runGitCommand repositoryDir command

    if not ok then failwith error else 
    msg |> Seq.iter (printfn "%s")

let gitCommandf repositoryDir fmt = Printf.ksprintf (gitCommand repositoryDir) fmt

let showGitCommand repositoryDir command =
    let ok,msg,errors = runGitCommand repositoryDir command
    msg |> Seq.iter (printfn "%s")
    if errors <> "" then
      printfn "Errors: %s" errors

/// Runs the git command and returns the first line of the result
let runSimpleGitCommand repositoryDir command =
    try
        let ok,msg,errors = runGitCommand repositoryDir command
        try
            msg.[0]
        with 
        | exn -> failwithf "Git didn't return a msg.\r\n%s" errors
    with 
    | exn -> failwithf "Could not run \"git %s\".\r\nError: %s" command exn.Message

let fixPath (path:string) =
    let path = path.Trim()
    if "\\\\" <* path then path.Trim() else path.Replace('\\', '/').Trim()

/// Searches the git dir recursivly up to the root
let findGitDir repositoryDir =
    let rec findGitDir (dirInfo:DirectoryInfo) =
        let gitDir = dirInfo.FullName @@ @"\.git" |> directoryInfo
        if gitDir.Exists then gitDir else findGitDir dirInfo.Parent
 

    if isNullOrEmpty repositoryDir then "." else repositoryDir
      |> directoryInfo
      |> findGitDir