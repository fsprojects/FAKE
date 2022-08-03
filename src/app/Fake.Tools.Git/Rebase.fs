namespace Fake.Tools.Git

/// Contains helper functions which allow to deal with git rebase.
[<RequireQualifiedAccess>]
module Rebase =

    /// Performs a rebase on top of the given branch with the current branch
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `onTopOfBranch` - The branch name to use for rebase
    let start repositoryDir onTopOfBranch =
        try
            sprintf "rebase %s" onTopOfBranch |> CommandHelper.gitCommand repositoryDir
        with _ ->
            failwithf "Rebaseing on %s failed." onTopOfBranch

    /// Restore the original branch and abort the rebase operation.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let abort repositoryDir =
        CommandHelper.gitCommand repositoryDir "rebase --abort"

    /// Restart the rebasing process after having resolved a merge conflict.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let continueRebase repositoryDir =
        CommandHelper.gitCommand repositoryDir "rebase --continue"

    /// Restart the rebasing process by skipping the current patch.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let skip repositoryDir =
        CommandHelper.gitCommand repositoryDir "rebase --skip"

    /// rebase failed ==> fallback on merge
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `onTopOfBranch` - The branch name to use for rebase
    let rollBackAndUseMerge repositoryDir onTopOfBranch =
        abort repositoryDir
        Merge.merge repositoryDir "" onTopOfBranch
        true

    /// Tries to rebase on top of the given branch.
    /// If the rebasing process fails a normal merge will be started.
    /// Returns if the process used merge instead of rebase.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `onTopOfBranch` - The branch name to use for rebase
    let rebaseOrFallbackOnMerge repositoryDir onTopOfBranch =
        try
            start repositoryDir onTopOfBranch

            if
                not (FileStatus.isInTheMiddleOfConflictedMerge repositoryDir)
                && not (FileStatus.isInTheMiddleOfRebase repositoryDir)
            then
                false
            else
                rollBackAndUseMerge repositoryDir onTopOfBranch
        with _ ->
            rollBackAndUseMerge repositoryDir onTopOfBranch
