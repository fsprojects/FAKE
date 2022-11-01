namespace Fake.Tools.Git

/// <summary>
/// Contains helper functions which allow to deal with git stash.
/// </summary>
[<RequireQualifiedAccess>]
module Stash =

    /// <summary>
    /// Stash the changes in a dirty working directory away.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="message">The stash message</param>
    let push repositoryDir message =
        sprintf "stash save %s" message |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Remove a single stashed state from the stash list and
    /// apply it on top of the current working tree state,
    /// i.e., do the inverse operation of git stash save.
    /// The working directory must match the index.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let pop repositoryDir =
        CommandHelper.gitCommand repositoryDir "stash pop"
