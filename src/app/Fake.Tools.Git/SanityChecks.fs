namespace Fake.Tools.Git

/// Contains helper function which can be used for sanity checks.
[<RequireQualifiedAccess>]
module SanityChecks =

    /// Checks if the given branch exists.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `revision1` - The revision to check for.
    let checkRevisionExists repositoryDir revision1 =
        let ok1, _, errors1 = CommandHelper.runGitCommand repositoryDir <| sprintf "log %s" revision1

        if not ok1 || errors1 <> "" then
            failwithf "Revision %s is not found in the current repository." revision1


    /// Checks if the given branch exists.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `branch` - The branch name to check
    let checkIfBranchExists repositoryDir branch =
        if not (Branches.getAllBranches repositoryDir |> List.exists ((=) branch)) then
            failwithf "Branch %s doesn't exists." branch

    /// Checks if the given branch is absent.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `branch` - The branch name to check
    let checkIfBranchIsAbsent repositoryDir branch =
        if Branches.getAllBranches repositoryDir |> List.exists ((=) branch) then
            failwithf "Branch %s exists but should be absent." branch

    /// Checks if the given branch is a local branch.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `branch` - The branch name to check
    let checkIsLocalBranch repositoryDir branch =
        if not (Branches.getLocalBranches repositoryDir |> List.exists ((=) branch)) then
            failwithf "Branch %s doesn't exists locally." branch

    /// Checks if the given branch is a remote branch.
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `branch` - The branch name to check
    let checkIsRemoteBranch repositoryDir branch =
        if not (Branches.getRemoteBranches repositoryDir |> List.exists ((=) branch)) then
            failwithf "Branch %s doesn't exists remotely." branch
