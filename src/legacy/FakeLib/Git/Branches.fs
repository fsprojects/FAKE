[<AutoOpen>]
/// Contains helper functions which allow to deal with git branches.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
module Fake.Git.Branches

#nowarn "44"
open Fake

let private cleanBranches text = 
    text
      |> Seq.map ("^[* ] " >=> "") 
      |> Seq.toList

/// Gets all local branches from the given repository.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let getLocalBranches repositoryDir =
   getGitResult repositoryDir "branch"
     |> cleanBranches

/// Gets all remote branches from the given repository.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let getRemoteBranches repositoryDir =
   getGitResult repositoryDir "branch -r"
     |> cleanBranches

/// Gets all local and remote branches from the given repository.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let getAllBranches repositoryDir = 
   getGitResult repositoryDir "branch -a"
     |> cleanBranches

/// Returns the SHA1 of the given commit from the given repository.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit` - The commit for which git should return the SHA1 - can be HEAD, HEAD~1, ... , a branch name or a prefix of a SHA1.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let getSHA1 repositoryDir commit = runSimpleGitCommand repositoryDir (sprintf "rev-parse %s" commit)

/// Returns the SHA1 of the merge base of the two given commits from the given repository.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit1` - The first commit for which git should find the merge base.
///  - `commit2` - The second commit for which git should find the merge base.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let findMergeBase repositoryDir commit1 commit2 =
    sprintf "merge-base %s %s" commit1 commit2
      |> runSimpleGitCommand repositoryDir

/// Returns the number of revisions between the two given commits.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `commit1` - The first commit for which git should find the merge base.
///  - `commit2` - The second commit for which git should find the merge base.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let revisionsBetween repositoryDir commit1 commit2 =
    let _,msg,_ =
      sprintf "rev-list %s..%s" commit1 commit2
        |> runGitCommand repositoryDir
    msg.Count

/// Performs a checkout of the given branch to the working copy.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `branch` - The branch for the checkout.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let checkoutBranch repositoryDir branch =
    sprintf "checkout %s" branch
      |> gitCommand repositoryDir

/// Performs a checkout of the given branch with an additional tracking branch.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `trackBranch` - The tracking branch.
///  - `branch` - The branch for the checkout.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let checkoutTracked repositoryDir trackBranch branch =
    gitCommandf repositoryDir "checkout --track -b %s %s" branch trackBranch

/// Creates a new branch based on the given baseBranch and checks it out to the working copy.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `baseBranch` - The base branch.
///  - `branch` - The new branch.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let checkoutNewBranch repositoryDir baseBranch branch =
    sprintf "checkout -b %s %s" branch baseBranch
      |> gitCommand repositoryDir

/// Performs a checkout of the given branch to the working copy.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `create` - Set this to true if the branch is new.
///  - `branch` - The new branch.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let checkout repositoryDir create branch =
    gitCommandf repositoryDir "checkout %s %s"
        (if create then "-b" else "")
        branch

/// Creates a new branch from the given commit.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `newBranchName` - The new branch.
///  - `commit` - The commit which git should take as the new HEAD. - can be HEAD, HEAD~1, ... , a branch name or a prefix of a SHA1.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let createBranch repositoryDir newBranchName commit =
    sprintf "branch -f %s %s" newBranchName commit
      |> gitCommand repositoryDir

/// Deletes the given branch.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `force` - Determines if git should be run with the *force* flag.
///  - `branch` - The branch which should be deleted.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let deleteBranch repositoryDir force branch =
    sprintf "branch %s %s" (if force then "-D" else "-d") branch
      |> gitCommand repositoryDir

/// Tags the current branch.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `tag` - The new tag.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let tag repositoryDir tag =
    sprintf "tag %s" tag
      |> gitCommand repositoryDir

/// Deletes the given tag.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `tag` - The tag which should be deleted.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let deleteTag repositoryDir tag =
    sprintf "tag -d %s" tag
      |> gitCommand repositoryDir

/// Pushes all branches to the default remote.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let push repositoryDir = directRunGitCommandAndFail repositoryDir "push"

/// Pushes the given tag to the given remote.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `remote` - The remote.
///  - `tag` - The tag.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let pushTag repositoryDir remote tag = directRunGitCommandAndFail repositoryDir (sprintf "push %s %s" remote tag)

/// Pushes the given branch to the given remote.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `remote` - The remote.
///  - `branch` - The branch.
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let pushBranch repositoryDir remote branch = directRunGitCommandAndFail repositoryDir (sprintf "push %s %s" remote branch)

/// Pulls a given branch from the given remote.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `remote` - The name of the remote.
///  - `branch` - The name of the branch to pull
[<System.Obsolete("Use Fake.Tools.Git.Branches instead")>]
let pull repositoryDir remote branch = 
    directRunGitCommandAndFail repositoryDir (sprintf "pull %s %s" remote branch)