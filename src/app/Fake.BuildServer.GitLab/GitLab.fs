/// Contains support for various build servers
namespace Fake.BuildServer

open System.IO
open Fake.Core
open Fake.IO

[<AutoOpen>]
module GitLabImportExtensions =
    type DotNetCoverageTool with
        member x.GitLabName =
            match x with
            | DotNetCoverageTool.DotCover -> "dotcover"
            | DotNetCoverageTool.PartCover -> "partcover"
            | DotNetCoverageTool.NCover -> "ncover"
            | DotNetCoverageTool.NCover3 -> "ncover3"

    type ImportData with
        member x.GitLabName =
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

/// native support for GitLab specific APIs.
/// The general documentation on how to use CI server integration can be found [here](/buildserver.html).
/// This module does not provide any special APIs please use FAKE APIs and they should integrate into this CI server.
/// If some integration is not working as expected or you have features you would like to use directly please open an issue. 
[<RequireQualifiedAccess>]
module GitLab =

    /// https://docs.gitlab.com/ee/ci/variables/
    type Environment =

        /// The branch or tag name for which project is built
        static member CommitRefName = Environment.environVar "CI_COMMIT_REF_NAME"
        /// $CI_COMMIT_REF_NAME lowercased, shortened to 63 bytes, and with everything except 0-9 and a-z replaced with -.
        /// No leading / trailing -.
        /// Use in URLs, host names and domain names.
        static member CommitRefSlug = Environment.environVar "CI_COMMIT_REF_SLUG"
        /// The commit revision for which project is built
        static member CommitSha = Environment.environVar "CI_COMMIT_SHA"        
        /// The previous latest commit present on a branch before a push request.
        static member CommitBeforeSha = Environment.environVar "CI_COMMIT_BEFORE_SHA"
        /// The commit tag name. Present only when building tags.
        static member CommitTag = Environment.environVar "CI_COMMIT_TAG"        
        /// The full commit message.
        static member CommitMessage = Environment.environVar "CI_COMMIT_MESSAGE"        
        /// The title of the commit - the full first line of the message
        static member CommitTitle = Environment.environVar "CI_COMMIT_TITLE"        
        /// The description of the commit: the message without first line, if the title is shorter than 100 characters; full message in other case.
        static member CommitDescription = Environment.environVar "CI_COMMIT_DESCRIPTION"
        /// The path to CI config file. Defaults to .gitlab-ci.yml
        static member ConfigPath = Environment.environVar "CI_CONFIG_PATH"
        /// Whether debug tracing is enabled
        static member DebugTrace = Environment.hasEnvironVar "CI_DEBUG_TRACE"
        /// Authentication username of the GitLab Deploy Token, only present if the Project has one related.
        static member DeployUser = Environment.environVar "CI_DEPLOY_USER"
        /// Authentication password of the GitLab Deploy Token, only present if the Project has one related.
        static member DeployPassword = Environment.environVar "CI_DEPLOY_PASSWORD"
        /// Marks that the job is executed in a disposable environment (something that is created only for this job and disposed of/destroyed after the execution - all executors except shell and ssh). If the environment is disposable, it is set to true, otherwise it is not defined at all.
        static member DisposableEnvironment = Environment.hasEnvironVar "CI_DISPOSABLE_ENVIRONMENT"
        /// The name of the environment for this job
        static member EnvironmentName = Environment.environVar "CI_ENVIRONMENT_NAME"
        /// A simplified version of the environment name, suitable for inclusion in DNS, URLs, Kubernetes labels, etc.
        static member EnvironmentSlug = Environment.environVar "CI_ENVIRONMENT_SLUG"        
        /// The URL of the environment for this job
        static member EnvironmentUrl = Environment.environVar "CI_ENVIRONMENT_URL"
        /// The unique id of the current job that GitLab CI uses internally
        static member JobId = Environment.environVar "CI_JOB_ID"
        /// The flag to indicate that job was manually started
        static member JobManual = Environment.environVar "CI_JOB_MANUAL"
        /// The name of the job as defined in .gitlab-ci.yml
        static member JobName = Environment.environVar "CI_JOB_NAME"
        /// The name of the stage as defined in .gitlab-ci.yml
        static member JobStage = Environment.environVar "CI_JOB_STAGE"
        /// Token used for authenticating with GitLab Container Registry, downloading dependent repositories, authenticate with multi-project pipelines when triggers are involved, and for downloading job artifacts
        static member JobToken = Environment.environVar "CI_JOB_TOKEN"
        /// Job details URL
        static member JobUrl = Environment.environVar "CI_JOB_URL"
        /// The URL to clone the Git repository
        static member RepositoryUrl = Environment.environVar "CI_REPOSITORY_URL"
        /// The description of the runner as saved in GitLab
        static member RunnerDescription = Environment.environVar "CI_RUNNER_DESCRIPTION"
        /// The unique id of runner being used
        static member RunnerId = Environment.environVar "CI_RUNNER_ID"
        /// The defined runner tags
        static member RunnerTags = Environment.environVar "CI_RUNNER_TAGS"
        /// GitLab Runner version that is executing the current job
        static member RunnerVersion = Environment.environVar "CI_RUNNER_VERSION"
        /// GitLab Runner revision that is executing the current job
        static member RunnerRevision = Environment.environVar "CI_RUNNER_REVISION"
        /// The OS/architecture of the GitLab Runner executable (note that this is not necessarily the same as the environment of the executor)
        static member RunnerExecutableArch = Environment.environVar "CI_RUNNER_EXECUTABLE_ARCH"
        /// The unique id of the current pipeline that GitLab CI uses internally
        static member PipelineId = Environment.environVar "CI_PIPELINE_ID"
        /// The unique id of the current pipeline scoped to project
        static member PipelineIID = Environment.environVar "CI_PIPELINE_IID"
        /// The flag to indicate that job was triggered
        static member PipelineTriggered = Environment.hasEnvironVar "CI_PIPELINE_TRIGGERED"
        /// Indicates how the pipeline was triggered. Possible options are: push, web, trigger, schedule, api, and pipeline. For pipelines created before GitLab 9.5, this will show as unknown
        static member PipelineSource = Environment.environVar "CI_PIPELINE_SOURCE"
        /// The full path where the repository is cloned and where the job is run
        static member ProjectDir = Environment.environVar "CI_PROJECT_DIR"
        /// The unique id of the current project that GitLab CI uses internally
        static member ProjectId = Environment.environVar "CI_PROJECT_ID"
        /// The project name that is currently being built (actually it is project folder name)
        static member ProjectName = Environment.environVar "CI_PROJECT_NAME"
        /// The project namespace (username or groupname) that is currently being built
        static member ProjectNamespace = Environment.environVar "CI_PROJECT_NAMESPACE"
        /// The namespace with project name
        static member ProjectPath = Environment.environVar "CI_PROJECT_PATH"
        ///  $CI_PROJECT_PATH lowercased and with everything except 0-9 and a-z replaced with -. Use in URLs and domain names.
        static member ProjectPathSlug = Environment.environVar "CI_PROJECT_PATH_SLUG"
        /// Pipeline details URL
        static member PipelineUrl = Environment.environVar "CI_PIPELINE_URL"
        /// The HTTP address to access project
        static member ProjectUrl = Environment.environVar "CI_PROJECT_URL"
        /// The project visibility (internal, private, public)
        static member ProjectVisibility = Environment.environVar "CI_PROJECT_VISIBILITY"
        /// If the Container Registry is enabled it returns the address of GitLab's Container Registry
        static member Registry = Environment.environVar "CI_REGISTRY"
        /// If the Container Registry is enabled for the project it returns the address of the registry tied to the specific project
        static member RegistryImage = Environment.environVar "CI_REGISTRY_IMAGE"
        /// The password to use to push containers to the GitLab Container Registry
        static member RegistryPassword = Environment.environVar "CI_REGISTRY_PASSWORD"
        /// The username to use to push containers to the GitLab Container Registry
        static member RegistryUser = Environment.environVar "CI_REGISTRY_USER"
        /// Mark that job is executed in CI environment
        static member Server = Environment.hasEnvironVar "CI_SERVER"
        /// The name of CI server that is used to coordinate jobs
        static member ServerName = Environment.environVar "CI_SERVER_NAME"
        /// GitLab revision that is used to schedule jobs
        static member ServerRevision = Environment.environVar "CI_SERVER_REVISION"
        /// GitLab version that is used to schedule jobs
        static member ServerVersion = Environment.environVar "CI_SERVER_VERSION"
        /// Marks that the job is executed in a shared environment (something that is persisted across CI invocations like shell or ssh executor). If the environment is shared, it is set to true, otherwise it is not defined at all.
        static member SharedEnvironment = Environment.hasEnvironVar "CI_SHARED_ENVIRONMENT"
        /// Number of attempts to fetch sources running a job
        static member GetSourcesAttempts = Environment.environVar "GET_SOURCES_ATTEMPTS"
        /// Mark that job is executed in GitLab CI environment
        static member GitlabCI = Environment.environVar "GITLAB_CI"
        /// The email of the user who started the job
        static member GitlabUserEmail = Environment.environVar "GITLAB_USER_EMAIL"
        /// The id of the user who started the job
        static member GitlabUserId = Environment.environVar "GITLAB_USER_ID"
        /// The login username of the user who started the job
        static member GitlabUserLogin = Environment.environVar "GITLAB_USER_LOGIN"
        /// The real name of the user who started the job
        static member GitlabUserName = Environment.environVar "GITLAB_USER_NAME"
        /// The comma separated list of licensed features available for your instance and plan
        static member GitlabFeatures = Environment.environVar "GITLAB_FEATURES"
        /// Number of attempts to restore the cache running a job
        static member RestoreCacheAttempts = Environment.environVar "RESTORE_CACHE_ATTEMPTS"

    /// Implements a TraceListener for TeamCity build servers.
    /// ## Parameters
    ///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
    ///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
    type internal GitLabTraceListener() =

        interface ITraceListener with
            /// Writes the given message to the Console.
            member __.Write msg = 
                let color = ConsoleWriter.colorMap msg
                let importantMessagesToStdErr = true
                let write = ConsoleWriter.writeAnsiColor //else ConsoleWriter.write
                match msg with
                | TraceData.ImportantMessage text | TraceData.ErrorMessage text ->
                    write importantMessagesToStdErr color true text
                | TraceData.LogMessage(text, newLine) | TraceData.TraceMessage(text, newLine) ->
                    write false color newLine text
                | TraceData.OpenTag (tag, descr) ->
                    match descr with
                    | Some d -> write false color true (sprintf "Starting %s '%s': %s" tag.Type tag.Name d)
                    | _ -> write false color true (sprintf "Starting %s '%s'" tag.Type tag.Name)  
                | TraceData.CloseTag (tag, time, state) ->
                    write false color true (sprintf "Finished (%A) '%s' in %O" state tag.Name time)
                | TraceData.BuildState (state, _) ->
                    write false color true (sprintf "Changing BuildState to: %A" state)
                | TraceData.ImportData (typ, path) ->
                    let name = Path.GetFileName path
                    let target = Path.Combine("artifacts", name)
                    let targetDir = Path.GetDirectoryName target
                    Directory.ensure targetDir
                    Shell.cp_r path target
                    write false color true (sprintf "Import data '%O': %s -> %s" typ path target)
                | TraceData.TestOutput (test, out, err) ->
                    write false color true (sprintf "Test '%s' output:\n\tOutput: %s\n\tError: %s" test out err)
                | TraceData.BuildNumber number ->
                    write false color true (sprintf "Build Number: %s" number)
                | TraceData.TestStatus (test, status) ->
                    write false color true (sprintf "Test '%s' status: %A" test status)

    let defaultTraceListener =
      GitLabTraceListener() :> ITraceListener
    let detect () =
        BuildServer.buildServer = BuildServer.GitLabCI
    let install(force:bool) =
        if not (detect()) then failwithf "Cannot run 'install()' on a non-AppVeyor environment"
        if force || not (CoreTracing.areListenersSet()) then
            CoreTracing.setTraceListeners [defaultTraceListener]
        () 
    let Installer =
        { new BuildServerInstaller() with
            member __.Install () = install (false)
            member __.Detect () = detect() }
