[<AutoOpen>]
/// Contains helper functions which allow to deal with git merge.
module Fake.Git.Merge

open Fake
open System.IO

/// Gets the current merge message.
let getMergeMessage repositoryDir =
    let file = (findGitDir repositoryDir).FullName + "\\MERGE_MSG"
    if File.Exists file then File.ReadAllText file else ""

/// Allows git to use fast-forward merges
let FastForwardFlag = "--ff"

/// Forbids git to use fast-forward merges
let NoFastForwardFlag = "--no-ff"

/// Git merge option.
type MergeType =
| SameCommit
| FirstNeedsFastForward
| SecondNeedsFastForward
| NeedsRealMerge

/// Tests whether branches and their "origin" counterparts have diverged and need merging first.
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `local` - The local branch name.
///  - `remote` - The remote branch name.
let compareBranches repositoryDir local remote =
    let commit1 = getSHA1 repositoryDir local
    let commit2 = getSHA1 repositoryDir remote
    if commit1 = commit2 then SameCommit else
    match findMergeBase repositoryDir commit1 commit2 with
    | x when x = commit1 -> FirstNeedsFastForward
    | x when x = commit2 -> SecondNeedsFastForward
    | _  -> NeedsRealMerge

/// Performs a merge of the given branch with the current branch
/// ## Parameters
///
///  - `repositoryDir` - The git repository.
///  - `flags` - Additional flags.
///  - `branch` - The branch we want to merge in.
/// ## Sample
///
///     merge @"C:\code\Fake" NoFastForwardFlag "master"
let merge repositoryDir flags branch =
    sprintf "merge %s %s" flags branch
      |> gitCommand repositoryDir