/// Contains helper functions which can be used to retrieve status information from git.
module Fake.Tools.Git.Information

open Fake.Tools.Git.CommandHelper
open Fake.Tools.Git.Branches
open Fake.Core
open Fake.Core.String.Operators
open System
open System.Text.RegularExpressions
open System.IO

let internal versionRegex = Regex("^git version ([\d.]*).*$", RegexOptions.Compiled)

/// Gets the git version
let getVersion repositoryDir =
    let ok,msg,errors = runGitCommand repositoryDir "--version"
    msg |> String.separated ""

/// [omit]
let extractGitVersion version =
    let regexRes = versionRegex.Match version
    if regexRes.Success then
        SemVer.parse regexRes.Groups.[1].Value
    else
        failwith "unable to find git version"

let isGitVersionHigherOrEqual referenceVersion =
    let versionParts = getVersion "." |> extractGitVersion

    versionParts > SemVer.parse referenceVersion

/// Gets the git branch name
let getBranchName repositoryDir =
    try
        let ok,msg,errors = runGitCommand repositoryDir "status -s -b"
        let s = msg |> Seq.head

        let replaceNoBranchString = "## HEAD ("
        let noBranch = "NoBranch"

        if String.startsWith replaceNoBranchString s
            then noBranch
            else match s.Contains("...") with
                    | true  -> s.Substring(3,s.IndexOf("...")-3)
                    | false -> s.Substring(3)
    with _ when (repositoryDir = "" || repositoryDir = ".") && BuildServer.buildServer = TeamFoundation ->
        match Environment.environVarOrNone "BUILD_SOURCEBRANCHNAME" with
        | None -> reraise()
        | Some s -> s

/// Returns the SHA1 of the current HEAD
let getCurrentSHA1 repositoryDir =
    try
        getSHA1 repositoryDir "HEAD"
    with _ when (repositoryDir = "" || repositoryDir = ".") && BuildServer.buildServer = TeamFoundation ->
        match Environment.environVarOrNone "BUILD_SOURCEVERSION" with
        | None -> reraise()
        | Some s -> s

/// Shows the git status
let showStatus repositoryDir = showGitCommand repositoryDir "status"

/// Checks if the working copy is clean
let isCleanWorkingCopy repositoryDir =
    let ok,msg,errors = runGitCommand repositoryDir "status"
    msg |> Seq.fold (fun acc s -> acc || "nothing to commit" <* s) false

/// Returns a friendly name from a SHA1
let showName repositoryDir sha1 =
    let ok,msg,errors = runGitCommand repositoryDir <| sprintf "name-rev %s" sha1
    if msg.Length = 0 then sha1 else msg.[0]

/// Returns true if rev1 is ahead of rev2
let isAheadOf repositoryDir rev1 rev2 =
    if rev1 = rev2 then false else
    findMergeBase repositoryDir rev1 rev2 = rev2

/// Gets the last git tag by calling git describe
let describe repositoryDir =
    let _,msg,error = runGitCommand repositoryDir "describe"
    if error <> "" then failwithf "git describe failed: %s" error
    msg |> Seq.head

/// Gets the git log in one line
let shortlog repositoryDir =
    let _,msg,error = runGitCommand repositoryDir "log --oneline -1"
    if error <> "" then failwithf "git log --oneline failed: %s" error
    msg |> Seq.head

/// Gets the last git tag of the current repository by calling git describe
let getLastTag() = (describe "").Split('-') |> Seq.head

/// Gets the current hash of the current repository
let getCurrentHash() =
    try
        let tmp =
            (shortlog "").Split(' ')
            |> Seq.head
            |> fun s -> s.Split('m')
        if tmp |> Array.length > 2 then tmp.[1].Substring(0,6) else tmp.[0].Substring(0,6)
    with _ when BuildServer.buildServer = TeamFoundation ->
        match Environment.environVarOrNone "BUILD_SOURCEVERSION" with
        | None -> reraise()
        | Some s -> s