/// Contains support for various build servers
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core
open Fake.IO
open Fake.Net
open Microsoft.FSharp.Reflection
open System.Text.RegularExpressions

module internal AppVeyorInternal =
    let environVar = Environment.environVar

    /// AppVeyor environment variables as [described](http://www.appveyor.com/docs/environment-variables)
    type AppVeyorEnvironment =

        /// AppVeyor Build Agent API URL
        static member ApiUrl = environVar "APPVEYOR_API_URL"

        /// AppVeyor Account Name
        static member AccountName = environVar "APPVEYOR_ACCOUNT_NAME"

        /// AppVeyor unique project ID
        static member ProjectId = environVar "APPVEYOR_PROJECT_ID"

        /// Project name
        static member ProjectName = environVar "APPVEYOR_PROJECT_NAME"

        /// Project slug (as seen in project details URL)
        static member ProjectSlug = environVar "APPVEYOR_PROJECT_SLUG"

        /// Path to clone directory
        static member BuildFolder = environVar "APPVEYOR_BUILD_FOLDER"

        /// AppVeyor unique build ID
        static member BuildId = environVar "APPVEYOR_BUILD_ID"

        /// Build number
        static member BuildNumber = environVar "APPVEYOR_BUILD_NUMBER"

        /// Build version
        static member BuildVersion = environVar "APPVEYOR_BUILD_VERSION"

        /// GitHub Pull Request number
        static member PullRequestNumber = environVar "APPVEYOR_PULL_REQUEST_NUMBER"

        /// GitHub Pull Request title
        static member PullRequestTitle = environVar "APPVEYOR_PULL_REQUEST_TITLE"

        /// GitHub Pull Request Repo name
        static member PullRequestRepoName = environVar "APPVEYOR_PULL_REQUEST_REPO_NAME"

        /// GitHub Pull Request branch
        static member PullRequestRepoBranch = environVar "APPVEYOR_PULL_REQUEST_REPO_BRANCH"

        /// AppVeyor unique job ID
        static member JobId = environVar "APPVEYOR_JOB_ID"

        /// GitHub, BitBucket or Kiln
        static member RepoProvider = environVar "APPVEYOR_REPO_PROVIDER"

        /// git or mercurial
        static member RepoScm = environVar "APPVEYOR_REPO_SCM"

        /// Repository name in format owner-name/repo-name
        static member RepoName = environVar "APPVEYOR_REPO_NAME"

        /// Build branch
        static member RepoBranch = environVar "APPVEYOR_REPO_BRANCH"

        /// Commit ID (SHA)
        static member RepoCommit = environVar "APPVEYOR_REPO_COMMIT"

        /// Commit author's name
        static member RepoCommitAuthor = environVar "APPVEYOR_REPO_COMMIT_AUTHOR"

        /// Commit author's email address
        static member RepoCommitAuthorEmail = environVar "APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL"

        /// Commit date/time
        static member RepoCommitTimestamp = environVar "APPVEYOR_REPO_COMMIT_TIMESTAMP"

        /// Commit message
        static member RepoCommitMessage = environVar "APPVEYOR_REPO_COMMIT_MESSAGE"

        /// The rest of the commit message after line break (if exists)
        static member RepoCommitMessageExtended = environVar "APPVEYOR_REPO_COMMIT_MESSAGE_EXTENDED"

        /// If the build runs by scheduler;
        static member IsScheduledBuild = environVar "APPVEYOR_SCHEDULED_BUILD"

        /// If the build has been started by the "New Build" button or from the same API
        static member IsForcedBuild = environVar "APPVEYOR_FORCED_BUILD"

        /// If the build has been started by the "Re-Build commit/PR" button or from the same API
        static member IsReBuild = environVar "APPVEYOR_RE_BUILD"

        /// true if build has started by pushed tag; otherwise false
        static member RepoTag =
            let rt = environVar "APPVEYOR_REPO_TAG"
            not (isNull rt) && rt.Equals("true", System.StringComparison.OrdinalIgnoreCase)

        /// contains tag name for builds started by tag
        static member RepoTagName = environVar "APPVEYOR_REPO_TAG_NAME"

        /// Platform name set on Build tab of project settings (or through platform parameter in appveyor.yml).
        static member Platform = environVar "PLATFORM"

        /// Configuration name set on Build tab of project settings (or through configuration parameter in appveyor.yml).
        static member Configuration  = environVar "CONFIGURATION"

        /// The job name
        static member JobName = environVar "APPVEYOR_JOB_NAME"
        
        /// The Job Number
        static member JobNumber = environVar "APPVEYOR_JOB_NUMBER"
        
        /// set to true to disable cache restore
        static member CacheSkipRestore = environVar "APPVEYOR_CACHE_SKIP_RESTORE"
        
        /// set to true to disable cache update
        static member CacheSkipSave = environVar "APPVEYOR_CACHE_SKIP_SAVE"
        
        /// Current build worker image the build is running on, e.g. Visual Studio 2015
        static member BuildWorkerImage = environVar "APPVEYOR_BUILD_WORKER_IMAGE"
        
        /// Artifact upload timeout in seconds. Default is 600 (10 minutes)
        static member ArtifactUploadTimeout = environVar "APPVEYOR_ARTIFACT_UPLOAD_TIMEOUT"
        
        /// Timeout in seconds to download arbirtary files using appveyor DownloadFile command. Default is 300 (5 minutes)
        static member FileDownloadTimeout = environVar "APPVEYOR_FILE_DOWNLOAD_TIMEOUT"
        
        /// Timeout in seconds to download repository (GitHub, Bitbucket or VSTS) as zip file (shallow clone). Default is 1800 (30 minutes)
        static member RepositoryShallowCloneTimeout = environVar "APPVEYOR_REPOSITORY_SHALLOW_CLONE_TIMEOUT"
        
        /// Timeout in seconds to download or upload each cache entry. Default is 300 (5 minutes)
        static member CacheEntryUploadDownloadTimeout = environVar "APPVEYOR_CACHE_ENTRY_UPLOAD_DOWNLOAD_TIMEOUT"
        
    let private sendToAppVeyor args =
        Process.Exec (fun info ->
            { info with 
                FileName = "appveyor"
                Arguments = args}) (System.TimeSpan.MaxValue)
        |> ignore

    let private add msg category =
        if not <| String.isNullOrEmpty msg then
            //let enableProcessTracingPreviousValue = Process.enableProcessTracing
            //Process.enableProcessTracing <- false
            sprintf "AddMessage %s -Category %s" (Process.quoteIfNeeded msg) (Process.quoteIfNeeded category) |> sendToAppVeyor
            //Process.enableProcessTracing <- enableProcessTracingPreviousValue
    let private addNoCategory msg = sprintf "AddMessage %s" (Process.quoteIfNeeded msg) |> sendToAppVeyor

    /// Starts the test case.
    let StartTestCase testSuiteName testCaseName =
        sendToAppVeyor <| sprintf "AddTest \"%s\" -Outcome Running" (testSuiteName + " - " + testCaseName)

    /// Reports a failed test.
    let TestFailed testSuiteName testCaseName message details =
        sendToAppVeyor <| sprintf "UpdateTest %s -Outcome Failed -ErrorMessage %s -ErrorStackTrace %s"
            (Process.quoteIfNeeded (testSuiteName + " - " + testCaseName))
            (Process.quoteIfNeeded message) (Process.quoteIfNeeded details)

    /// Ignores the test case.
    let IgnoreTestCase testSuiteName testCaseName message = sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Ignored" (testSuiteName + " - " + testCaseName)

    /// Reports a succeeded test.
    let TestSucceeded testSuiteName testCaseName = sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Passed" (testSuiteName + " - " + testCaseName)

    /// Finishes the test case.
    let FinishTestCase testSuiteName testCaseName (duration : System.TimeSpan) =
        let duration =
            duration.TotalMilliseconds
            |> round
            |> string

        sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Duration %s" (testSuiteName + " - " + testCaseName) duration

    /// Union type representing the available test result formats accepted by AppVeyor.
    type TestResultsType =
        | MsTest
        | Xunit
        | NUnit
        | NUnit3
        | JUnit

    /// Uploads a test result file to make them visible in Test tab of the build console.
    let UploadTestResultsFile (testResultsType : TestResultsType) file =
        let resultsType = (sprintf "%A" testResultsType).ToLower()
        let url = sprintf "https://ci.appveyor.com/api/testresults/%s/%s" resultsType AppVeyorEnvironment.JobId
        try
            Http.upload url file
            printfn "Successfully uploaded test results %s" file
        with
        | ex -> printfn "An error occurred while uploading %s:\r\n%O" file ex

    /// Uploads all the test results ".xml" files in a directory to make them visible in Test tab of the build console.
    let UploadTestResultsXml (testResultsType : TestResultsType) outputDir =
        System.IO.Directory.EnumerateFiles(path = outputDir, searchPattern = "*.xml")
        |> Seq.map(fun file -> async { UploadTestResultsFile testResultsType file })
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore

    /// Set environment variable
    let SetVariable name value =
        sendToAppVeyor <| sprintf "SetVariable -Name \"%s\" -Value \"%s\"" name value

    /// Type of artifact that is pushed
    type ArtifactType = Auto | WebDeployPackage

    /// AppVeyor parameters for artifact push as [described](https://www.appveyor.com/docs/build-worker-api/#push-artifact)
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
    let defaultPushArtifactParams =
        {
            Path = ""
            FileName = ""
            DeploymentName = ""
            Type = Auto
        }

    let private appendArgIfNotNullOrEmpty value name builder =
        if (String.isNotNullOrEmpty value) then
            StringBuilder.appendWithoutQuotes (sprintf "-%s \"%s\"" name value) builder
        else
            builder

    /// Push an artifact
    let PushArtifact (setParams : PushArtifactParams -> PushArtifactParams) =
        let parameters = setParams defaultPushArtifactParams
        new System.Text.StringBuilder()
        |> StringBuilder.append "PushArtifact"
        |> StringBuilder.append parameters.Path
        |> appendArgIfNotNullOrEmpty parameters.FileName "FileName"
        |> appendArgIfNotNullOrEmpty parameters.DeploymentName "DeploymentName"
        |> appendArgIfNotNullOrEmpty (sprintf "%A" parameters.Type) "Type"
        |> StringBuilder.toText
        |> sendToAppVeyor

    /// Push multiple artifacts
    let PushArtifacts paths =
        for path in paths do
            PushArtifact (fun p -> { p with Path = path; FileName = Path.GetFileName(path) })

    /// AppVeyor parameters for update build as [described](https://www.appveyor.com/docs/build-worker-api/#update-build-details)
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
    let UpdateBuild (setParams : UpdateBuildParams -> UpdateBuildParams) =
        let parameters = setParams defaultUpdateBuildParams

        let committedStr =
            match parameters.Committed with
            | Some x -> x.ToString("o")
            | None -> ""

        System.Text.StringBuilder()
        |> StringBuilder.append "UpdateBuild"
        |> appendArgIfNotNullOrEmpty parameters.Version "Version"
        |> appendArgIfNotNullOrEmpty parameters.Message "Message"
        |> appendArgIfNotNullOrEmpty parameters.CommitId "CommitId"
        |> appendArgIfNotNullOrEmpty committedStr "Committed"
        |> appendArgIfNotNullOrEmpty parameters.AuthorName "AuthorName"
        |> appendArgIfNotNullOrEmpty parameters.AuthorEmail "AuthorEmail"
        |> appendArgIfNotNullOrEmpty parameters.CommitterName "CommitterName"
        |> appendArgIfNotNullOrEmpty parameters.CommitterEmail "CommitterEmail"
        |> StringBuilder.toText
        |> sendToAppVeyor

    /// Update build version. This must be unique for the current project.
    let UpdateBuildVersion version =
        UpdateBuild (fun p -> { p with Version = version })
