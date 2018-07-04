/// Contains support for various build servers
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core
open Fake.IO

[<RequireQualifiedAccess>]
module TeamFoundation =
    // See https://github.com/Microsoft/vsts-tasks/blob/master/docs/authoring/commands.md
    
    let write action properties message =
        let ensureProp (s:string) =
            // TODO: Escaping!
            if s.Contains ";" || s.Contains "=" || s.Contains "]" then
                failwithf "property name or value cannot contain ';', '=' or ']'"
            s            
        let formattedProperties =
            let temp =
                properties  
                |> Seq.map (fun (prop, value) -> sprintf "%s=%s;" (ensureProp prop) (ensureProp value))
                |> String.separated ""
            if String.isNullOrWhiteSpace temp then "" else " " + temp
        sprintf "##vso[%s%s]%s" action formattedProperties message
        // printf is racing with others in parallel mode
        |> fun s -> System.Console.WriteLine("\n{0}", s)
        
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
        write "artifact.upload" parameters path

    let private setBuildNumber number =
        write "build.updatebuildnumber" [] number

    let setBuildState state message =
        write "task.complete" ["result", state] message

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

    type Environment =
        static member BuildSourceBranch = Environment.environVar "BUILD_SOURCEBRANCH"
        static member BuildSourceBranchName = Environment.environVar "BUILD_SOURCEBRANCHNAME"
        static member BuildSourceVersion = Environment.environVar "BUILD_SOURCEVERSION"
        static member BuildId = Environment.environVar "BUILD_BUILDID"

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
                | TraceData.ImportantMessage text | TraceData.ErrorMessage text ->
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
                | TraceData.BuildState state ->
                    let vsoState, msg =
                        match state with
                        | TagStatus.Success -> "Succeeded", "OK" 
                        | TagStatus.Warning -> "SucceededWithIssues", "WARN"
                        | TagStatus.Failed -> "Failed", "ERROR"
                    setBuildState vsoState msg
                | TraceData.ImportData (typ, path) ->
                    publishArtifact typ.Name (Some "fake-artifacts") path
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
