/// Contains helper functions which allow to deal with git rebase.
[<System.Obsolete("Use Fake.Tools.Git.Rebase instead")>]
module Fake.Git.Rebase

#nowarn "44"
open Fake

/// Performs a rebase on top of the given branch with the current branch
[<System.Obsolete("Use Fake.Tools.Git.Rebase instead")>]
let start repositoryDir onTopOfBranch =
    try
        sprintf "rebase %s" onTopOfBranch
            |> gitCommand repositoryDir
    with
    | _ -> failwithf "Rebaseing on %s failed." onTopOfBranch

/// Restore the original branch and abort the rebase operation. 
[<System.Obsolete("Use Fake.Tools.Git.Rebase instead")>]
let abort repositoryDir = gitCommand repositoryDir "rebase --abort"

/// Restart the rebasing process after having resolved a merge conflict. 
[<System.Obsolete("Use Fake.Tools.Git.Rebase instead")>]
let continueRebase repositoryDir = gitCommand repositoryDir "rebase --continue"

/// Restart the rebasing process by skipping the current patch. 
[<System.Obsolete("Use Fake.Tools.Git.Rebase instead")>]
let skip repositoryDir = gitCommand repositoryDir "rebase --skip"

/// rebase failed ==> fallback on merge
/// [omit]
[<System.Obsolete("Use Fake.Tools.Git.Rebase instead")>]
let rollBackAndUseMerge repositoryDir onTopOfBranch =    
    abort repositoryDir
    merge repositoryDir "" onTopOfBranch
    true

/// Tries to rebase on top of the given branch.
/// If the rebasing process fails a normal merge will be started.
/// Returns if the process used merge instead of rebase.
[<System.Obsolete("Use Fake.Tools.Git.Rebase instead")>]
let rebaseOrFallbackOnMerge repositoryDir onTopOfBranch =
    try
        start repositoryDir onTopOfBranch
        if not (isInTheMiddleOfConflictedMerge repositoryDir) &&
            not (isInTheMiddleOfRebase repositoryDir) then false else
        rollBackAndUseMerge repositoryDir onTopOfBranch
    with
    | _ -> rollBackAndUseMerge repositoryDir onTopOfBranch