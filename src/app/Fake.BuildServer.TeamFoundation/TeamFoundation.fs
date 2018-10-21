/// Contains support for various build servers
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core
open Fake.IO

/// native support for Azure DevOps (previously VSTS) / Team Foundation Server specific APIs.
/// The general documentation on how to use CI server integration can be found [here](/buildserver.html)
/// 
/// ### Secret Variables
/// 
/// This CI server supports the concept of secret variables and uses the [Vault](/core-vault.html) to store them.
/// In order to access secret variables you need to use one of the fake 5 tasks from [vsts-fsharp](https://github.com/isaacabraham/vsts-fsharp).
/// 
/// #### Example implementation (supports runner and vault tasks)
///
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
///
[<RequireQualifiedAccess>]
module TeamFoundation =
    // See https://github.com/Microsoft/vsts-tasks/blob/master/docs/authoring/commands.md
    
    let write action properties message =
        // https://github.com/Microsoft/vsts-task-lib/blob/3a7905b99d698a535d9f5a477efc124894b8d2ae/node/taskcommand.ts
        let ensurePropVal (s:string) =
            s.Replace("\r", "%0D")
             .Replace("\n", "%0A")
             .Replace("]", "%5D")
             .Replace(";", "%3B")
        let ensurePropName (s:string)=
            if s.Contains ";" || s.Contains "=" || s.Contains "]" then
                failwithf "property name cannot contain ';', '=' or ']'"
            s
        let ensureMsg (s:string) =
            s.Replace("\r", "%0D").Replace("\n", "%0A");                  
        let formattedProperties =
            let temp =
                properties  
                |> Seq.map (fun (prop, value) -> sprintf "%s=%s;" (ensurePropName prop) (ensurePropVal value))
                |> String.separated ""
            if String.isNullOrWhiteSpace temp then "" else " " + temp
        sprintf "##vso[%s%s]%s" action formattedProperties message
        // printf is racing with others in parallel mode
        |> fun s -> System.Console.WriteLine("\n{0}", s)

    let private seqToPropValue (args:_ seq) = System.String.Join(",", args)
    let setVariable variableName value =
        write "task.setvariable" ["variable", variableName] value
        
    let private toType t o =
        o |> Option.map (fun value -> t, value)
    let private toList t o =
        o |> toType t |> Option.toList
    let logIssue isWarning sourcePath lineNumber columnNumber code message =

        let typ = if isWarning then "warning" else "error"
        let parameters = toList "type" (Some typ) @ toList "sourcepath" sourcePath @ toList "linenumber" lineNumber @ toList "columnnumber" columnNumber @ toList "code" code
        write "task.logissue" parameters message

    let private uploadFile path =
        write "task.uploadfile" [] path

    let private publishArtifact artifactFolder artifactName path =
        let parameters = ["containerfolder", artifactFolder] @ toList "artifactname" artifactName
        write "artifact.upload" parameters (Path.GetFullPath path)

    let private setBuildNumber number =
        write "build.updatebuildnumber" [] number

    let setBuildState state message =
        write "task.complete" ["result", state] message

    // undocumented API: https://github.com/Microsoft/vsts-task-lib/blob/3a7905b99d698a535d9f5a477efc124894b8d2ae/node/task.ts#L1717
    let private publishTests runnerType (resultsFiles:string seq) (mergeResults:bool) (platform:string) (config:string) (runTitle:string) (publishRunAttachments:bool) =
        write "results.publish"
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

    let internal logDetailRaw
        (id:Guid) (parentId:Guid option) typ name (order:int option) (startTime:DateTime option) (finishTime:DateTime option) 
        (progress:int option) (state:LogDetailState option) (result:LogDetailResult option) message =
        let parameters =
            toList "id" (Some (string id)) @ toList "parentid" (parentId |> Option.map string) @ toList "type" typ 
            @ toList "name" name @ toList "order" (order |> Option.map string)
            @ toList "starttime" (startTime |> Option.map string) @ toList "finishtime" (finishTime |> Option.map string)
            @ toList "progress" (progress |> Option.map string) @ toList "state" (state |> Option.map string)
            @ toList "result" (result |> Option.map string)
        write "task.logdetail" parameters message

    let internal createLogDetail id parentId typ name order message =
        logDetailRaw id parentId (Some typ) (Some name) (Some order) None None None None None message

    let setLogDetailProgress id progress =
        logDetailRaw id None None None None None None (Some progress) (Some InProgress) None "Updating progress"

    let internal setLogDetailFinished id result =
        logDetailRaw id None None None None None None None (Some Completed) (Some result) "Setting logdetail to finished."    

    /// Access (secret) build variables
    let variables = Vault.fromEnvironmentVariable "FAKE_VSTS_VAULT_VARIABLES"  

    type BuildReason =
        | Manual
        | IndividualCI
        | BatchedCI
        | Schedule
        | ValidateShelveset
        | CheckInShelvset
        | PullRequest
        | BuildCompletion
        | Other of string


    type Environment =
        static member BuildSourceBranch = Environment.environVar "BUILD_SOURCEBRANCH"
        static member BuildSourceBranchName = Environment.environVar "BUILD_SOURCEBRANCHNAME"
        static member BuildSourceVersion = Environment.environVar "BUILD_SOURCEVERSION"
        static member BuildId = Environment.environVar "BUILD_BUILDID"
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

        static member SystemPullRequestIsFork =
            let s = Environment.environVar "SYSTEM_PULLREQUEST_ISFORK"
            if String.IsNullOrEmpty s  then None
            else
                match bool.TryParse(s) with
                | true, v -> Some v
                | _ -> None
        static member SystemPullRequestPullRequestId = Environment.environVar "SYSTEM_PULLREQUEST_PULLREQUESTID"
        static member SystemPullRequestSourceBranch = Environment.environVar "SYSTEM_PULLREQUEST_SOURCEBRANCH"
        static member SystemPullRequestSourceRepositoryURI = Environment.environVar "SYSTEM_PULLREQUEST_SOURCEREPOSITORYURI"
        static member SystemPullRequestTargetBranch = Environment.environVar "SYSTEM_PULLREQUEST_TARGETBRANCH"


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

    /// Implements a TraceListener for TeamCity build servers.
    /// ## Parameters
    ///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
    ///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
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
                | TraceData.ErrorMessage text ->
                    logIssue false None None None None text
                | TraceData.ImportantMessage text ->
                    logIssue true None None None None text
                | TraceData.LogMessage(text, newLine) | TraceData.TraceMessage(text, newLine) ->
                    writeConsole false color newLine text
                | TraceData.OpenTag (tag, descr) ->
                    let id = Guid.NewGuid()
                    let parentId =
                        match openTags.Value with
                        | [] -> None
                        | (_, id) :: _ -> Some id
                    openTags.Value <- (tag,id) :: openTags.Value
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
                | TraceData.ImportData (ImportData.Junit _, path) ->
                    publishTests "JUnit" [path] false "" "" "" true
                | TraceData.ImportData (ImportData.Nunit _, path) ->
                    publishTests "NUnit" [path] false "" "" "" true
                | TraceData.ImportData (ImportData.Mstest _, path) ->
                    publishTests "VSTest" [path] false "" "" "" true
                | TraceData.ImportData (ImportData.Xunit _, path) ->
                    publishTests "XUnit" [path] false "" "" "" true
                | TraceData.ImportData (ImportData.BuildArtifactWithName name, path) ->
                    publishArtifactIfOk name (Some name) path
                | TraceData.ImportData (typ, path) ->
                    publishArtifactIfOk typ.Name (Some "fake-artifacts") path
                | TraceData.TestOutput (test, out, err) ->
                    writeConsole false color true (sprintf "Test '%s' output:\n\tOutput: %s\n\tError: %s" test out err)
                | TraceData.BuildNumber number ->
                    setBuildNumber number
                | TraceData.TestStatus (test, status) ->
                    writeConsole false color true (sprintf "Test '%s' status: %A" test status)

    let defaultTraceListener =
      TeamFoundationTraceListener() :> ITraceListener
    let detect () =
        BuildServer.buildServer = BuildServer.TeamFoundation
    let install(force:bool) =
        if not (detect()) then failwithf "Cannot run 'install()' on a non-TeamFoundation environment"
        if force || not (CoreTracing.areListenersSet()) then
            CoreTracing.setTraceListeners [defaultTraceListener]
        () 
    let Installer =
        { new BuildServerInstaller() with
            member __.Install () = install (false)
            member __.Detect () = detect() }
