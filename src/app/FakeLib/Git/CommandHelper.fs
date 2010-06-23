[<AutoOpen>]
module Fake.Git.CommandHelper

open System
open System.Diagnostics
open System.IO
open System.Threading  
open System.Text
open System.Collections.Generic
open Fake

let gitPath = 
    let ev = environVar "GIT"
    if not (isNullOrEmpty ev) then ev else findPath "GitPath" "git.exe"
    
let gitExPath = 
    let ev = environVar "GIT"
    if not (isNullOrEmpty ev) then ev else findPath "GitExPath" "gitex.cmd"

let getString sequence =
    let sb = sequence |> Seq.fold (fun (sb:StringBuilder) (s:string) -> sb.Append s) (new StringBuilder())
    sb.ToString()

let runGitCommand command = 
    let ok,msg,errors = 
        ExecProcessAndReturnMessages (fun info ->  
          info.FileName <- gitPath
          info.Arguments <- command)
    ok,msg,toLines errors

let runGitCommandf fmt = Printf.ksprintf runGitCommand fmt

let getGitResult command = 
    let _,msg,_ = runGitCommand command
    msg

let directRunGitCommand command = 
    execProcess2 (fun info ->  
      info.FileName <- gitPath
      info.Arguments <- command)

let gitCommand command =
    let ok,msg,error = runGitCommand command

    if not ok then failwith error else 
    msg |> Seq.iter (printfn "%s")

let gitCommandf fmt = Printf.ksprintf gitCommand fmt

let showGitCommand command =
    let ok,msg,errors = runGitCommand command
    msg |> Seq.iter (printfn "%s")
    if errors <> "" then
      printfn "Errors: %s" errors

let runGitExCommand command = 
    execProcess (fun info ->  
      info.FileName <- gitExPath
      info.Arguments <- command)

let runAsyncGitExCommand command = 
    use p = new Process()
    p.StartInfo.UseShellExecute <- false
    p.StartInfo.FileName <- gitExPath
    p.StartInfo.Arguments <- command
    p.Start()

/// Runs the git command and returns the first line of the result
let runSimpleGitCommand command =
    try
        let ok,msg,errors = runGitCommand command
        try
            msg.[0]
        with 
        | exn -> failwithf "Git didn't return a msg.\r\n%s" errors
    with 
    | exn -> failwithf "Could not run \"git %s\".\r\nError: %s" command exn.Message

let fixPath (path:string) =
    let path = path.Trim()
    if "\\\\" <* path then path.Trim() else path.Replace('\\', '/').Trim()

let workingDirGitDir =
    lazy (
        let di = new System.IO.DirectoryInfo("./.git")
        
        if di.Exists then di.FullName else
        let di2 = new  System.IO.DirectoryInfo(".")
        failwithf ".git directory not found in current woking copy (%s)" di2.FullName)

let getWorkingDirGitDir() = workingDirGitDir.Force()