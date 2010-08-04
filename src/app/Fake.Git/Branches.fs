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

/// <summary>Creates a new branch based on the given baseBranch and checks it out to the working copy</summary>
/// <param name="repositoryDir">The repository directory.</param>
/// <param name="baseBranch">The base branch.</param>
/// <param name="branch">The new branch.</param>
let checkoutNewBranch repositoryDir baseBranch branch =
    sprintf "checkout -b %s %s" branch baseBranch
      |> gitCommand repositoryDir

/// Performs a checkout of the given branch to the working copy
let checkoutBranch repositoryDir branch =
    sprintf "checkout %s" branch
      |> gitCommand repositoryDir

/// Creates a new branch from the given commit
let createBranch repositoryDir newBranchName fromCommit =
    sprintf "branch -f %s %s" newBranchName fromCommit
      |> gitCommand repositoryDir

/// Deletes the given branch
let deleteBranch repositoryDir force branch =
    sprintf "branch %s %s" (if force then "-D" else "-d") branch
      |> gitCommand repositoryDir

/// Tags the current branch
let tag repositoryDir tag =
    sprintf "tag %s" tag
      |> gitCommand repositoryDir

/// Deletes the given tag
let deleteTag repositoryDir tag =
    sprintf "tag -d %s" tag
      |> gitCommand repositoryDir

/// Checks a branch out
let checkoutTracked repositoryDir create trackBranch branch =
    gitCommandf repositoryDir "checkout --track -b %s %s" branch trackBranch


/// Checks a branch out
let checkout repositoryDir create branch =
    gitCommandf repositoryDir "checkout %s %s"
        (if create then "-b" else "")
        branch

/// Push all
let push repositoryDir = directRunGitCommand repositoryDir "push" |> ignore

/// Pull
let pull repositoryDir remote branch = 
    directRunGitCommand repositoryDir (sprintf "pull %s %s" remote branch) |> ignore