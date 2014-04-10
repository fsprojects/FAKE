[<AutoOpen>]
/// Contains code to configure FAKE for AppVeyor integration
module Fake.AppVeyor

open Fake.MSBuildHelper

/// AppVeyor environment variables as [described](http://www.appveyor.com/docs/environment-variables)
type AppVeyorEnvironment =
    static member ApiUrl = environVar "APPVEYOR_API_URL"
    static member ProjectId = environVar "APPVEYOR_PROJECT_ID"
    static member ProjectName = environVar "APPVEYOR_PROJECT_NAME"
    static member ProjectSlug = environVar "APPVEYOR_PROJECT_SLUG"
    static member BuildFolder = environVar "APPVEYOR_BUILD_FOLDER"
    static member BuildId = environVar "APPVEYOR_BUILD_ID"
    static member BuildNumber = environVar "APPVEYOR_BUILD_NUMBER"
    static member BuildVersion = environVar "APPVEYOR_BUILD_VERSION"
    static member JobId = environVar "APPVEYOR_JOB_ID"
    static member RepoProvider = environVar "APPVEYOR_REPO_PROVIDER"
    static member RepoScm = environVar "APPVEYOR_REPO_SCM"
    static member RepoName = environVar "APPVEYOR_REPO_NAME"
    static member RepoBranch = environVar "APPVEYOR_REPO_BRANCH"
    static member RepoCommit = environVar "APPVEYOR_REPO_COMMIT"
    static member RepoCommitAuthor = environVar "APPVEYOR_REPO_COMMIT_AUTHOR"
    static member RepoCommitAuthorEmail = environVar "APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL"
    static member RepoCommitTimestamp = environVar "APPVEYOR_REPO_COMMIT_TIMESTAMP"
    static member RepoCommitMessage = environVar "APPVEYOR_REPO_COMMIT_MESSAGE"

let private sendToAppVeyor args = 
    ExecProcess (fun info -> 
        info.FileName <- "appveyor"
        info.Arguments <- args) (System.TimeSpan.MaxValue)
    |> ignore

let private add msg category = 
    sprintf "AddMessage %s -Category %s" (quoteIfNeeded msg) (quoteIfNeeded category) |> sendToAppVeyor
let private addNoCategory msg = sprintf "AddMessage %s" (quoteIfNeeded msg) |> sendToAppVeyor

// Add trace listener to track messages
if buildServer = BuildServer.AppVeyor then 
    listeners.Add({ new ITraceListener with
                        member this.Write msg = 
                            match msg with
                            | ErrorMessage x -> add x "Error"
                            | ImportantMessage x -> add x "Warning"
                            | LogMessage(x, _) -> add x "Information"
                            | TraceMessage(x, _) -> 
                                if not enableProcessTracing then addNoCategory x
                            | StartMessage | FinishedMessage | OpenTag(_, _) | CloseTag _ -> () })

// Add MSBuildLogger to track build messages
if buildServer = BuildServer.AppVeyor then 
    MSBuildLoggers <- @"""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll""" :: MSBuildLoggers
