namespace Fake.Tools.Git

open Fake.Core
open Fake.Core.String.Operators
open System.Text.RegularExpressions

/// <summary>
/// Contains helper functions which can be used to retrieve status information from git.
/// </summary>
[<RequireQualifiedAccess>]
module Information =

    let internal versionRegex = Regex("^git version ([\d.]*).*$", RegexOptions.Compiled)

    /// <summary>
    /// Gets the git version
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
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

    /// <summary>
    /// Check if the given reference version is higher or equal to found git version
    /// </summary>
    ///
    /// <param name="referenceVersion">The version to compare with</param>
    let isGitVersionHigherOrEqual referenceVersion =
        let versionParts = getVersion "." |> extractGitVersion

        versionParts > SemVer.parse referenceVersion

    /// <summary>
    /// Gets the git branch name
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
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

    /// <summary>
    /// Returns the SHA1 of the current <c>HEAD</c>
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let getCurrentSHA1 repositoryDir =
        try
            Branches.getSHA1 repositoryDir "HEAD"
        with _ when
            (repositoryDir = "" || repositoryDir = ".")
            && BuildServer.buildServer = TeamFoundation ->
            match Environment.environVarOrNone "BUILD_SOURCEVERSION" with
            | None -> reraise ()
            | Some s -> s

    /// <summary>
    /// Shows the git status
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let showStatus repositoryDir = CommandHelper.showGitCommand repositoryDir "status"

    /// <summary>
    /// Checks if the working copy is clean
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let isCleanWorkingCopy repositoryDir =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir "status"
        msg |> Seq.fold (fun acc s -> acc || "nothing to commit" <* s) false

    /// <summary>
    /// Returns a friendly name from a SHA1
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="sha1">The sha1 to use</param>
    let showName repositoryDir sha1 =
        let _, msg, _ = CommandHelper.runGitCommand repositoryDir <| sprintf "name-rev %s" sha1
        if msg.Length = 0 then sha1 else msg[0]

    /// <summary>
    /// Returns true if rev1 is ahead of rev2
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    /// <param name="rev1">The first revision to use</param>
    /// <param name="rev2">The second revision to use</param>
    let isAheadOf repositoryDir rev1 rev2 =
        if rev1 = rev2 then
            false
        else
            Branches.findMergeBase repositoryDir rev1 rev2 = rev2

    /// <summary>
    /// Gets the last git tag by calling git describe
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let describe repositoryDir =
        let _, msg, error = CommandHelper.runGitCommand repositoryDir "describe"

        if error <> "" then
            failwithf "git describe failed: %s" error

        msg |> Seq.head

    /// <summary>
    /// Gets the git log in one line
    /// </summary>
    ///
    /// <param name="repositoryDir">The git repository.</param>
    let shortlog repositoryDir =
        let _, msg, error = CommandHelper.runGitCommand repositoryDir "log --oneline -1"

        if error <> "" then
            failwithf "git log --oneline failed: %s" error

        msg |> Seq.head

    /// <summary>
    /// Gets the last git tag of the current repository by calling git describe
    /// </summary>
    let getLastTag () =
        let _,msg,error = CommandHelper.runGitCommand "" "describe --tags --abbrev=0"
        if error <> "" then failwithf "git describe --tags failed: %s" error
        msg |> Seq.head

    /// <summary>
    /// Gets the current hash of the current repository
    /// </summary>
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
