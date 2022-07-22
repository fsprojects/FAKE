namespace Fake.Tools.Git

open Fake.Tools.Git
open System.IO

/// Contains helper functions which can be used to retrieve file status information from git.
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

    /// Gets the changed files between the given revisions
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `revision1` - The first revision to use.
    ///  - `revision2` - The second revision to use.
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

    /// Gets all changed files in the current revision
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let getAllFiles repositoryDir =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir <| sprintf "ls-files"
        msg |> Seq.map (fun line -> Added, line)

    /// Gets the changed files since the given revision incl. changes in the working copy
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `revision` - The revision to use.
    let getChangedFilesInWorkingCopy repositoryDir revision =
        getChangedFiles repositoryDir revision ""

    /// Gets all conflicted files
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let getConflictedFiles repositoryDir =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir "ls-files --unmerged"

        msg
        |> Seq.map (fun file -> file.LastIndexOfAny([| ' '; '\t' |]), file)
        |> Seq.filter (fun (index, _) -> index > 0)
        |> Seq.map (fun (index, file) -> file.Substring(index + 1))
        |> Seq.toList

    /// Returns true if the working copy is in a conflicted merge otherwise false
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let isInTheMiddleOfConflictedMerge repositoryDir = [] <> getConflictedFiles repositoryDir

    /// Returns the current rebase directory for the given repository.
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let getRebaseDir (repositoryDir: string) =
        if Directory.Exists(repositoryDir + ".git\\rebase-apply\\") then
            repositoryDir + ".git\\rebase-apply\\"
        elif Directory.Exists(repositoryDir + ".git\\rebase\\") then
            repositoryDir + ".git\\rebase\\"
        else
            ""

    /// Returns true if the given repository is in the middle of a rebase process.
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let isInTheMiddleOfRebase repositoryDir =
        let rebaseDir = getRebaseDir repositoryDir
        Directory.Exists rebaseDir && (not <| File.Exists(rebaseDir + "applying"))

    /// Returns true if the given repository is in the middle of a patch process.
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let isInTheMiddleOfPatch repositoryDir =
        let rebaseDir = getRebaseDir repositoryDir
        Directory.Exists rebaseDir && (not <| File.Exists(rebaseDir + "rebasing"))

    /// Cleans the working copy by doing a git reset --hard and a clean -f.
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let cleanWorkingCopy repositoryDir =
        Reset.ResetHard repositoryDir
        CommandHelper.showGitCommand repositoryDir "clean -f"
