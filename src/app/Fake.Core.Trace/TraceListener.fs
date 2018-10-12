/// Defines default listeners for build output traces
namespace Fake.Core

open System

/// Note: Adding new cases to this type is not considered a breaking change!
/// Please consider not using a match on this type in code external to the fake repository.
[<RequireQualifiedAccess>]
type KnownTags =
    | Task of name:string
    | Target of name:string
    | FinalTarget of name:string
    | FailureTarget of name:string
    | Compilation of compiler:string
    | TestSuite of suiteName:string
    | Test of testName:string
    | Other of typeDef:string * name:string
    member x.Name =
        match x with
        | Task n
        | Target n
        | FinalTarget n
        | FailureTarget n
        | Compilation n
        | TestSuite n
        | Test n
        | Other (_, n) -> n
    member x.Type =
        match x with
        | Task _ -> "task"
        | Target _ -> "target"
        | FinalTarget _ -> "final target"
        | FailureTarget _ -> "failure target"
        | Compilation _ -> "compilation"
        | TestSuite _ -> "testsuite"
        | Test _ -> "test"
        | Other (t, _) -> t

/// Note: Adding new cases to this type is not considered a breaking change!
/// Please consider not using a match on this type in code external to the fake repository.
[<RequireQualifiedAccess>]
type DotNetCoverageTool =
    | DotCover
    | PartCover
    | NCover
    | NCover3
    override x.ToString() =
        match x with
        | DotCover -> "dotcover"
        | PartCover -> "partcover"
        | NCover -> "ncover"
        | NCover3 -> "ncover3"

/// Note: Adding new cases to this type is not considered a breaking change!
/// Please consider not using a match on this type in code external to the fake repository.
[<RequireQualifiedAccess>]
type NunitDataVersion =
    | Nunit
    | Nunit3

/// Note: Adding new cases to this type is not considered a breaking change!
/// Please consider not using a match on this type in code external to the fake repository.
[<RequireQualifiedAccess>]
type ImportData =
    | BuildArtifact
    | BuildArtifactWithName of artifactName:string
    | DotNetCoverage of DotNetCoverageTool
    | DotNetDupFinder
    | PmdCpd
    | Pmd
    | FxCop
    | ReSharperInspectCode
    | Jslint
    | FindBugs
    | Checkstyle
    | Gtest
    | Mstest
    | Surefire
    | Junit
    | Xunit
    | Nunit of NunitDataVersion
    member x.Name =
        match x with
        | BuildArtifact -> "buildArtifact"
        // Some build servers like TFS allow to group artifacts by name.
        | BuildArtifactWithName _ -> "buildArtifactWithName"
        | DotNetCoverage _ -> "dotNetCoverage"
        | DotNetDupFinder -> "DotNetDupFinder"
        | PmdCpd -> "pmdCpd"
        | Pmd -> "pmd"
        | ReSharperInspectCode -> "ReSharperInspectCode"
        | Jslint -> "jslint"
        | FindBugs -> "findBugs"
        | Checkstyle -> "checkstyle"
        | Gtest -> "gtest"
        | Mstest -> "mstest"
        | Surefire -> "surefire"
        | Junit -> "junit"
        | FxCop -> "FxCop"
        | Xunit -> "xunit"
        | Nunit NunitDataVersion.Nunit -> "nunit"
        | Nunit NunitDataVersion.Nunit3 -> "nunit3"
    override x.ToString() =
        match x with
        | BuildArtifactWithName name -> sprintf "buildArtifact (%s)" name
        | DotNetCoverage tool -> sprintf "dotNetCoverage (%O)" tool
        | _ -> x.Name

/// Note: Adding new cases to this type is not considered a breaking change!
/// Please consider not using a match on this type in code external to the fake repository.
[<RequireQualifiedAccess>]
type TestStatus =
    | Ignored of message:string
    | Failed of message:string * details:string * expectedActual:(string * string) option

module TestStatus =
    let inline mapMessage f (t:TestStatus) =
        match t with
        | TestStatus.Failed (message, details, Some (expected, actual)) ->
            TestStatus.Failed (f message, f details, Some (f expected, f actual))
        | TestStatus.Failed (message, details, None) ->
            TestStatus.Failed (f message, f details, None)
        | _ -> t

/// Note: Adding new cases to this type is not considered a breaking change!
/// Please consider not using a match on this type in code external to the fake repository.
[<RequireQualifiedAccess>]
type TagStatus =
    | Success
    | Warning
    | Failed

