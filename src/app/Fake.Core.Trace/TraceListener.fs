/// Defines default listeners for build output traces
namespace Fake.Core

open Fake.Core.BuildServer

open System

type KnownTags =
    | Task of name:string
    | Target of name:string
    | Compilation of compiler:string
    | TestSuite of suiteName:string
    | Test of testName:string
    | Other of typeDef:string * name:string
    member x.Name =
        match x with
        | Task n
        | Target n
        | Compilation n
        | TestSuite n
        | Test n
        | Other (_, n) -> n
    member x.Type =
        match x with
        | Task _ -> "task"
        | Target _ -> "target"
        | Compilation _ -> "compilation"
        | TestSuite _ -> "testsuite"
        | Test _ -> "test"
        | Other (t, _) -> t

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

type ImportData =
    | BuildArtifact
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
    | Nunit
    member x.Name =
        match x with
        | BuildArtifact -> "buildArtifact"
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
        | Nunit -> "nunit"
    override x.ToString() =
        match x with
        | DotNetCoverage tool -> sprintf "dotNetCoverage (%O)" tool
        | _ -> x.Name

type TestStatus =
    | Ignored of message:string
    | Failed of message:string * details:string * expectedActual:(string * string) option

module TestStatus =
    let inline mapMessage f (t:TestStatus) =
        match t with
        | Failed (message, details, Some (expected, actual)) ->
            Failed (f message, f details, Some (f expected, f actual))
        | Failed (message, details, None) ->
            Failed (f message, f details, None)
        | _ -> t        


/// Defines Tracing information for TraceListeners
type TraceData = 
    | ImportData of typ:ImportData * path:string
    | BuildNumber of text:string
    | ImportantMessage of text:string
    | ErrorMessage of text:string
    | LogMessage of text:string * newLine:bool
    | TraceMessage of text:string * newLine:bool
    | OpenTag of KnownTags * description:string
    | TestStatus of testName:string * status:TestStatus
    | TestOutput of testName:string * out:string * isStdErr:bool
    | CloseTag of KnownTags * time:TimeSpan
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
        | CloseTag _ -> None
    member x.Message =
        match x with
        | ImportantMessage text
        | ErrorMessage text
        | LogMessage (text, _)
        | TraceMessage (text, _) -> Some text
        | BuildNumber _
        | TestStatus _
        | TestOutput _
        | ImportData _
        | OpenTag _
        | CloseTag _ -> None

module TraceData =
    let inline mapMessage f (t:TraceData) =
        match t with
        | ImportantMessage text -> ImportantMessage (f text) 
        | ErrorMessage text -> ErrorMessage (f text)
        | LogMessage (text, d) -> LogMessage (f text, d)
        | TraceMessage (text, d) -> TraceMessage (f text, d)
        | TestStatus (testName,status) -> TestStatus(testName, TestStatus.mapMessage f status)
        | TestOutput (testName,out,isStdErr) -> TestOutput (testName,f out,isStdErr)
        | _ -> t

    let internal repl (oldStr:string) (repl:string) (s:string) =
        s.Replace(oldStr, repl)
    let replace oldString replacement (t:TraceData) =
        mapMessage (repl oldString replacement) t

/// Defines a TraceListener interface
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

    /// A default color map which maps TracePriorities to ConsoleColors
    let colorMap traceData = 
        match traceData with
        | ImportantMessage _ -> ConsoleColor.Yellow
        | ErrorMessage _ -> ConsoleColor.Red
        | LogMessage _ -> ConsoleColor.Gray
        | TraceMessage _ -> ConsoleColor.Green
        | _ -> ConsoleColor.Gray

/// Implements a TraceListener for System.Console.
/// ## Parameters
///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
type ConsoleTraceListener(importantMessagesToStdErr, colorMap) =
    interface ITraceListener with
        /// Writes the given message to the Console.
        member __.Write msg = 
            let color = colorMap msg
            match msg with
            | ImportantMessage text | ErrorMessage text ->
                ConsoleWriter.write importantMessagesToStdErr color true text
            | LogMessage(text, newLine) | TraceMessage(text, newLine) ->
                ConsoleWriter.write false color newLine text
            | OpenTag (tag, descr) ->
                ConsoleWriter.write false color true (sprintf "Starting %s '%s': %s" tag.Type tag.Name descr)
            | CloseTag (tag, time) ->
                ConsoleWriter.write false color true (sprintf "Finished '%s' in %O" tag.Name time)
            | ImportData (typ, path) ->
                ConsoleWriter.write false color true (sprintf "Import data '%O': %s" typ path)
            | BuildNumber _
            | TestOutput _
            | TestStatus _ -> ()

type TraceSecret =
    { Value : string; Replacement : string }

module TraceSecrets =
    let private traceSecretsVar = "Fake.Core.Trace.TraceSecrets"
    let private getTraceSecrets, _, (setTraceSecrets:TraceSecret list -> unit) = 
        Fake.Core.Context.fakeVar traceSecretsVar

    let getAll () =
        match getTraceSecrets() with
        | Some secrets -> secrets
        | None -> []

    let register replacement secret =
        setTraceSecrets ({ Value = secret; Replacement = replacement } :: getAll() |> List.filter (fun s -> s.Value <> secret))

    let guardMessage (s:string) =
        getAll()
        |> Seq.fold (fun state secret -> TraceData.repl secret.Value secret.Replacement state) s

module CoreTracing =
    // If we write the stderr on those build servers the build will fail.
    let importantMessagesToStdErr = buildServer <> CCNet && buildServer <> AppVeyor && buildServer <> TeamCity && buildServer <> TeamFoundation

    /// The default TraceListener for Console.
    let defaultConsoleTraceListener  =
      ConsoleTraceListener(importantMessagesToStdErr, ConsoleWriter.colorMap) :> ITraceListener


    /// A List with all registered listeners

    let private traceListenersVar = "Fake.Core.Trace.TraceListeners"
    let private getTraceListeners, _, (setTraceListenersPrivate:ITraceListener list -> unit) = 
        Fake.Core.Context.fakeVar traceListenersVar

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

    /// Allows to post messages to all trace listeners
    let postMessage x =
        let msg =
            TraceSecrets.getAll()
            |> Seq.fold (fun state secret -> TraceData.replace secret.Value secret.Replacement state) x

        getListeners() |> Seq.iter (fun listener -> listener.Write msg)
