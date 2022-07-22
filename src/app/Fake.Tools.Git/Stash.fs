namespace Fake.Tools.Git

/// Contains helper functions which allow to deal with git stash.
[<RequireQualifiedAccess>]
module Stash =

    /// Stash the changes in a dirty working directory away.
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `message` - The stash message
    let push repositoryDir message =
        sprintf "stash save %s" message |> CommandHelper.gitCommand repositoryDir

    /// Remove a single stashed state from the stash list and
    /// apply it on top of the current working tree state,
    /// i.e., do the inverse operation of git stash save.
    /// The working directory must match the index.
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let pop repositoryDir = CommandHelper.gitCommand repositoryDir "stash pop"
