[<AutoOpen>]
module Fake.Git.FileStatus

open Fake
open System

type FileStatus =
| Added
| Modified
| Deleted
  with 
    static member parse s = 
      match s with
      | "A" -> Added
      | "M" -> Modified
      | "D" -> Deleted
      | _ -> failwith <| sprintf "Unknown file status %s" s
 
/// Gets the changed files between the given revisions
let getChangedFiles repositoryDir revision1 revision2 =    
    checkRevisionExists repositoryDir revision1
    if revision2 <> "" then
        checkRevisionExists repositoryDir revision2

    let _,msg,_ = runGitCommand repositoryDir <| sprintf "diff %s %s --name-status" revision1 revision2
    msg
      |> Seq.map (fun line -> 
            let a = line.Split('\t')
            FileStatus.parse a.[0],a.[1])

/// Gets the changed files since the given revision incl. changes in the working copy
let getChangedFilesInWorkingCopy repositoryDir revision = getChangedFiles repositoryDir revision ""

/// Gets all conflicted files
let getConflictedFiles repositoryDir =
    let _,msg,_ = runGitCommand repositoryDir "ls-files --unmerged"
    msg 
      |> Seq.map (fun file -> file.LastIndexOfAny([| ' '; '\t'|]),file)
      |> Seq.filter (fun (index,file) -> index > 0)
      |> Seq.map (fun (index,file) -> file.Substring(index + 1))
      |> Seq.toList

/// Returns true if the working copy is in a conflicted merge otherwise false
let isInTheMiddleOfConflictedMerge repositoryDir = Seq.isEmpty <| getConflictedFiles repositoryDir

/// Cleans the working copy by doing a git reset --hard and a clean -f
let cleanWorkingCopy repositoryDir = 
    ResetHard repositoryDir
    showGitCommand repositoryDir "clean -f"