namespace Fake.Tools.Git

open System.IO
open Fake.Tools.Git

/// <summary>
/// Contains helper functions which allow to deal with git merge.
/// </summary>
[<RequireQualifiedAccess>]
module Merge =

    /// <summary>
    /// Gets the current merge message.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let getMergeMessage repositoryDir =
        let file = (CommandHelper.findGitDir repositoryDir).FullName + "\\MERGE_MSG"
        if File.Exists file then File.ReadAllText file else ""

    /// <summary>
    /// Allows git to use fast-forward merges
    /// </summary>
    let FastForwardFlag = "--ff"

    /// <summary>
    /// Forbids git to use fast-forward merges
    /// </summary>
    let NoFastForwardFlag = "--no-ff"

    /// <summary>
    /// Git merge option.
    /// </summary>
    type MergeType =
        | SameCommit
        | FirstNeedsFastForward
        | SecondNeedsFastForward
        | NeedsRealMerge

    /// <summary>
    /// Tests whether branches and their "origin" counterparts have diverged and need merging first.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="local">The local branch name.</param>
    /// <param name="remote">The remote branch name.</param>
    let compareBranches repositoryDir local remote =
        let commit1 = Branches.getSHA1 repositoryDir local
        let commit2 = Branches.getSHA1 repositoryDir remote

        if commit1 = commit2 then
            SameCommit
        else
            match Branches.findMergeBase repositoryDir commit1 commit2 with
            | x when x = commit1 -> FirstNeedsFastForward
            | x when x = commit2 -> SecondNeedsFastForward
            | _ -> NeedsRealMerge

    /// <summary>
    /// Performs a merge of the given branch with the current branch
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="flags">Additional flags.</param>
    /// <param name="branch">The branch we want to merge in.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// merge @"C:\code\Fake" NoFastForwardFlag "master"
    /// </code>
    /// </example>   
    let merge repositoryDir flags branch =
        sprintf "merge %s %s" flags branch |> CommandHelper.gitCommand repositoryDir
