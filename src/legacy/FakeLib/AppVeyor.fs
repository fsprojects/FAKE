/// Contains code to configure FAKE for AppVeyor integration
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor instead")>]
module Fake.AppVeyor

open System
open System.IO

/// AppVeyor environment variables as [described](http://www.appveyor.com/docs/environment-variables)
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
type AppVeyorEnvironment =

    /// AppVeyor Build Agent API URL
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member ApiUrl = environVar "APPVEYOR_API_URL"

    /// AppVeyor Account Name
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member AccountName = environVar "APPVEYOR_ACCOUNT_NAME"

    /// AppVeyor unique project ID
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member ProjectId = environVar "APPVEYOR_PROJECT_ID"

    /// Project name
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member ProjectName = environVar "APPVEYOR_PROJECT_NAME"

    /// Project slug (as seen in project details URL)
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member ProjectSlug = environVar "APPVEYOR_PROJECT_SLUG"

    /// Path to clone directory
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member BuildFolder = environVar "APPVEYOR_BUILD_FOLDER"

    /// AppVeyor unique build ID
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member BuildId = environVar "APPVEYOR_BUILD_ID"

    /// Build number
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member BuildNumber = environVar "APPVEYOR_BUILD_NUMBER"

    /// Build version
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member BuildVersion = environVar "APPVEYOR_BUILD_VERSION"

    /// GitHub Pull Request number
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member PullRequestNumber = environVar "APPVEYOR_PULL_REQUEST_NUMBER"

    /// GitHub Pull Request title
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member PullRequestTitle = environVar "APPVEYOR_PULL_REQUEST_TITLE"

    /// GitHub Pull Request Repo name
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member PullRequestRepoName = environVar "APPVEYOR_PULL_REQUEST_REPO_NAME"

    /// GitHub Pull Request branch
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member PullRequestRepoBranch = environVar "APPVEYOR_PULL_REQUEST_REPO_BRANCH"

    /// AppVeyor unique job ID
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member JobId = environVar "APPVEYOR_JOB_ID"

    /// GitHub, BitBucket or Kiln
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoProvider = environVar "APPVEYOR_REPO_PROVIDER"

    /// git or mercurial
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoScm = environVar "APPVEYOR_REPO_SCM"

    /// Repository name in format owner-name/repo-name
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoName = environVar "APPVEYOR_REPO_NAME"

    /// Build branch
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoBranch = environVar "APPVEYOR_REPO_BRANCH"

    /// Commit ID (SHA)
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoCommit = environVar "APPVEYOR_REPO_COMMIT"

    /// Commit author's name
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoCommitAuthor = environVar "APPVEYOR_REPO_COMMIT_AUTHOR"

    /// Commit author's email address
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoCommitAuthorEmail = environVar "APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL"

    /// Commit date/time
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoCommitTimestamp = environVar "APPVEYOR_REPO_COMMIT_TIMESTAMP"

    /// Commit message
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoCommitMessage = environVar "APPVEYOR_REPO_COMMIT_MESSAGE"

    /// The rest of the commit message after line break (if exists)
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoCommitMessageExtended = environVar "APPVEYOR_REPO_COMMIT_MESSAGE_EXTENDED"

    /// If the build runs by scheduler;
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member IsScheduledBuild = environVar "APPVEYOR_SCHEDULED_BUILD"

    /// If the build has been started by the "New Build" button or from the same API
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member IsForcedBuild = environVar "APPVEYOR_FORCED_BUILD"

    /// If the build has been started by the "Re-Build commit/PR" button or from the same API
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member IsReBuild = environVar "APPVEYOR_RE_BUILD"

    /// true if build has started by pushed tag; otherwise false
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoTag =
        let rt = environVar "APPVEYOR_REPO_TAG"
        rt <> null && rt.Equals("true", System.StringComparison.OrdinalIgnoreCase)

    /// contains tag name for builds started by tag
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepoTagName = environVar "APPVEYOR_REPO_TAG_NAME"

    /// Platform name set on Build tab of project settings (or through platform parameter in appveyor.yml).
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member Platform = environVar "PLATFORM"

    /// Configuration name set on Build tab of project settings (or through configuration parameter in appveyor.yml).
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member Configuration  = environVar "CONFIGURATION"

    /// The job name
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member JobName = environVar "APPVEYOR_JOB_NAME"
    
    /// The Job Number
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member JobNumber = environVar "APPVEYOR_JOB_NUMBER"
    
    /// set to true to disable cache restore
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member CacheSkipRestore = environVar "APPVEYOR_CACHE_SKIP_RESTORE"
    
    /// set to true to disable cache update
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member CacheSkipSave = environVar "APPVEYOR_CACHE_SKIP_SAVE"
    
    /// Current build worker image the build is running on, e.g. Visual Studio 2015
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member BuildWorkerImage = environVar "APPVEYOR_BUILD_WORKER_IMAGE"
    
    /// Artifact upload timeout in seconds. Default is 600 (10 minutes)
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member ArtifactUploadTimeout = environVar "APPVEYOR_ARTIFACT_UPLOAD_TIMEOUT"
    
    /// Timeout in seconds to download arbirtary files using appveyor DownloadFile command. Default is 300 (5 minutes)
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member FileDownloadTimeout = environVar "APPVEYOR_FILE_DOWNLOAD_TIMEOUT"
    
    /// Timeout in seconds to download repository (GitHub, Bitbucket or VSTS) as zip file (shallow clone). Default is 1800 (30 minutes)
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member RepositoryShallowCloneTimeout = environVar "APPVEYOR_REPOSITORY_SHALLOW_CLONE_TIMEOUT"
    
    /// Timeout in seconds to download or upload each cache entry. Default is 300 (5 minutes)
    [<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.Environment instead")>]
    static member CacheEntryUploadDownloadTimeout = environVar "APPVEYOR_CACHE_ENTRY_UPLOAD_DOWNLOAD_TIMEOUT"
    
let private sendToAppVeyor args =
    ExecProcess (fun info ->
        info.FileName <- "appveyor"
        info.Arguments <- args) (System.TimeSpan.MaxValue)
    |> ignore

let private add msg category =
    if not <| isNullOrEmpty msg then
        let enableProcessTracingPreviousValue = enableProcessTracing
        enableProcessTracing <- false
        sprintf "AddMessage %s -Category %s" (quoteIfNeeded msg) (quoteIfNeeded category) |> sendToAppVeyor
        enableProcessTracing <- enableProcessTracingPreviousValue
let private addNoCategory msg = sprintf "AddMessage %s" (quoteIfNeeded msg) |> sendToAppVeyor

// Add trace listener to track messages
if buildServer = BuildServer.AppVeyor then
    { new ITraceListener with
          member this.Write msg =
              match msg with
              | ErrorMessage x -> add x "Error"
              | ImportantMessage x -> add x "Warning"
              | LogMessage(x, _) -> add x "Information"
              | TraceMessage(x, _) ->
                  if not enableProcessTracing then addNoCategory x
              | StartMessage | FinishedMessage | OpenTag(_, _) | CloseTag _ -> () }
    |> listeners.Add

/// Finishes the test suite.
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', install the build-server and use 'use dis = Trace.traceTag (KnownTags.TestSuite name)' instead")>]
let FinishTestSuite testSuiteName = () // Nothing in API yet

/// Starts the test suite.
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', install the build-server and use 'use dis = Trace.traceTag (KnownTags.TestSuite name)' instead")>]
let StartTestSuite testSuiteName = () // Nothing in API yet

/// Starts the test case.
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', install the build-server and use 'use dis = Trace.traceTag (KnownTags.Test name)' instead")>]
let StartTestCase testSuiteName testCaseName =
    sendToAppVeyor <| sprintf "AddTest \"%s\" -Outcome Running" (testSuiteName + " - " + testCaseName)

/// Reports a failed test.
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', install the build-server and use 'Trace.testStatus testName (TestStatus.Failed(message, details, None))' instead")>]
let TestFailed testSuiteName testCaseName message details =
    sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Failed -ErrorMessage %s -ErrorStackTrace %s" (testSuiteName + " - " + testCaseName)
        (EncapsulateSpecialChars message) (EncapsulateSpecialChars details)

/// Ignores the test case.
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', install the build-server and use 'Trace.testStatus testName (TestStatus.Ignore(message))' instead")>]
let IgnoreTestCase testSuiteName testCaseName message = sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Ignored" (testSuiteName + " - " + testCaseName)

/// Reports a succeeded test.
[<System.Obsolete("please remove this call, success is implicit.")>]
let TestSucceeded testSuiteName testCaseName = sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Passed" (testSuiteName + " - " + testCaseName)

/// Finishes the test case.
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', install the build-server and use 'use dis = Trace.traceTag (KnownTags.Test name)' instead")>]
let FinishTestCase testSuiteName testCaseName (duration : System.TimeSpan) =
    let duration =
        duration.TotalMilliseconds
        |> round
        |> string

    sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Duration %s" (testSuiteName + " - " + testCaseName) duration

/// Union type representing the available test result formats accepted by AppVeyor.
[<System.Obsolete("This type should no longer be required (use 'Fake.BuildServer.AppVeyor').")>]
type TestResultsType =
    | MsTest
    | Xunit
    | NUnit
    | NUnit3
    | JUnit

/// Uploads a test result file to make them visible in Test tab of the build console.
[<System.Obsolete("This should no longer be required (install 'Fake.BuildServer.AppVeyor') and use 'Trace.publish (ImportData.<type>) file'.")>]
let UploadTestResultsFile (testResultsType : TestResultsType) file =
    if buildServer = BuildServer.AppVeyor then
        let resultsType = (sprintf "%A" testResultsType).ToLower()
        let url = sprintf "https://ci.appveyor.com/api/testresults/%s/%s" resultsType AppVeyorEnvironment.JobId
        use wc = new System.Net.WebClient()
        try
            wc.UploadFile(url, file) |> ignore
            printfn "Successfully uploaded test results %s" file
        with
        | ex -> printfn "An error occurred while uploading %s:\r\n%O" file ex

/// Uploads all the test results ".xml" files in a directory to make them visible in Test tab of the build console.
[<System.Obsolete("This should no longer be required (install 'Fake.BuildServer.AppVeyor') and use 'Trace.publish (ImportData.<type>) file'.")>]
let UploadTestResultsXml (testResultsType : TestResultsType) outputDir =
    if buildServer = BuildServer.AppVeyor then
        System.IO.Directory.EnumerateFiles(path = outputDir, searchPattern = "*.xml")
        |> Seq.map(fun file -> async { UploadTestResultsFile testResultsType file })
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore

/// Set environment variable
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.setVariable instead")>]
let SetVariable name value =
    sendToAppVeyor <| sprintf "SetVariable -Name \"%s\" -Value \"%s\"" name value

/// Type of artifact that is pushed
[<System.Obsolete("This type should no longer be required (use 'Fake.BuildServer.AppVeyor').")>]
type ArtifactType = Auto | WebDeployPackage

/// AppVeyor parameters for artifact push as [described](https://www.appveyor.com/docs/build-worker-api/#push-artifact)
[<System.Obsolete("This type should no longer be required (use 'Fake.BuildServer.AppVeyor').")>]
[<CLIMutable>]
type PushArtifactParams =
    {
        /// The full local path to the artifact
        Path: string
        /// File name to display in the artifact tab
        FileName: string
        /// Deployment name
        DeploymentName: string
        /// Type of the artifact
        Type: ArtifactType
    }

/// AppVeyor artifact push default parameters

[<System.Obsolete("This should no longer be required (use 'Fake.BuildServer.AppVeyor').")>]
let defaultPushArtifactParams =
    {
        Path = ""
        FileName = ""
        DeploymentName = ""
        Type = Auto
    }

let private appendArgIfNotNullOrEmpty value name builder =
    if (isNotNullOrEmpty value) then
        appendWithoutQuotes (sprintf "-%s \"%s\"" name value) builder
    else
        builder

/// Push an artifact
[<System.Obsolete("This should no longer be required (install 'Fake.BuildServer.AppVeyor') and use 'Trace.publish ImportData.BuildArtifact file'.")>]
let PushArtifact (setParams : PushArtifactParams -> PushArtifactParams) =
    if buildServer = BuildServer.AppVeyor then
        let parameters = setParams defaultPushArtifactParams
        new System.Text.StringBuilder()
        |> append "PushArtifact"
        |> append parameters.Path
        |> appendArgIfNotNullOrEmpty parameters.FileName "FileName"
        |> appendArgIfNotNullOrEmpty parameters.DeploymentName "DeploymentName"
        |> appendArgIfNotNullOrEmpty (sprintf "%A" parameters.Type) "Type"
        |> toText
        |> sendToAppVeyor

/// Push multiple artifacts
[<System.Obsolete("This should no longer be required (install 'Fake.BuildServer.AppVeyor') and use 'Trace.publish ImportData.BuildArtifact file'.")>]
let PushArtifacts paths =
    if buildServer = BuildServer.AppVeyor then
        for path in paths do
            PushArtifact (fun p -> { p with Path = path; FileName = Path.GetFileName(path) })

/// AppVeyor parameters for update build as [described](https://www.appveyor.com/docs/build-worker-api/#update-build-details)
[<System.Obsolete("This should no longer be required (use 'Fake.BuildServer.AppVeyor').")>]
[<CLIMutable>]
type UpdateBuildParams =
    { /// Build version; must be unique for the current project
      Version : string
      /// Commit message
      Message : string
      /// Commit hash
      CommitId : string
      /// Commit date
      Committed : DateTime option
      /// Commit author name
      AuthorName : string
      /// Commit author email address
      AuthorEmail : string
      /// Committer name
      CommitterName : string
      /// Committer email address
      CommitterEmail : string }

let private defaultUpdateBuildParams =
    { Version = ""
      Message = ""
      CommitId = ""
      Committed = None
      AuthorName = ""
      AuthorEmail = ""
      CommitterName = ""
      CommitterEmail = "" }

/// Update build details
[<System.Obsolete("please use nuget 'Fake.BuildServer.AppVeyor', open Fake.BuildServer and use AppVeyor.updateBuild instead")>]
let UpdateBuild (setParams : UpdateBuildParams -> UpdateBuildParams) =
    if buildServer = BuildServer.AppVeyor then
        let parameters = setParams defaultUpdateBuildParams

        let committedStr =
            match parameters.Committed with
            | Some x -> x.ToString("o")
            | None -> ""

        System.Text.StringBuilder()
        |> append "UpdateBuild"
        |> appendArgIfNotNullOrEmpty parameters.Version "Version"
        |> appendArgIfNotNullOrEmpty parameters.Message "Message"
        |> appendArgIfNotNullOrEmpty parameters.CommitId "CommitId"
        |> appendArgIfNotNullOrEmpty committedStr "Committed"
        |> appendArgIfNotNullOrEmpty parameters.AuthorName "AuthorName"
        |> appendArgIfNotNullOrEmpty parameters.AuthorEmail "AuthorEmail"
        |> appendArgIfNotNullOrEmpty parameters.CommitterName "CommitterName"
        |> appendArgIfNotNullOrEmpty parameters.CommitterEmail "CommitterEmail"
        |> toText
        |> sendToAppVeyor

/// Update build version. This must be unique for the current project.
[<System.Obsolete("This should no longer be required (install 'Fake.BuildServer.AppVeyor') and use 'Trace.setBuildNumber version'.")>]
let UpdateBuildVersion version =
    UpdateBuild (fun p -> { p with Version = version })
