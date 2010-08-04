[<AutoOpen>]
module Fake.Git.Merge

open Fake
open System.IO

/// Gets the current merge message
let getMergeMessage repositoryDir =
    let file = (findGitDir repositoryDir).FullName + "\\MERGE_MSG"
    if File.Exists file then File.ReadAllText file else ""

let FastForwardFlag = "--ff"

let NoFastForwardFlag = "--no-ff"

type MergeType =
| SameCommit
| FirstNeedsFastForward
| SecondNeedsFastForward
| NeedsRealMerge

/// <summary>
/// Tests whether branches and their "origin" counterparts have diverged and need
/// merging first.
/// </summary>
///
/// <param name="repositoryDir">The path to the repository.</param>
/// <param name="local">The local branch name.</param>
/// <param name="remote">The remote branch name.</param>
let compareBranches repositoryDir local remote =
    let commit1 = getSHA1 repositoryDir local
    let commit2 = getSHA1 repositoryDir remote
    if commit1 = commit2 then SameCommit else
    match findMergeBase repositoryDir commit1 commit2 with
    | x when x = commit1 -> FirstNeedsFastForward
    | x when x = commit2 -> SecondNeedsFastForward
    | _  -> NeedsRealMerge

/// Performs a merge of the given branch with the current branch
let merge repositoryDir flags branch =
    sprintf "merge %s %s" flags branch
      |> gitCommand repositoryDir