/// Defines Tracing information for TraceListeners
/// Note: Adding new cases to this type is not considered a breaking change!
/// Please consider not using a match on this type in code external to the fake repository.
[<RequireQualifiedAccess>]
type TraceData =
    | ImportData of typ:ImportData * path:string
    | BuildNumber of text:string
    | ImportantMessage of text:string
    | ErrorMessage of text:string
    | LogMessage of text:string * newLine:bool
    | TraceMessage of text:string * newLine:bool
    /// Happens when a tag (Task, Target, Test, ...) has started.
    | OpenTag of KnownTags * description:string option
    | TestStatus of testName:string * status:TestStatus
    | TestOutput of testName:string * out:string * err:string
    | CloseTag of KnownTags * time:TimeSpan * TagStatus
    | BuildState of TagStatus * string option
    member x.NewLine =
        match x with
        | ImportantMessage _
        | ErrorMessage _ -> Some true
        | LogMessage (_, newLine)
        | TraceMessage (_, newLine) -> Some newLine
        | BuildNumber _
        | TestStatus _
        | TestOutput _
        | ImportData _
        | OpenTag _
        | BuildState _
        | CloseTag _ -> None
    member x.Message =
        match x with
        | ImportantMessage text
        | ErrorMessage text
        | LogMessage (text, _)
        | TraceMessage (text, _)
        | BuildState (_, Some text) -> Some text
        | BuildNumber _
        | TestStatus _
        | TestOutput _
        | ImportData _
        | OpenTag _
        | BuildState _
        | CloseTag _ -> None

module TraceData =
    let inline mapKnownTags f (t:KnownTags) = 
        match t with
        | KnownTags.Task tag -> KnownTags.Task(f tag)
        | KnownTags.Target tag -> KnownTags.Target(f tag)
        | KnownTags.FinalTarget tag -> KnownTags.FinalTarget(f tag)
        | KnownTags.FailureTarget tag -> KnownTags.FailureTarget(f tag)
        | _ -> t

    let inline mapMessage f (t:TraceData) =
        match t with
        | TraceData.ImportantMessage text -> TraceData.ImportantMessage (f text)
        | TraceData.ErrorMessage text -> TraceData.ErrorMessage (f text)
        | TraceData.LogMessage (text, d) -> TraceData.LogMessage (f text, d)
        | TraceData.TraceMessage (text, d) -> TraceData.TraceMessage (f text, d)
        | TraceData.TestStatus (testName,status) -> TraceData.TestStatus(testName, TestStatus.mapMessage f status)
        | TraceData.TestOutput (testName,out,err) -> TraceData.TestOutput (testName,f out,f err)
        | TraceData.OpenTag(tag, Some d) -> TraceData.OpenTag((mapKnownTags f tag), Some(f d))
        | TraceData.OpenTag(tag, None) -> TraceData.OpenTag((mapKnownTags f tag), None)
        | TraceData.CloseTag(tag, time, status) -> TraceData.CloseTag((mapKnownTags f tag), time, status)        
        | TraceData.BuildState(tag, Some message) -> TraceData.BuildState(tag, Some(f message))    
        | _ -> t

    let internal repl (oldStr:string) (repl:string) (s:string) =
        s.Replace(oldStr, repl)
    let replace oldString replacement (t:TraceData) =
        mapMessage (repl oldString replacement) t

/// Defines a TraceListener interface
/// Note: Please contribute implementations to the fake repository, as external implementations are not supported.
type ITraceListener =
    abstract Write : TraceData -> unit

module ConsoleWriter =

    let write toStdErr color newLine text =
        let curColor = Console.ForegroundColor
        try
          if curColor <> color then Console.ForegroundColor <- color
          let printer =
            match toStdErr, newLine with
            | true, true -> eprintfn
            | true, false -> eprintf
            | false, true -> printfn
            | false, false -> printf
          printer "%s" text
        finally
          if curColor <> color then Console.ForegroundColor <- curColor

    let writeAnsiColor toStdErr color newLine text =
        let printer =
            match toStdErr, newLine with
            | true, true -> eprintfn
            | true, false -> eprintf
            | false, true -> printfn
            | false, false -> printf
        let colorCode = function
            | ConsoleColor.Black -> [30]
            | ConsoleColor.Blue -> [34]
            | ConsoleColor.Cyan -> [36]
            | ConsoleColor.Gray -> [37;2]
            | ConsoleColor.Green -> [32]
            | ConsoleColor.Magenta -> [35]
            | ConsoleColor.Red -> [31]
            | ConsoleColor.White -> [37]
            | ConsoleColor.Yellow -> [33]
            | ConsoleColor.DarkBlue -> [34;2]
            | ConsoleColor.DarkCyan -> [36;2]
            | ConsoleColor.DarkGray -> [37;2]
            | ConsoleColor.DarkGreen -> [32;2]
            | ConsoleColor.DarkMagenta -> [35;2]
            | ConsoleColor.DarkRed -> [31;2]
            | ConsoleColor.DarkYellow -> [33;2]
            | _ -> [39]

        let codeStr =
            colorCode color
            |> List.map (sprintf "%i")
            |> String.concat ";"

        printer "\x1b[%sm%s\x1b[0m" codeStr text

    /// A default color map which maps TracePriorities to ConsoleColors
    let colorMap traceData =
        match traceData with
        | TraceData.ImportantMessage _ -> ConsoleColor.Yellow
        | TraceData.ErrorMessage _ -> ConsoleColor.Red
        | TraceData.LogMessage _ -> ConsoleColor.Gray
        | TraceData.TraceMessage _ -> ConsoleColor.Green
        | _ -> ConsoleColor.Gray

