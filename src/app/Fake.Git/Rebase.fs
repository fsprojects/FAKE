module Fake.Git.Rebase

open Fake

/// Performs a rebase on top of the given branch with the current branch
let start repositoryDir onTopOfBranch =
    sprintf "rebase %s" onTopOfBranch
      |> gitCommand repositoryDir

/// Restore the original branch and abort the rebase operation. 
let abort repositoryDir = gitCommand repositoryDir "rebase --abort"

/// Restart the rebasing process after having resolved a merge conflict. 
let continueRebase repositoryDir = gitCommand repositoryDir "rebase --continue"

/// Restart the rebasing process by skipping the current patch. 
let skip repositoryDir = gitCommand repositoryDir "rebase --skip"

/// <summary>
/// Tries to rebase on top of the given branch.
/// If the rebasing process fails a normal merge will be started.
/// </summary>
/// <returns>If the process used merge instead of rebase.</returns>
let rebaseOrFallbackOnMerge repositoryDir baseBranch =
    start repositoryDir baseBranch
    if not (isInTheMiddleOfConflictedMerge repositoryDir) &&
        not (isInTheMiddleOfRebase repositoryDir)
    then false else

    // rebase failed ==> fallback on merge
    abort repositoryDir
    merge repositoryDir "" baseBranch
    true