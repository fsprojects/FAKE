namespace Fake.Tools.Git

open System

/// <summary>
/// Contains helper functions which allow to deal with git reset.
/// </summary>
[<RequireQualifiedAccess>]
module Reset =

    let internal addArgs commit file =
        sprintf
            "%s%s"
            (if String.IsNullOrEmpty commit then
                 ""
             else
                 " \"" + commit + "\"")
            (if String.IsNullOrEmpty file then
                 ""
             else
                 " -- \"" + file + "\"")

    /// <summary>
    /// the intent of the 'reset' helper is to either set a repo to a certain point, or set a file to a certain point.
    /// Git reset doesn't take file paths in the hard/mixed/soft modes, and so you have to use checkout instead for that.
    /// This function encapsulates caring about that so you don't have to.
    /// </summary>
    let internal resetOrCheckout file mode =
        match file |> String.IsNullOrEmpty with
        | true -> sprintf "reset --%s" mode
        | false -> "checkout"

    /// <summary>
    /// Performs a git reset "soft".
    /// Does not touch the index file nor the working tree at all.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="commit">The commit to which git should perform the reset.</param>
    /// <param name="file">The file to reset - null means all files.</param>
    let soft repositoryDir commit file =
        resetOrCheckout file "soft" + addArgs commit file |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Performs a git reset "mixed".
    /// Resets the index but not the working tree and reports what has not been updated.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="commit">The commit to which git should perform the reset.</param>
    /// <param name="file">The file to reset - null means all files.</param>
    let mixed repositoryDir commit file =
        resetOrCheckout file "mixed" + addArgs commit file |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Performs a git reset "hard".
    /// Resets the index and working tree. Any changes to tracked files in the working tree since
    /// <c>&lt;commit&gt;</c> are discarded.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="commit">The commit to which git should perform the reset.</param>
    /// <param name="file">The file to reset - null means all files.</param>
    let hard repositoryDir commit file =
        resetOrCheckout file "hard" + addArgs commit file |> CommandHelper.gitCommand repositoryDir

    /// <summary>
    /// Performs a git reset "soft" to the current HEAD.
    /// Does not touch the index file nor the working tree at all.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let ResetSoft repositoryDir = soft repositoryDir null null

    /// <summary>
    /// Performs a git reset "mixed" to the current HEAD.
    /// Resets the index but not the working tree and reports what has not been updated.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let ResetMixed repositoryDir = mixed repositoryDir null null

    /// <summary>
    /// Performs a git reset "hard" to the current HEAD.
    /// Resets the index and working tree. Any changes to tracked files in the working tree since
    /// <c>&lt;commit&gt;</c> are discarded.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let ResetHard repositoryDir = hard repositoryDir null null
