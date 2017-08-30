[<AutoOpen>]
/// Contains helper functions which can be used to retrieve status information from git.
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
module Fake.Git.Information
#nowarn "44"

open Fake
open System
open System.Text.RegularExpressions
open System.IO
   
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let versionRegex = Regex("^git version ([\d.]*).*$", RegexOptions.Compiled)

/// Gets the git version
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let getVersion repositoryDir = 
    let ok,msg,errors = runGitCommand repositoryDir "--version"
    msg |> separated ""
    
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let isVersionHigherOrEqual currentVersion referenceVersion = 
  
    parseVersion currentVersion >= parseVersion referenceVersion

/// [omit]       
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let extractGitVersion version =
    let regexRes = versionRegex.Match version 
    if regexRes.Success then
        regexRes.Groups.[1].Value
    else
        failwith "unable to find git version"
         
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let isGitVersionHigherOrEqual referenceVersion = 

    let versionParts = getVersion "." |> extractGitVersion

    isVersionHigherOrEqual versionParts referenceVersion

/// Gets the git branch name
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let getBranchName repositoryDir =
    try
        let ok,msg,errors = runGitCommand repositoryDir "status -s -b"
        let s = msg |> Seq.head
        
        let replaceNoBranchString = "## HEAD ("
        let noBranch = "NoBranch"

        if startsWith replaceNoBranchString s 
            then noBranch
            else match s.Contains("...") with
                    | true  -> s.Substring(3,s.IndexOf("...")-3)
                    | false -> s.Substring(3)
    with _ when (repositoryDir = "" || repositoryDir = ".") && buildServer = TeamFoundation ->
        match environVarOrNone "BUILD_SOURCEBRANCHNAME" with
        | None -> reraise()
        | Some s -> s

/// Returns the SHA1 of the current HEAD
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let getCurrentSHA1 repositoryDir =
    try
        getSHA1 repositoryDir "HEAD"
    with _ when (repositoryDir = "" || repositoryDir = ".") && buildServer = TeamFoundation ->
        match environVarOrNone "BUILD_SOURCEVERSION" with
        | None -> reraise()
        | Some s -> s

/// Shows the git status
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let showStatus repositoryDir = showGitCommand repositoryDir "status"

/// Checks if the working copy is clean
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let isCleanWorkingCopy repositoryDir =
    let ok,msg,errors = runGitCommand repositoryDir "status"
    msg |> Seq.fold (fun acc s -> acc || "nothing to commit" <* s) false

/// Returns a friendly name from a SHA1
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let showName repositoryDir sha1 =
    let ok,msg,errors = runGitCommand repositoryDir <| sprintf "name-rev %s" sha1
    if msg.Count = 0 then sha1 else msg.[0] 

/// Returns true if rev1 is ahead of rev2
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let isAheadOf repositoryDir rev1 rev2 = 
    if rev1 = rev2 then false else
    findMergeBase repositoryDir rev1 rev2 = rev2

/// Gets the last git tag by calling git describe
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let describe repositoryDir =
    let _,msg,error = runGitCommand repositoryDir "describe"
    if error <> "" then failwithf "git describe failed: %s" error
    msg |> Seq.head

/// Gets the git log in one line
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let shortlog repositoryDir =
    let _,msg,error = runGitCommand repositoryDir "log --oneline -1"
    if error <> "" then failwithf "git log --oneline failed: %s" error
    msg |> Seq.head

/// Gets the last git tag of the current repository by calling git describe
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let getLastTag() = (describe "").Split('-') |> Seq.head

/// Gets the current hash of the current repository
[<System.Obsolete("Use Fake.Tools.Git.Information instead")>]
let getCurrentHash() =
    try
        let tmp =
            (shortlog "").Split(' ')
            |> Seq.head
            |> fun s -> s.Split('m')
        if tmp |> Array.length > 2 then tmp.[1].Substring(0,6) else tmp.[0].Substring(0,6)
    with _ when buildServer = TeamFoundation ->
        match environVarOrNone "BUILD_SOURCEVERSION" with
        | None -> reraise()
        | Some s -> s