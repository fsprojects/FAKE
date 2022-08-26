namespace Fake.Tools.Git

open Fake.Core.String.Operators

/// <namespacedoc>
/// <summary>
/// Tools.Git namespace contains tasks to interact with Git version control system tool
/// </summary>
/// </namespacedoc>
/// 
/// <summary>
/// Contains helper functions which allow to deal with git branches.
/// </summary>
[<RequireQualifiedAccess>]
module Branches =

    let private cleanBranches text =
        text |> Seq.map ("^[* ] " >=> "") |> Seq.toList

    /// <summary>
    /// Gets all local branches from the given repository.
    /// </summary>
    ///
    /// <param name="repositoryDir">The path of the target directory.</param>
    ///  - `repositoryDir` - 
    let getLocalBranches repositoryDir =
        CommandHelper.getGitResult repositoryDir "branch" |> cleanBranches

    /// <summary>
    /// Gets all remote branches from the given repository.
    /// </summary>
    ///
    /// <param name="repositoryDir">The path of the target directory.</param>
    let getRemoteBranches repositoryDir =
        CommandHelper.getGitResult repositoryDir "branch -r" |> cleanBranches

    /// <summary>
    /// Gets all local and remote branches from the given repository.
    /// </summary>
    ///
    /// <param name="repositoryDir">The path of the target directory.</param>
    let getAllBranches repositoryDir =
        CommandHelper.getGitResult repositoryDir "branch -a" |> cleanBranches

    /// <summary>
    /// Returns the SHA1 of the given commit from the given repository.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="commit">The commit for which git should return the SHA1 - can be <c>HEAD</c>, <c>HEAD~1</c>, ... ,
    /// a branch name or a prefix of a SHA1.</param>
    let getSHA1 repositoryDir commit =
        CommandHelper.runSimpleGitCommand repositoryDir (sprintf "rev-parse %s" commit)

    /// <summary>
    /// Returns the SHA1 of the merge base of the two given commits from the given repository.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="commit1">The first commit for which git should find the merge base.</param>
    /// <param name="commit2">The second commit for which git should find the merge base.</param>
    let findMergeBase repositoryDir commit1 commit2 =
        sprintf "merge-base %s %s" commit1 commit2 |> CommandHelper.runSimpleGitCommand repositoryDir

    /// <summary>
    /// Returns the number of revisions between the two given commits.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="commit1">The first commit for which git should find the merge base.</param>
    /// <param name="commit2">The second commit for which git should find the merge base.</param>
    let revisionsBetween repositoryDir commit1 commit2 =
        let _, msg, _ =
            sprintf "rev-list %s..%s" commit1 commit2 |> CommandHelper.runGitCommand repositoryDir

        msg.Length

    /// <summary>
    /// Performs a checkout of the given branch to the working copy.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="branch">The branch for the checkout.</param>
    let checkoutBranch repositoryDir branch =
        sprintf "checkout %s" branch |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Performs a checkout of the given branch with an additional tracking branch.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="trackBranch">The tracking branch.</param>
    /// <param name="branch">The branch for the checkout.</param>
    let checkoutTracked repositoryDir trackBranch branch =
        CommandHelper.gitCommandf repositoryDir "checkout --track -b %s %s" branch trackBranch

    /// <summary>
    /// Creates a new branch based on the given baseBranch and checks it out to the working copy.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="baseBranch">The base branch.</param>
    /// <param name="branch">The new branch.</param>
    let checkoutNewBranch repositoryDir baseBranch branch =
        sprintf "checkout -b %s %s" branch baseBranch |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Performs a checkout of the given branch to the working copy.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="create">Set this to true if the branch is new.</param>
    /// <param name="branch">The new branch.</param>
    let checkout repositoryDir create branch =
        CommandHelper.gitCommandf repositoryDir "checkout %s %s" (if create then "-b" else "") branch

    /// <summary>
    /// Creates a new branch from the given commit.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="newBranchName">The new branch.</param>
    /// <param name="commit">The commit which git should take as the new HEAD. - can be <c>HEAD</c>, <c>HEAD~1</c>, ...
    /// , a branch name or a prefix of a SHA1.</param>
    ///  - `repositoryDir` - 
    ///  - `newBranchName` - 
    ///  - `commit` - 
    let createBranch repositoryDir newBranchName commit =
        sprintf "branch -f %s %s" newBranchName commit |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Deletes the given branch.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="force">Determines if git should be run with the <b>force</b> flag.</param>
    /// <param name="branch">The branch which should be deleted.</param>
    let deleteBranch repositoryDir force branch =
        sprintf "branch %s %s" (if force then "-D" else "-d") branch
        |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Tags the current branch.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="tag">The new tag.</param>
    let tag repositoryDir tag =
        sprintf "tag %s" tag |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Deletes the given tag.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="tag">The tag which should be deleted.</param>
    let deleteTag repositoryDir tag =
        sprintf "tag -d %s" tag |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Pushes all branches to the default remote.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let push repositoryDir =
        CommandHelper.directRunGitCommandAndFail repositoryDir "push"

    /// <summary>
    /// Pushes the given tag to the given remote.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="remote">The remote.</param>
    /// <param name="tag">The tag.</param>
    let pushTag repositoryDir remote tag =
        CommandHelper.directRunGitCommandAndFail repositoryDir (sprintf "push %s %s" remote tag)

    /// <summary>
    /// Pushes the given branch to the given remote.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="remote">The remote.</param>
    /// <param name="branch">The branch.</param>
    let pushBranch repositoryDir remote branch =
        CommandHelper.directRunGitCommandAndFail repositoryDir (sprintf "push %s %s" remote branch)

    /// <summary>
    /// Pulls a given branch from the given remote.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="remote">The name of the remote.</param>
    /// <param name="branch">The name of the branch to pull.</param>
    let pull repositoryDir remote branch =
        CommandHelper.directRunGitCommandAndFail repositoryDir (sprintf "pull %s %s" remote branch)
