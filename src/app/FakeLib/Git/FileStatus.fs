[<AutoOpen>]
module Fake.Git.FileStatus

open Fake
open System

type FileStatus =
| Added
| Modified
| Deleted
  with 
    static member Get s = 
      match s with
      | "A" -> Added
      | "M" -> Modified
      | "D" -> Deleted
      | _ -> failwith <| sprintf "Unknown file status %s" s
 
/// Gets the changed files between the given revisions
let getChangedFiles revision1 revision2 =
    checkRevisionExists revision1
    if revision2 <> "" then
        checkRevisionExists revision2

    let ok,msg,errors = runGitCommand <| sprintf "diff %s %s --name-status" revision1 revision2
    msg
      |> Seq.map (fun line -> 
            let a = line.Split('\t')
            FileStatus.Get a.[0],a.[1])

/// Gets the changed files since the given revision incl. changes in the working copy
let getChangedFilesInWorkingCopy revision = getChangedFiles revision ""

/// Gets all conflicted files
let getConflictedFiles() =
    let ok,msg,errors = runGitCommand "ls-files --unmerged"
    msg 
      |> Seq.map (fun file -> file.LastIndexOfAny([| ' '; '\t'|]),file)
      |> Seq.filter (fun (index,file) -> index > 0)
      |> Seq.map (fun (index,file) -> file.Substring(index + 1))
      |> Seq.toList

/// Returns true if the working copy is in a conflicted merge otherwise false
let isInTheMiddleOfConflictedMerge() = Seq.isEmpty <| getConflictedFiles()

/// Shows the repository browser
let gitexBrowse () = runAsyncGitExCommand "browse" |> ignore
    
/// Shows the file history
let gitexFileHistory fileName =
    checkFileExists fileName
    sprintf "filehistory %s" fileName
      |> runGitExCommand
      |> ignore

/// Cleans the working copy by doing a git reset --hard and a clean -f
let cleanWorkingCopy () = 
    ResetHard()
    showGitCommand "clean -f"