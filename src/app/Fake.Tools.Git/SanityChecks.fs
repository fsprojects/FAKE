namespace Fake.Tools.Git

/// <summary>
/// Contains helper function which can be used for sanity checks.
/// </summary>
[<RequireQualifiedAccess>]
module SanityChecks =

    /// <summary>
    /// Checks if the given branch exists.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="revision1">The revision to check for.</param>
    let checkRevisionExists repositoryDir revision1 =
        let ok1, _, errors1 = CommandHelper.runGitCommand repositoryDir <| sprintf "log %s" revision1

        if not ok1 || errors1 <> "" then
            failwithf "Revision %s is not found in the current repository." revision1


    /// <summary>
    /// Checks if the given branch exists.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="branch">The branch name to check</param>
    let checkIfBranchExists repositoryDir branch =
        if not (Branches.getAllBranches repositoryDir |> List.exists ((=) branch)) then
            failwithf "Branch %s doesn't exists." branch

    /// <summary>
    /// Checks if the given branch is absent.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="branch">The branch name to check</param>
    let checkIfBranchIsAbsent repositoryDir branch =
        if Branches.getAllBranches repositoryDir |> List.exists ((=) branch) then
            failwithf "Branch %s exists but should be absent." branch

    /// <summary>
    /// Checks if the given branch is a local branch.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="branch">The branch name to check</param>
    let checkIsLocalBranch repositoryDir branch =
        if not (Branches.getLocalBranches repositoryDir |> List.exists ((=) branch)) then
            failwithf "Branch %s doesn't exists locally." branch

    /// <summary>
    /// Checks if the given branch is a remote branch.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="branch">The branch name to check</param>
    let checkIsRemoteBranch repositoryDir branch =
        if not (Branches.getRemoteBranches repositoryDir |> List.exists ((=) branch)) then
            failwithf "Branch %s doesn't exists remotely." branch
