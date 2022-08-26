namespace Fake.Tools.Git

/// <summary>
/// Contains helper functions which allow to deal with git rebase.
/// </summary>
[<RequireQualifiedAccess>]
module Rebase =

    /// <summary>
    /// Performs a rebase on top of the given branch with the current branch
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="onTopOfBranch">The branch name to use for rebase</param>
    let start repositoryDir onTopOfBranch =
        try
            sprintf "rebase %s" onTopOfBranch |> CommandHelper.gitCommand repositoryDir
        with _ ->
            failwithf "Rebaseing on %s failed." onTopOfBranch

    /// <summary>
    /// Restore the original branch and abort the rebase operation.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let abort repositoryDir =
        CommandHelper.gitCommand repositoryDir "rebase --abort"

    /// <summary>
    /// Restart the rebasing process after having resolved a merge conflict.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let continueRebase repositoryDir =
        CommandHelper.gitCommand repositoryDir "rebase --continue"

    /// <summary>
    /// Restart the rebasing process by skipping the current patch.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let skip repositoryDir =
        CommandHelper.gitCommand repositoryDir "rebase --skip"

    /// <summary>
    /// rebase failed ==> fallback on merge
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="onTopOfBranch">The branch name to use for rebase</param>
    let rollBackAndUseMerge repositoryDir onTopOfBranch =
        abort repositoryDir
        Merge.merge repositoryDir "" onTopOfBranch
        true

    /// <summary>
    /// Tries to rebase on top of the given branch.
    /// If the rebasing process fails a normal merge will be started.
    /// Returns if the process used merge instead of rebase.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="onTopOfBranch">The branch name to use for rebase</param>
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
