/// Contains support for various build servers
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core
open Fake.IO

[<AutoOpen>]
module AppVeyorImportExtensions =
    type DotNetCoverageTool with
        member x.AppVeyorName =
            match x with
            | DotNetCoverageTool.DotCover -> "dotcover"
            | DotNetCoverageTool.PartCover -> "partcover"
            | DotNetCoverageTool.NCover -> "ncover"
            | DotNetCoverageTool.NCover3 -> "ncover3"

    type ImportData with
        member x.AppVeyorName =
            match x with
            | ImportData.BuildArtifactWithName _
            | ImportData.BuildArtifact -> "buildArtifact"
            | ImportData.DotNetCoverage _ -> "dotNetCoverage"
            | ImportData.DotNetDupFinder -> "DotNetDupFinder"
            | ImportData.PmdCpd -> "pmdCpd"
            | ImportData.Pmd -> "pmd"
            | ImportData.ReSharperInspectCode -> "ReSharperInspectCode"
            | ImportData.Jslint -> "jslint"
            | ImportData.FindBugs -> "findBugs"
            | ImportData.Checkstyle -> "checkstyle"
            | ImportData.Gtest -> "gtest"
            | ImportData.Surefire -> "surefire"
            | ImportData.FxCop -> "FxCop"
            | ImportData.Mstest -> "mstest"
            | ImportData.Nunit NunitDataVersion.Nunit -> "nunit"
            | ImportData.Nunit NunitDataVersion.Nunit3 -> "nunit3"
            | ImportData.Junit -> "junit"
            | ImportData.Xunit -> "xunit"