/// Implements a TraceListener for System.Console.
/// ## Parameters
///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
type ConsoleTraceListener(importantMessagesToStdErr, colorMap, ansiColor) =
    interface ITraceListener with
        /// Writes the given message to the Console.
        member __.Write msg =
            let color = colorMap msg
            let write = if ansiColor then ConsoleWriter.writeAnsiColor else ConsoleWriter.write
            match msg with
            | TraceData.ImportantMessage text | TraceData.ErrorMessage text ->
                write importantMessagesToStdErr color true text
            | TraceData.LogMessage(text, newLine) | TraceData.TraceMessage(text, newLine) ->
                write false color newLine text
            | TraceData.OpenTag(KnownTags.Target _ as tag, description)
            | TraceData.OpenTag(KnownTags.FailureTarget _ as tag, description)
            | TraceData.OpenTag(KnownTags.FinalTarget _ as tag, description) ->
                let color2 = colorMap (TraceData.TraceMessage("", true))
                match description with
                | Some d -> write false color2 true (sprintf "Starting %s '%s': %s" tag.Type tag.Name d)
                | _ -> write false color2 true (sprintf "Starting %s '%s'" tag.Type tag.Name)                
            | TraceData.OpenTag (tag, description) ->
                match description with
                | Some d -> write false color true (sprintf "Starting %s '%s': %s" tag.Type tag.Name d)
                | _ -> write false color true (sprintf "Starting %s '%s'" tag.Type tag.Name)                
            | TraceData.CloseTag (tag, time, status) ->
                write false color true (sprintf "Finished (%A) '%s' in %O" status tag.Name time)
            | TraceData.ImportData (typ, path) ->
                write false color true (sprintf "Import data '%O': %s" typ path)
            | TraceData.BuildState (state, None) ->
                write false color true (sprintf "Changing BuildState to: %A" state)
            | TraceData.BuildState (state, Some message) ->
                write false color true (sprintf "Changing BuildState to: %A - %s" state message)            
            | TraceData.TestOutput (test, out, err) ->
                write false color true (sprintf "Test '%s' output:\n\tOutput: %s\n\tError: %s" test out err)
            | TraceData.BuildNumber number ->
                write false color true (sprintf "Build Number: %s" number)
            | TraceData.TestStatus (test, status) ->
                write false color true (sprintf "Test '%s' status: %A" test status)


type TraceSecret =
    { Value : string; Replacement : string }

module TraceSecrets =
    let private traceSecretsVar = "Fake.Core.Trace.TraceSecrets"
    let private getTraceSecrets, _, (setTraceSecrets:TraceSecret list -> unit) =
        Fake.Core.FakeVar.defineOrNone traceSecretsVar

    let getAll () =
        match getTraceSecrets() with
        | Some secrets -> secrets
        | None -> []

    let register replacement secret =
        getAll()
        |> List.filter (fun s -> s.Value <> secret)
        |> fun l -> { Value = secret; Replacement = replacement } :: l
        |> fun l -> setTraceSecrets l

    let guardMessage (s:string) =
        getAll()
        |> Seq.fold (fun state secret -> TraceData.repl secret.Value secret.Replacement state) s

module CoreTracing =
    // If we write the stderr on those build servers the build will fail.
    let importantMessagesToStdErr =
        let buildServer = BuildServer.buildServer
        buildServer <> CCNet && buildServer <> AppVeyor && buildServer <> TeamCity && buildServer <> TeamFoundation

    /// The default TraceListener for Console.
    let defaultConsoleTraceListener  =
      ConsoleTraceListener(importantMessagesToStdErr, ConsoleWriter.colorMap, false) :> ITraceListener


    /// A List with all registered listeners

    let private traceListenersVar = "Fake.Core.Trace.TraceListeners"
    let private getTraceListeners, _, (setTraceListenersPrivate:ITraceListener list -> unit) =
        Fake.Core.FakeVar.defineOrNone traceListenersVar

    let areListenersSet () =
        match getTraceListeners() with
        | None -> false
        | Some _ -> true


    // register listeners
    let getListeners () =
        match getTraceListeners() with
        | None -> [defaultConsoleTraceListener]
        | Some t -> t

    let setTraceListeners l = setTraceListenersPrivate l
    let addListener l = setTraceListenersPrivate (l :: getListeners())

    let ensureConsoleListener () =
        let current = getListeners()
        if current |> Seq.contains defaultConsoleTraceListener |> not then
            setTraceListenersPrivate (defaultConsoleTraceListener :: current)

    /// Allows to post messages to all trace listeners
    let postMessage x =
        let msg =
            TraceSecrets.getAll()
            |> Seq.fold (fun state secret -> TraceData.replace secret.Value secret.Replacement state) x

        getListeners() |> Seq.iter (fun listener -> listener.Write msg)
