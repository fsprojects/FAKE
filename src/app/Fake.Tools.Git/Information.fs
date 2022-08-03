namespace Fake.Tools.Git

open Fake.Core
open Fake.Core.String.Operators
open System.Text.RegularExpressions

/// Contains helper functions which can be used to retrieve status information from git.
[<RequireQualifiedAccess>]
module Information =

    let internal versionRegex = Regex("^git version ([\d.]*).*$", RegexOptions.Compiled)

    /// Gets the git version
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let getVersion repositoryDir =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir "--version"
        msg |> String.separated ""

    /// [omit]
    let extractGitVersion version =
        let regexRes = versionRegex.Match version

        if regexRes.Success then
            SemVer.parse regexRes.Groups[1].Value
        else
            failwith "unable to find git version"

    /// Check if the given reference version is higher or equal to found git version
    ///
    /// ## Parameters
    ///  - `referenceVersion` - The version to compare with
    let isGitVersionHigherOrEqual referenceVersion =
        let versionParts = getVersion "." |> extractGitVersion

        versionParts > SemVer.parse referenceVersion

    /// Gets the git branch name
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let getBranchName repositoryDir =
        try
            let _, msg, _ = CommandHelper.runGitCommand repositoryDir "status -s -b"
            let s = msg |> Seq.head

            let replaceNoBranchString = "## HEAD ("
            let noBranch = "NoBranch"

            if String.startsWith replaceNoBranchString s then
                noBranch
            else
                match s.Contains("...") with
                | true -> s.Substring(3, s.IndexOf("...") - 3)
                | false -> s.Substring(3)
        with _ when
            (repositoryDir = "" || repositoryDir = ".")
            && BuildServer.buildServer = TeamFoundation ->
            match Environment.environVarOrNone "BUILD_SOURCEBRANCHNAME" with
            | None -> reraise ()
            | Some s -> s

    /// Returns the SHA1 of the current HEAD
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let getCurrentSHA1 repositoryDir =
        try
            Branches.getSHA1 repositoryDir "HEAD"
        with _ when
            (repositoryDir = "" || repositoryDir = ".")
            && BuildServer.buildServer = TeamFoundation ->
            match Environment.environVarOrNone "BUILD_SOURCEVERSION" with
            | None -> reraise ()
            | Some s -> s

    /// Shows the git status
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let showStatus repositoryDir = CommandHelper.showGitCommand repositoryDir "status"

    /// Checks if the working copy is clean
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let isCleanWorkingCopy repositoryDir =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir "status"
        msg |> Seq.fold (fun acc s -> acc || "nothing to commit" <* s) false

    /// Returns a friendly name from a SHA1
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `sha1` - The sha1 to use
    let showName repositoryDir sha1 =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir <| sprintf "name-rev %s" sha1
        if msg.Length = 0 then sha1 else msg[0]

    /// Returns true if rev1 is ahead of rev2
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    ///  - `rev1` - The first revision to use
    ///  - `rev2` - The second revision to use
    let isAheadOf repositoryDir rev1 rev2 =
        if rev1 = rev2 then
            false
        else
            Branches.findMergeBase repositoryDir rev1 rev2 = rev2

    /// Gets the last git tag by calling git describe
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let describe repositoryDir =
        let _, msg, error = CommandHelper.runGitCommand repositoryDir "describe"

        if error <> "" then
            failwithf "git describe failed: %s" error

        msg |> Seq.head

    /// Gets the git log in one line
    ///
    /// ## Parameters
    ///  - `repositoryDir` - The git repository.
    let shortlog repositoryDir =
        let _, msg, error = CommandHelper.runGitCommand repositoryDir "log --oneline -1"

        if error <> "" then
            failwithf "git log --oneline failed: %s" error

        msg |> Seq.head

    /// Gets the last git tag of the current repository by calling git describe
    let getLastTag () =
        let _,msg,error = CommandHelper.runGitCommand "" "describe --tags --abbrev=0"
        if error <> "" then failwithf "git describe --tags failed: %s" error
        msg |> Seq.head

    /// Gets the current hash of the current repository
    let getCurrentHash () =
        try
            let tmp = (shortlog "").Split(' ') |> Seq.head |> (fun s -> s.Split('m'))

            if tmp |> Array.length > 2 then
                tmp[1].Substring(0, 6)
            else
                tmp[0].Substring(0, 6)
        with _ when BuildServer.buildServer = TeamFoundation ->
            match Environment.environVarOrNone "BUILD_SOURCEVERSION" with
            | None -> reraise ()
            | Some s -> s
