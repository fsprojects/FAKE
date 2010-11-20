[<AutoOpen>]
module Fake.Git.Information

open Fake
open System.IO

/// Gets the git branch name
let getBranchName repositoryDir = 
    let ok,msg,errors = runGitCommand repositoryDir "status"
    let s = msg |> Seq.head 
    if s = "# Not currently on any branch." then "NoBranch" else
    s.Replace("# On branch ","")
    
/// Gets the git version
let getVersion repositoryDir = 
    let ok,msg,errors = runGitCommand repositoryDir "--version"
    msg |> separated ""

/// Returns the SHA1 of the current HEAD
let getCurrentSHA1 repositoryDir = getSHA1 repositoryDir "HEAD"

/// Shows the git status
let showStatus repositoryDir = showGitCommand repositoryDir "status"

/// Checks if the working copy is clean
let isCleanWorkingCopy repositoryDir =
    let ok,msg,errors = runGitCommand repositoryDir "status"
    msg |> Seq.fold (fun acc s -> acc || "nothing to commit" <* s) false

/// Returns a friendly name from a SHA1
let showName repositoryDir sha1 =
    let ok,msg,errors = runGitCommand repositoryDir <| sprintf "name-rev %s" sha1
    if msg.Count = 0 then sha1 else msg.[0] 

/// Returns true if rev1 is ahead of rev2
let isAheadOf repositoryDir rev1 rev2 = 
    if rev1 = rev2 then false else
    findMergeBase repositoryDir rev1 rev2 = rev2

