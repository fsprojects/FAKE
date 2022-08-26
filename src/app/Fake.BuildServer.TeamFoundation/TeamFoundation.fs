namespace Fake.BuildServer

open System
open System.IO
open Fake.Core
open Fake.IO

/// <summary>
/// Native support for Azure DevOps (previously VSTS) / Team Foundation Server specific APIs.
/// </summary>
/// <remarks>
/// The general documentation on how to use CI server integration can be found <a href="/articles/buildserver.html">here</a>.
/// This module does not provide any special APIs please use FAKE APIs and they should integrate into this CI server.
/// If some integration is not working as expected or you have features you would like to use directly please open an issue.
/// <br/><br/>Secret Variables:
/// <br/>This CI server supports the concept of secret variables and uses the <a href="/articles/core-vault.html">Vault</a> to store them.
/// In order to access secret variables you need to use one of the fake 5 tasks from
/// <a href="https://github.com/isaacabraham/vsts-fsharp">vsts-fsharp</a>.
/// <br/>
/// <br/>
/// </remarks>
/// <example>
/// Example implementation (supports runner and vault tasks)
/// <code lang="fsharp">
///        // Either use a local vault filled by the 'FAKE_VAULT_VARIABLES' environment variable
///        // or fall back to the build process if none is given
///        let vault =
///            match Vault.fromFakeEnvironmentOrNone() with
///            | Some v -> v // fake 5 vault task, uses 'FAKE_VAULT_VARIABLES' by default
///            | None -> TeamFoundation.variables // fake 5 runner task
///
///        // Only needed if you want to fallback to 'normal' environment variables (locally for example)
///        let getVarOrDefault name =
///            match vault.TryGet name with
///            | Some v -> v
///            | None -> Environment.environVarOrFail name
///        Target.create "Deploy" (fun _ ->
///            let token = getVarOrDefault "github_token"
///            // Use token to deploy to github
///
///            let apiKey = getVarOrDefault "nugetkey"
///            // Use apiKey to deploy to nuget
///            ()
///        )
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module TeamFoundation =
    // See https://github.com/Microsoft/vsts-tasks/blob/master/docs/authoring/commands.md

    /// Format and write given message to console with given formatting options
    let write action properties message =
        // https://github.com/Microsoft/vsts-task-lib/blob/3a7905b99d698a535d9f5a477efc124894b8d2ae/node/taskcommand.ts
        let ensurePropVal (s: string) =
            s
                .Replace("\r", "%0D")
                .Replace("\n", "%0A")
                .Replace("]", "%5D")
                .Replace(";", "%3B")

        let ensurePropName (s: string) =
            if s.Contains ";" || s.Contains "=" || s.Contains "]" then
                failwithf "property name cannot contain ';', '=' or ']'"

            s

        let formattedProperties =
            let temp =
                properties
                |> Seq.map (fun (prop, value) -> sprintf "%s=%s;" (ensurePropName prop) (ensurePropVal value))
                |> String.separated ""

            if String.isNullOrWhiteSpace temp then "" else " " + temp

        sprintf "##vso[%s%s]%s" action formattedProperties message
        // printf is racing with others in parallel mode
        |> fun s -> System.Console.WriteLine("\n{0}", s)

    let private seqToPropValue (args: _ seq) = String.Join(",", args)

    /// <summary>
    /// Set <c>task.setvariable</c> with given name and value
    /// </summary>
    /// 
    /// <param name="variableName">The name of the variable to set</param>
    /// <param name="value">The value of the variable to set</param>
    let setVariable variableName value =
        write "task.setvariable" [ "variable", variableName ] value

    let private toType t o = o |> Option.map (fun value -> t, value)
    
    let private toList t o = o |> toType t |> Option.toList

    /// <summary>
    /// Set the <c>task.logissue</c> with given data
    /// </summary>
    let logIssue isWarning sourcePath lineNumber columnNumber code message =

        let typ = if isWarning then "warning" else "error"

        let parameters =
            toList "type" (Some typ)
            @ toList "sourcepath" sourcePath
              @ toList "linenumber" lineNumber
                @ toList "columnnumber" columnNumber @ toList "code" code

        write "task.logissue" parameters message

    let private uploadFile path = write "task.uploadfile" [] path

    let private publishArtifact artifactFolder artifactName path =
        let parameters =
            [ "containerfolder", artifactFolder ] @ toList "artifactname" artifactName

        write "artifact.upload" parameters (Path.GetFullPath path)

    let private setBuildNumber number =
        write "build.updatebuildnumber" [] number

    /// Set the <c>task.complete</c> to given state and message
    ///
    /// <param name="state">The build state</param>
    /// <param name="message">The build state resulting message</param>
    let setBuildState state message =
        write "task.complete" [ "result", state ] message

    // undocumented API: https://github.com/Microsoft/vsts-task-lib/blob/3a7905b99d698a535d9f5a477efc124894b8d2ae/node/task.ts#L1717
    let private publishTests
        runnerType
        (resultsFiles: string seq)
        (mergeResults: bool)
        (platform: string)
        (config: string)
        (runTitle: string)
        (publishRunAttachments: bool)
        =
        write
            "results.publish"
            [ yield "type", runnerType
              if mergeResults then
                  yield "mergeResults", "true"
              if String.isNotNullOrEmpty platform then
                  yield "platform", platform
              if String.isNotNullOrEmpty config then
                  yield "config", config
              if String.isNotNullOrEmpty runTitle then
                  yield "runTitle", runTitle
              if publishRunAttachments then
                  yield "publishRunAttachments", "true"
              if not (Seq.isEmpty resultsFiles) then
                  yield "resultFiles", resultsFiles |> Seq.map Path.GetFullPath |> seqToPropValue
              yield "testRunSystem", "VSTSTask" ]
            ""

    type internal LogDetailState =
        | Unknown
        | Initialized
        | InProgress
        | Completed

    type internal LogDetailResult =
        | Succeeded
        | SucceededWithIssues
        | Failed
        | Cancelled
        | Skipped

    type internal LogDetailData =
        { id: Guid
          parentId: Guid option
          typ: string option
          name: string option
          order: int option
          startTime: DateTime option
          finishTime: DateTime option
          progress: int option
          state: LogDetailState option
          result: LogDetailResult option
          message: string }

    let internal logDetailRaw logDetailData =
        let parameters =
            toList "id" (Some(string logDetailData.id))
            @ toList "parentid" (logDetailData.parentId |> Option.map string)
              @ toList "type" logDetailData.typ
                @ toList "name" logDetailData.name
                  @ toList "order" (logDetailData.order |> Option.map string)
                    @ toList "starttime" (logDetailData.startTime |> Option.map string)
                      @ toList "finishtime" (logDetailData.finishTime |> Option.map string)
                        @ toList "progress" (logDetailData.progress |> Option.map string)
                          @ toList "state" (logDetailData.state |> Option.map string)
                            @ toList "result" (logDetailData.result |> Option.map string)

        write "task.logdetail" parameters logDetailData.message

    let internal createLogDetail id parentId typ name order message =
        logDetailRaw
            { id = id
              parentId = parentId
              typ = Some typ
              name = Some name
              order = Some order
              startTime = None
              finishTime = None
              progress = None
              state = None
              result = None
              message = message }

    /// <summary>
    /// Log a message with in progress details
    /// </summary>
    let setLogDetailProgress id progress =
        logDetailRaw
            { id = id
              parentId = None
              typ = None
              name = None
              order = None
              startTime = None
              finishTime = None
              progress = Some progress
              state = Some InProgress
              result = None
              message = "Updating progress" }

    /// Log a message with completed details
    let internal setLogDetailFinished id result =
        logDetailRaw
            { id = id
              parentId = None
              typ = None
              name = None
              order = None
              startTime = None
              finishTime = None
              progress = None
              state = Some Completed
              result = (Some result)
              message = "Setting logdetail to finished." }

    /// <summary>
    /// Access (secret) build variables
    /// </summary>
    let variables = Vault.fromEnvironmentVariable "FAKE_VSTS_VAULT_VARIABLES"

    /// <summary>
    /// Defined the supported build reasons that cause the build to run.
    /// </summary>
    type BuildReason =
        /// A user manually queued the build.
        | Manual
        /// Continuous integration (CI) triggered by a Git push or a TFVC check-in.
        | IndividualCI
        /// Continuous integration (CI) triggered by a Git push or a TFVC check-in, and the Batch changes was selected.
        | BatchedCI
        /// Scheduled trigger.
        | Schedule
        /// A user manually queued the build of a specific TFVC shelveset.
        | ValidateShelveset
        /// Gated check-in trigger.
        | CheckInShelvset
        /// The build was triggered by a Git branch policy that requires a build.
        | PullRequest
        /// The build was started when another build completed.
        | BuildCompletion
        /// Covers any other cases not included in type.
        | Other of string

    /// <summary>
    /// Exported environment variables during build.
    /// See the <a href="https://help.github.com/en/actions/configuring-and-managing-workflows/using-environment-variables#default-environment-variables">
    /// official documentation</a> for details.
    /// </summary>
    type Environment =

        /// The branch of the triggering repo the build was queued for
        static member BuildSourceBranch = Environment.environVar "BUILD_SOURCEBRANCH"

        /// The name of the branch in the triggering repo the build was queued for
        static member BuildSourceBranchName = Environment.environVar "BUILD_SOURCEBRANCHNAME"

        /// The latest version control change of the triggering repo that is included in this build.
        static member BuildSourceVersion = Environment.environVar "BUILD_SOURCEVERSION"

        /// The ID of the record for the completed build.
        static member BuildId = Environment.environVar "BUILD_BUILDID"

        /// The event that caused the build to run. See BuildReason type for supported build reasons
        static member BuildReason =
            match Environment.environVar "BUILD_REASON" with
            | "Manual" -> BuildReason.Manual
            | "IndividualCI" -> BuildReason.IndividualCI
            | "BatchedCI" -> BuildReason.BatchedCI
            | "Schedule" -> BuildReason.Schedule
            | "ValidateShelveset" -> BuildReason.ValidateShelveset
            | "CheckInShelvset" -> BuildReason.CheckInShelvset
            | "PullRequest" -> BuildReason.PullRequest
            | "BuildCompletion" -> BuildReason.BuildCompletion
            | s -> BuildReason.Other s

        /// If the pull request is from a fork of the repository, this variable is set to True. Otherwise, it is set to False.
        static member SystemPullRequestIsFork =
            let s = Environment.environVar "SYSTEM_PULLREQUEST_ISFORK"

            if String.IsNullOrEmpty s then
                None
            else
                match bool.TryParse(s) with
                | true, v -> Some v
                | _ -> None

        /// The ID of the pull request that caused this build. For example: 17
        static member SystemPullRequestPullRequestId =
            Environment.environVar "SYSTEM_PULLREQUEST_PULLREQUESTID"

        /// The branch that is being reviewed in a pull request. For example: refs/heads/users/raisa/new-feature for Azure Repos.
        static member SystemPullRequestSourceBranch =
            Environment.environVar "SYSTEM_PULLREQUEST_SOURCEBRANCH"

        /// The URL to the repo that contains the pull request. For example: https://dev.azure.com/ouraccount/_git/OurProject.
        static member SystemPullRequestSourceRepositoryURI =
            Environment.environVar "SYSTEM_PULLREQUEST_SOURCEREPOSITORYURI"

        /// The branch that is the target of a pull request. For example: refs/heads/main when your repository is in Azure Repos and main when your repository is in GitHub.
        static member SystemPullRequestTargetBranch =
            Environment.environVar "SYSTEM_PULLREQUEST_TARGETBRANCH"

    let private publishArtifactIfOk artifactFolder artifactName path =
        let pushAnyWay = Environment.environVarAsBoolOrDefault "FAKE_VSO_PUSH_ALWAYS" false
        let canPush =
            match Environment.SystemPullRequestIsFork with
            | Some true when Environment.BuildReason = BuildReason.PullRequest -> false
            | _ -> true
        if pushAnyWay || canPush then
            publishArtifact artifactFolder artifactName path
        else
            logIssue true None None None None 
                (sprintf "Cannot publish artifact '%s' in PR because of https://developercommunity.visualstudio.com/content/problem/350007/build-from-github-pr-fork-error-tf400813-the-user-1.html. You can set FAKE_VSO_PUSH_ALWAYS to true in order to try to push anyway (when the bug has been fixed)."
                    path)

    /// <summary>
    /// Implements a TraceListener for Azure DevOps (previously VSTS) / Team Foundation build servers.
    /// </summary>
    /// [omit]
    type internal TeamFoundationTraceListener() =
        let mutable openTags = System.Threading.AsyncLocal<_>()
        do openTags.Value <- []
        let mutable order = 1

        interface ITraceListener with
            /// Writes the given message to the Console.
            member __.Write msg =
                let color = ConsoleWriter.colorMap msg
                let writeConsole = ConsoleWriter.write

                match msg with
                | TraceData.ErrorMessage text -> logIssue false None None None None text
                | TraceData.ImportantMessage text -> logIssue true None None None None text
                | TraceData.LogMessage (text, newLine)
                | TraceData.TraceMessage (text, newLine) -> writeConsole false color newLine text
                | TraceData.OpenTag (tag, descr) ->
                    let id = Guid.NewGuid()

                    let parentId =
                        match openTags.Value with
                        | [] -> None
                        | (_, id) :: _ -> Some id

                    openTags.Value <- (tag, id) :: openTags.Value
                    let order = System.Threading.Interlocked.Increment(&order)

                    match descr with
                    | Some d -> createLogDetail id parentId tag.Type tag.Name order d
                    | _ -> createLogDetail id parentId tag.Type tag.Name order null
                | TraceData.CloseTag (tag, time, state) ->
                    ignore time

                    let id, rest =
                        match openTags.Value with
                        | [] -> failwithf "Cannot close tag, as it was not opened before! (Expected %A)" tag
                        | (savedTag, id) :: rest ->
                            ignore savedTag // TODO: Check if tag = savedTag
                            id, rest

                    openTags.Value <- rest

                    let result =
                        match state with
                        | TagStatus.Warning -> LogDetailResult.SucceededWithIssues
                        | TagStatus.Failed -> LogDetailResult.Failed
                        | TagStatus.Success -> LogDetailResult.Succeeded

                    setLogDetailFinished id result
                | TraceData.BuildState (state, _) ->
                    let vsoState, msg =
                        match state with
                        | TagStatus.Success -> "Succeeded", "OK"
                        | TagStatus.Warning -> "SucceededWithIssues", "WARN"
                        | TagStatus.Failed -> "Failed", "ERROR"

                    setBuildState vsoState msg
                | TraceData.ImportData (ImportData.Junit _, path) -> publishTests "JUnit" [ path ] false "" "" "" true
                | TraceData.ImportData (ImportData.Nunit _, path) -> publishTests "NUnit" [ path ] false "" "" "" true
                | TraceData.ImportData (ImportData.Mstest _, path) -> publishTests "VSTest" [ path ] false "" "" "" true
                | TraceData.ImportData (ImportData.Xunit _, path) -> publishTests "XUnit" [ path ] false "" "" "" true
                | TraceData.ImportData (ImportData.BuildArtifactWithName name, path) ->
                    publishArtifactIfOk name (Some name) path
                | TraceData.ImportData (typ, path) -> publishArtifactIfOk typ.Name (Some "fake-artifacts") path
                | TraceData.TestOutput (test, out, err) ->
                    writeConsole false color true (sprintf "Test '%s' output:\n\tOutput: %s\n\tError: %s" test out err)
                | TraceData.BuildNumber number -> setBuildNumber number
                | TraceData.TestStatus (test, status) ->
                    writeConsole false color true (sprintf "Test '%s' status: %A" test status)

    /// [omit]
    let defaultTraceListener = TeamFoundationTraceListener() :> ITraceListener

    /// [omit]
    let detect () =
        BuildServer.buildServer = TeamFoundation

    /// [omit]
    let install (force: bool) =
        if not (detect ()) then
            failwithf "Cannot run 'install()' on a non-TeamFoundation environment"

        if force || not (CoreTracing.areListenersSet ()) then
            CoreTracing.setTraceListeners [ defaultTraceListener ]

        ()

    /// [omit]
    let Installer =
        { new BuildServerInstaller() with
            member __.Install() = install false
            member __.Detect() = detect () }
