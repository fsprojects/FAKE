[<AutoOpen>]
module Fake.Git.Information

open Fake
open System.IO

/// Gets the git branch name
let getBranchName() = 
    let ok,msg,errors = runGitCommand "status"
    let s = msg |> Seq.head 
    if s = "# Not currently on any branch." then "NoBranch" else
    s.Replace("# On branch ","")
    
/// Gets the git version
let getVersion() = 
    let ok,msg,errors = runGitCommand "--version"
    msg |> getString

/// Searches the git dir recursivly up to the root
let findGitDir() =
    let rec findGitDir (dirInfo:DirectoryInfo) =
        let gitDir = new DirectoryInfo(dirInfo.FullName + @"\.git")
        if gitDir.Exists then gitDir else findGitDir dirInfo.Parent

    findGitDir (new DirectoryInfo("."))

/// Returns the SHA1 of the current HEAD
let getCurrentSHA1() = getSHA1 "HEAD"

/// Shows the git status
let showStatus() = showGitCommand "status"

/// Checks if the working copy is clean
let isCleanWorkingCopy() =
    let ok,msg,errors = runGitCommand "status"
    msg |> Seq.fold (fun acc s -> acc || "nothing to commit" <* s) false

/// Returns a friendly name from a SHA1
let showName sha1 =
    let ok,msg,errors = runGitCommand <| sprintf "name-rev %s" sha1
    if msg.Count = 0 then sha1 else msg.[0] 
    
/// Returns the merge base of rev1 and rev2
let mergeBase rev1 rev2 = 
    sprintf "merge-base %s %s" rev1 rev2
      |> runSimpleGitCommand

/// Returns true if rev1 is ahead of rev2
let isAheadOf rev1 rev2 = 
    if rev1 = rev2 then false else
    mergeBase rev1 rev2 = rev2

