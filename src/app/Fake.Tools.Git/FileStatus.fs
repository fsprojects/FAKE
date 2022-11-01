namespace Fake.Tools.Git

open Fake.Tools.Git
open System.IO

/// <summary>
/// Contains helper functions which can be used to retrieve file status information from git.
/// </summary>
[<RequireQualifiedAccess>]
module FileStatus =

    /// A type which represents a file status in git.
    type FileStatus =
        | Added
        | Copied
        | Deleted
        | Modified
        | Renamed
        | TypeChange

        static member Parse =
            function
            | "A" -> Added
            | c when c.StartsWith "C" -> Copied
            | "D" -> Deleted
            | m when m.StartsWith "M" -> Modified
            | r when r.StartsWith "R" -> Renamed
            | "T" -> TypeChange
            | _ -> Modified

    /// <summary>
    /// Gets the changed files between the given revisions
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="revision1">The first revision to use.</param>
    /// <param name="revision2">The second revision to use.</param>
    let getChangedFiles repositoryDir revision1 revision2 =
        SanityChecks.checkRevisionExists repositoryDir revision1

        if revision2 <> "" then
            SanityChecks.checkRevisionExists repositoryDir revision2

        let _, msg, _ =
            CommandHelper.runGitCommand repositoryDir
            <| sprintf "diff %s %s --name-status" revision1 revision2

        msg
        |> Seq.map (fun line ->
            let a = line.Split('\t')
            FileStatus.Parse a[0], a[1])

    /// <summary>
    /// Gets all changed files in the current revision
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let getAllFiles repositoryDir =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir <| sprintf "ls-files"
        msg |> Seq.map (fun line -> Added, line)

    /// <summary>
    /// Gets the changed files since the given revision incl. changes in the working copy
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="revision">The revision to use.</param>
    let getChangedFilesInWorkingCopy repositoryDir revision =
        getChangedFiles repositoryDir revision ""

    /// <summary>
    /// Gets all conflicted files
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let getConflictedFiles repositoryDir =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir "ls-files --unmerged"

        msg
        |> Seq.map (fun file -> file.LastIndexOfAny([| ' '; '\t' |]), file)
        |> Seq.filter (fun (index, _) -> index > 0)
        |> Seq.map (fun (index, file) -> file.Substring(index + 1))
        |> Seq.toList

    /// <summary>
    /// Returns true if the working copy is in a conflicted merge otherwise false
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let isInTheMiddleOfConflictedMerge repositoryDir = [] <> getConflictedFiles repositoryDir

    /// <summary>
    /// Returns the current rebase directory for the given repository.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let getRebaseDir (repositoryDir: string) =
        if Directory.Exists(repositoryDir + ".git\\rebase-apply\\") then
            repositoryDir + ".git\\rebase-apply\\"
        elif Directory.Exists(repositoryDir + ".git\\rebase\\") then
            repositoryDir + ".git\\rebase\\"
        else
            ""

    /// <summary>
    /// Returns true if the given repository is in the middle of a rebase process.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let isInTheMiddleOfRebase repositoryDir =
        let rebaseDir = getRebaseDir repositoryDir
        Directory.Exists rebaseDir && (not <| File.Exists(rebaseDir + "applying"))

    /// <summary>
    /// Returns true if the given repository is in the middle of a patch process.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let isInTheMiddleOfPatch repositoryDir =
        let rebaseDir = getRebaseDir repositoryDir
        Directory.Exists rebaseDir && (not <| File.Exists(rebaseDir + "rebasing"))

    /// <summary>
    /// Cleans the working copy by doing a git reset --hard and a clean -f.
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let cleanWorkingCopy repositoryDir =
        Reset.ResetHard repositoryDir
        CommandHelper.showGitCommand repositoryDir "clean -f"
