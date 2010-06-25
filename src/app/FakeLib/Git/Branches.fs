[<AutoOpen>]
module Fake.Git.Branches

open Fake

let private cleanBranches text = 
    text
      |> Seq.map ("^[* ] " >=> "") 
      |> Seq.toList

/// Gets all local branches
let getLocalBranches repositoryDir =
   getGitResult repositoryDir "branch"
     |> cleanBranches

/// Gets all remote branches
let getRemoteBranches repositoryDir =
   getGitResult repositoryDir "branch -r"
     |> cleanBranches

/// Gets all local and remote branches
let getAllBranches repositoryDir = 
   getGitResult repositoryDir "branch -a"
     |> cleanBranches

/// Returns the SHA1 of the given head
let getSHA1 repositoryDir s = runSimpleGitCommand repositoryDir (sprintf "rev-parse %s" s)

/// Returns the SHA1 of the merge base of the two given commits
let findMergeBase repositoryDir branch1 branch2 =
    sprintf "merge-base %s %s" branch1 branch2
      |> runSimpleGitCommand repositoryDir

/// Returns the number of revisions between the two given commits
let revisionsBetween repositoryDir branch1 branch2 =
    let _,msg,_ =
      sprintf "rev-list %s..%s" branch1 branch2
        |> runGitCommand repositoryDir
    msg.Count

/// Creates a new branch based on the given baseBranch and checks it out to the working copy
let checkoutNewBranch repositoryDir baseBranch branch =
    sprintf "checkout -b %s %s" branch baseBranch
      |> gitCommand repositoryDir

/// Performs a checkout of the given branch to the working copy
let checkoutBranch repositoryDir branch =
    sprintf "checkout %s" branch
      |> gitCommand repositoryDir

/// Performs a merge of the given branch with the current branch
let merge repositoryDir flags branch =
    sprintf "merge %s %s" flags branch
      |> gitCommand repositoryDir

/// Deletes the given branch
let deleteBranch repositoryDir force branch =
    sprintf "branch %s %s" (if force then "-D" else "-d") branch
      |> gitCommand repositoryDir

/// Deletes the given tag
let deleteTag repositoryDir tag =
    sprintf "tag -d %s" tag
      |> gitCommand repositoryDir

type MergeType =
| SameCommit
| FirstNeedsFastForward
| SecondNeedsFastForward
| NeedsRealMerge

/// Tests whether branches and their "origin" counterparts have diverged and need
/// merging first. It returns error codes to provide more detail, like so:
///
/// 0 - repositoryDir
/// 1 - Branch heads point to the same commit
/// 2 - First given branch needs fast-forwarding
/// 3 - Second given branch needs fast-forwarding
/// 4 - Branch needs a real merge
let compareBranches repositoryDir local remote =
    let commit1 = getSHA1 repositoryDir local
    let commit2 = getSHA1 repositoryDir remote
    if commit1 = commit2 then SameCommit else
    match findMergeBase repositoryDir commit1 commit2 with
    | x when x = commit1 -> FirstNeedsFastForward
    | x when x = commit2 -> SecondNeedsFastForward
    | _  -> NeedsRealMerge