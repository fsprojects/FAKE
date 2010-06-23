[<AutoOpen>]
module Fake.Git.Branches

open Fake

let private cleanBranches text = 
    text
      |> Seq.map ("^[* ] " >=> "") 
      |> Seq.toList

/// Gets all local branches
let getLocalBranches () =
   CommandHelper.getGitResult "branch"
     |> cleanBranches

/// Gets all remote branches
let getRemoteBranches () =
   CommandHelper.getGitResult "branch -r"
     |> cleanBranches

/// Gets all local and remote branches
let getAllBranches () = 
   CommandHelper.getGitResult "branch -a"
     |> cleanBranches

/// Returns the SHA1 of the given head
let getSHA1 = CommandHelper.runSimpleGitCommand << sprintf "rev-parse %s"

/// Returns the SHA1 of the merge base of the two given commits
let findMergeBase branch1 branch2 =
    sprintf "merge-base %s %s" branch1 branch2
      |> CommandHelper.runSimpleGitCommand

/// Returns the number of revisions between the two given commits
let revisionsBetween branch1 branch2 =
    let _,msg,_ =
      sprintf "rev-list %s..%s" branch1 branch2
        |> CommandHelper.runGitCommand
    msg.Count

/// Creates a new branch based on the given baseBranch and checks it out to the working copy
let checkoutNewBranch baseBranch branch =
    sprintf "checkout -b %s %s" branch baseBranch
      |> CommandHelper.gitCommand      

/// Performs a checkout of the given branch to the working copy
let checkoutBranch branch =
    sprintf "checkout %s" branch
      |> CommandHelper.gitCommand

/// Performs a merge of the given branch with the current branch
let merge flags branch =
    sprintf "merge %s %s" flags branch
      |> CommandHelper.gitCommand

/// Deletes the given branch
let deleteBranch force branch =
    sprintf "branch %s %s" (if force then "-D" else "-d") branch
      |> CommandHelper.gitCommand

/// Deletes the given tag
let deleteTag tag =
    sprintf "tag -d %s" tag
      |> CommandHelper.gitCommand

type MergeType =
| SameCommit
| FirstNeedsFastForward
| SecondNeedsFastForward
| NeedsRealMerge

/// Tests whether branches and their "origin" counterparts have diverged and need
/// merging first. It returns error codes to provide more detail, like so:
///
/// 0 - Branch heads point to the same commit
/// 1 - First given branch needs fast-forwarding
/// 2 - Second given branch needs fast-forwarding
/// 3 - Branch needs a real merge
let compareBranches local remote =
    let commit1 = getSHA1 local
    let commit2 = getSHA1 remote
    if commit1 = commit2 then SameCommit else
    match findMergeBase commit1 commit2 with
    | x when x = commit1 -> FirstNeedsFastForward
    | x when x = commit2 -> SecondNeedsFastForward
    | _  -> NeedsRealMerge