/// native support for AppVeyor specific APIs.
/// The general documentation on how to use CI server integration can be found [here](/buildserver.html).
/// This module does not provide any special APIs please use FAKE APIs and they should integrate into this CI server.
/// If some integration is not working as expected or you have features you would like to use directly please open an issue. 
[<RequireQualifiedAccess>]
module AppVeyor =
    // See https://www.appveyor.com/docs/build-worker-api/#update-tests

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
    let private appendArgIfNotNullOrEmpty = AppVeyorInternal.appendArgIfNotNullOrEmpty
    /// Update build details
    let updateBuild (setParams : UpdateBuildParams -> UpdateBuildParams) =
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
        |> AppVeyorInternal.sendToAppVeyor

    /// Update build version. This must be unique for the current project.
    let private updateBuildVersion version =
        updateBuild (fun p -> { p with Version = version })
    let setVariable name value =
        AppVeyorInternal.sendToAppVeyor <| sprintf "SetVariable -Name \"%s\" -Value \"%s\"" name value
    let private environVar = Environment.environVar

    /// AppVeyor environment variables as [described](http://www.appveyor.com/docs/environment-variables)
    type Environment =

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
        


    /// Implements a TraceListener for TeamCity build servers.
    /// ## Parameters
    ///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
    ///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
    type internal AppVeyorTraceListener() =
        let mutable currentTestSuite = None
        let getCurrentTestSuite() =
            match currentTestSuite with
            | None -> "defaultSuite"
            | Some s -> s

        let mutable currentTestOutput = None        

        let mutable currentTestResult = None

        interface ITraceListener with
            /// Writes the given message to the Console.
            member __.Write msg = 
                let color = ConsoleWriter.colorMap msg
                match msg with
                | TraceData.OpenTag (KnownTags.Test name, _) ->
                    AppVeyorInternal.StartTestCase (getCurrentTestSuite()) name 
                | TraceData.TestOutput (testName,out,err) ->
                    currentTestOutput <- Some(out,err)
                | TraceData.TestStatus (testName, status) ->
                    currentTestResult <- Some status
                | TraceData.CloseTag (KnownTags.Test name, time, status) ->
                    let outcome, msg, detail =
                        match currentTestResult with
                        | None -> "Passed", "", ""
                        | Some (TestStatus.Ignored msg) -> "Ignored", msg, ""
                        | Some (TestStatus.Failed(message, detail, None)) -> "Failed", message, detail
                        | Some (TestStatus.Failed(message, detail, Some(expected, actual))) ->
                            "Failed", sprintf "%s: Expected '%s' but was '%s'" message expected actual, detail
                    let stdOut, stdErr =
                        match currentTestOutput with
                        | Some (out, err) -> out, err
                        | None -> "", ""                    
                    AppVeyorInternal.UpdateTestEx (getCurrentTestSuite()) name outcome msg detail stdOut stdErr
                    AppVeyorInternal.FinishTestCase (getCurrentTestSuite()) name time
                | TraceData.OpenTag (KnownTags.TestSuite name, _) ->
                    currentTestSuite <- Some name
                | TraceData.CloseTag (KnownTags.TestSuite name, _, _) ->
                    currentTestSuite <- None
                | TraceData.BuildState (state, _) ->
                    ConsoleWriter.writeAnsiColor false color true (sprintf "Changing BuildState to: %A" state)
                | TraceData.OpenTag (tag, descr) ->
                    match descr with
                    | Some d -> ConsoleWriter.writeAnsiColor false color true (sprintf "Starting %s '%s': %s" tag.Type tag.Name d)
                    | _ -> ConsoleWriter.writeAnsiColor false color true (sprintf "Starting %s '%s'" tag.Type tag.Name)  
                | TraceData.CloseTag (tag, time, state) ->
                    ConsoleWriter.writeAnsiColor false color true (sprintf "Finished (%A) '%s' in %O" state tag.Name time)
                | TraceData.ImportantMessage text ->
                    ConsoleWriter.writeAnsiColor false color true text
                    AppVeyorInternal.AddMessage AppVeyorInternal.MessageCategory.Warning "" text
                | TraceData.ErrorMessage text ->
                    ConsoleWriter.writeAnsiColor false color true text
                    AppVeyorInternal.AddMessage AppVeyorInternal.MessageCategory.Error "" text
                | TraceData.LogMessage(text, newLine) | TraceData.TraceMessage(text, newLine) ->
                    ConsoleWriter.writeAnsiColor false color newLine text
                | TraceData.ImportData (ImportData.Nunit NunitDataVersion.Nunit, path) ->
                    AppVeyorInternal.UploadTestResultsFile AppVeyorInternal.TestResultsType.NUnit path
                | TraceData.ImportData (ImportData.Nunit NunitDataVersion.Nunit3, path) ->
                    AppVeyorInternal.UploadTestResultsFile AppVeyorInternal.TestResultsType.NUnit3 path
                | TraceData.ImportData (ImportData.Mstest, path) ->
                    AppVeyorInternal.UploadTestResultsFile AppVeyorInternal.TestResultsType.MsTest path
                | TraceData.ImportData (ImportData.Xunit, path) ->
                    AppVeyorInternal.UploadTestResultsFile AppVeyorInternal.TestResultsType.Xunit path
                | TraceData.ImportData (ImportData.Junit, path) ->
                    AppVeyorInternal.UploadTestResultsFile AppVeyorInternal.TestResultsType.JUnit path
                | TraceData.ImportData (ImportData.BuildArtifactWithName _, path)
                | TraceData.ImportData (ImportData.BuildArtifact, path) ->
                    AppVeyorInternal.PushArtifact (fun parms -> { parms with Path = path; FileName = Path.GetFileName path })
                | TraceData.ImportData (typ, path) ->
                    AppVeyorInternal.PushArtifact (fun parms ->
                        { parms with Path = path; FileName = Path.GetFileName path; DeploymentName = typ.AppVeyorName })
                | TraceData.BuildNumber number -> updateBuildVersion number

    let defaultTraceListener =
      AppVeyorTraceListener() :> ITraceListener
    let detect () =
        BuildServer.buildServer = BuildServer.AppVeyor
    let install(force:bool) =
        if not (detect()) then failwithf "Cannot run 'install()' on a non-AppVeyor environment"
        if force || not (CoreTracing.areListenersSet()) then
            CoreTracing.setTraceListeners [defaultTraceListener]
        () 
    let Installer =
        { new BuildServerInstaller() with
            member __.Install () = install (false)
            member __.Detect () = detect() }
