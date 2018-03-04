[<AutoOpen>]
/// Contains helper functions which can be used to retrieve file status information from git.
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
module Fake.Git.FileStatus

#nowarn "44"
open Fake
open System
open System.IO

/// A type which represents a file status in git.
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
type FileStatus =
| Added
| Copied
| Deleted
| Modified
| Renamed
| TypeChange
    with 
        static member Parse = function
          | "A" -> Added
          | c when c.StartsWith "C" -> Copied
          | "D" -> Deleted
          | m when m.StartsWith "M" -> Modified
          | r when r.StartsWith "R" -> Renamed
          | "T" -> TypeChange
          | _ -> Modified
 
/// Gets the changed files between the given revisions
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let getChangedFiles repositoryDir revision1 revision2 =
    checkRevisionExists repositoryDir revision1
    if revision2 <> "" then
        checkRevisionExists repositoryDir revision2

    let _,msg,_ = runGitCommand repositoryDir <| sprintf "diff %s %s --name-status" revision1 revision2
    msg
      |> Seq.map (fun line -> 
            let a = line.Split('\t')
            FileStatus.Parse a.[0],a.[1])

/// Gets all changed files in the current revision
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let getAllFiles repositoryDir = 
    let _,msg,_ = runGitCommand repositoryDir <| sprintf "ls-files"
    msg
      |> Seq.map (fun line -> Added,line)
                  
/// Gets the changed files since the given revision incl. changes in the working copy
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let getChangedFilesInWorkingCopy repositoryDir revision = getChangedFiles repositoryDir revision ""

/// Gets all conflicted files
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let getConflictedFiles repositoryDir =
    let _,msg,_ = runGitCommand repositoryDir "ls-files --unmerged"
    msg 
      |> Seq.map (fun file -> file.LastIndexOfAny([| ' '; '\t'|]),file)
      |> Seq.filter (fun (index,file) -> index > 0)
      |> Seq.map (fun (index,file) -> file.Substring(index + 1))
      |> Seq.toList

/// Returns true if the working copy is in a conflicted merge otherwise false
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let isInTheMiddleOfConflictedMerge repositoryDir = [] <> getConflictedFiles repositoryDir

/// Returns the current rebase directory for the given repository.
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let getRebaseDir (repositoryDir:string) =
    if Directory.Exists(repositoryDir + ".git\\rebase-apply\\") then
        repositoryDir + ".git\\rebase-apply\\"
    elif Directory.Exists(repositoryDir + ".git\\rebase\\") then
        repositoryDir + ".git\\rebase\\"
    else ""

/// Returns true if the given repository is in the middle of a rebase process.
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let isInTheMiddleOfRebase repositoryDir = 
    let rebaseDir = getRebaseDir repositoryDir
    Directory.Exists rebaseDir && (not <| File.Exists(rebaseDir + "applying"))

/// Returns true if the given repository is in the middle of a patch process.
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let isInTheMiddleOfPatch repositoryDir =
    let rebaseDir = getRebaseDir repositoryDir
    Directory.Exists rebaseDir && (not <| File.Exists(rebaseDir + "rebasing"))

/// Cleans the working copy by doing a git reset --hard and a clean -f.
[<System.Obsolete("Use Fake.Tools.Git.FileStatus instead")>]
let cleanWorkingCopy repositoryDir = 
    ResetHard repositoryDir
    showGitCommand repositoryDir "clean -f"