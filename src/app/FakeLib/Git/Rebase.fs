/// Contains helper functions which allow to deal with git rebase.
module Fake.Git.Rebase

open Fake

/// Performs a rebase on top of the given branch with the current branch
let start repositoryDir onTopOfBranch =
    try
        sprintf "rebase %s" onTopOfBranch
            |> gitCommand repositoryDir
    with
    | _ -> failwithf "Rebaseing on %s failed." onTopOfBranch

/// Restore the original branch and abort the rebase operation. 
let abort repositoryDir = gitCommand repositoryDir "rebase --abort"

/// Restart the rebasing process after having resolved a merge conflict. 
let continueRebase repositoryDir = gitCommand repositoryDir "rebase --continue"

/// Restart the rebasing process by skipping the current patch. 
let skip repositoryDir = gitCommand repositoryDir "rebase --skip"

/// rebase failed ==> fallback on merge
/// [omit]
let rollBackAndUseMerge repositoryDir onTopOfBranch =    
    abort repositoryDir
    merge repositoryDir "" onTopOfBranch
    true

/// Tries to rebase on top of the given branch.
/// If the rebasing process fails a normal merge will be started.
/// Returns if the process used merge instead of rebase.
let rebaseOrFallbackOnMerge repositoryDir onTopOfBranch =
    try
        start repositoryDir onTopOfBranch
        if not (isInTheMiddleOfConflictedMerge repositoryDir) &&
            not (isInTheMiddleOfRebase repositoryDir) then false else
        rollBackAndUseMerge repositoryDir onTopOfBranch
    with
    | _ -> rollBackAndUseMerge repositoryDir onTopOfBranch