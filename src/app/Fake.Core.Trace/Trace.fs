/// This module contains function which allow to trace build output
[<RequireQualifiedAccess>]
module Fake.Core.Trace

open Fake.Core

open System
open System.Reflection
open System.Threading

type FAKEException(msg) =
    inherit System.Exception(msg)


let private openTags = new ThreadLocal<list<System.Diagnostics.Stopwatch * KnownTags>>(fun _ -> [])

let mutable private verbose = Environment.environVarAsBoolOrDefault "FAKE_COREFX_VERBOSE" false
let mutable private hintPrinted = false
let setVerbose() = verbose <- true


/// Logs the specified string        
let log message = TraceData.LogMessage(message, true) |> CoreTracing.postMessage

let isVerbose (printHint) =
    if printHint && not hintPrinted then
        hintPrinted <- true
        if not verbose then
            let hint = "Note: To further diagnose the following problem you can increase verbosity of the fake modules by setting the 'FAKE_COREFX_VERBOSE' environment variable to 'true' or by using 'Trace.setVerbose()' in your script."
            log hint
    verbose

/// Logs the specified message
let logfn fmt = Printf.ksprintf log fmt

/// Logs the specified message (without line break)
let logf fmt = Printf.ksprintf (fun text -> CoreTracing.postMessage (TraceData.LogMessage(text, false))) fmt

/// Logs the specified string if the verbose mode is activated.
let logVerbosefn fmt = 
    Printf.ksprintf (if isVerbose(false) then log
                     else ignore) fmt

/// Writes a trace to the command line (in green)
let trace message = CoreTracing.postMessage (TraceData.TraceMessage(message, true))

/// Writes a message to the command line (in green)
let tracefn fmt = Printf.ksprintf trace fmt

/// Writes a message to the command line (in green) and without a line break
let tracef fmt = Printf.ksprintf (fun text -> CoreTracing.postMessage (TraceData.TraceMessage(text, false))) fmt

/// Writes a trace to the command line (in green) if the verbose mode is activated.
let traceVerbose s = 
    if isVerbose(false) then trace s

/// Writes a trace to stderr (in yellow)  
let traceImportant text = CoreTracing.postMessage (TraceData.ImportantMessage text)

/// Writes a message to stderr (in yellow) 
let traceImportantfn fmt = Printf.ksprintf traceImportant fmt

/// Writes a trace to the command line (in yellow)
let traceFAKE fmt = Printf.ksprintf (TraceData.ImportantMessage >> CoreTracing.postMessage) fmt

/// Traces an error (in red)
let traceError error = CoreTracing.postMessage (TraceData.ErrorMessage error)

/// Writes an error message to stderr (in red) 
let traceErrorfn fmt = Printf.ksprintf traceError fmt

open Microsoft.FSharp.Core.Printf

/// Converts an exception and its inner exceptions to a nice string.
let exceptionAndInnersToString (ex:Exception) =
    let sb = Text.StringBuilder()
    let delimeter = String.replicate 50 "*"
    let nl = Environment.NewLine
    let rec printException (e:Exception) count =
        if (e :? TargetException && not (isNull e.InnerException))
        then printException (e.InnerException) count
        else
            if (count = 1) then bprintf sb "Exception Message:%s%s%s" e.Message nl delimeter
            else bprintf sb "%s%s%d)Exception Message:%s%s%s" nl nl count e.Message nl delimeter
            bprintf sb "%sType: %s" nl (e.GetType().FullName)
            // Loop through the public properties of the exception object
            // and record their values.
            e.GetType().GetTypeInfo().GetProperties()
            |> Array.iter (fun p ->
                // Do not log information for the InnerException or StackTrace.
                // This information is captured later in the process.
                if (p.Name <> "InnerException" && p.Name <> "StackTrace" &&
                    p.Name <> "Message" && p.Name <> "Data") then
                    try
                        let value = p.GetValue(e, null)
                        if (not (isNull value))
                        then bprintf sb "%s%s: %s" nl p.Name (value.ToString())
                    with
                    | e2 -> bprintf sb "%s%s: %s" nl p.Name e2.Message
            )
            if not (isNull e.StackTrace) then
                bprintf sb "%s%sStackTrace%s%s%s" nl nl nl delimeter nl
                bprintf sb "%s%s" nl e.StackTrace
            if not (isNull e.InnerException)
            then printException e.InnerException (count+1)
    printException ex 1
    sb.ToString()

/// Traces an exception details (in red)
let traceException (ex:Exception) = exceptionAndInnersToString ex |> traceError

/// Traces the EnvironmentVariables
let traceEnvironmentVariables() = 
#if !DOTNETCORE
    // [ EnvironTarget.Machine; EnvironTarget.Process; EnvironTarget.User ] 
    // |> Seq.iter (fun mode -> 
    //        tracefn "Environment-Settings (%A):" mode
    //        environVars mode |> Seq.iter (tracefn "  %A"))
    tracefn "Environment-Settings :\n" 
    Environment.environVars () |> Seq.iter (fun (a,b) ->
        tracefn "  %A - %A" a b 
    )

#else
    tracefn "Environment-Settings (%A):" "Process"
    Environment.environVars () |> Seq.iter (tracefn "  %A")
#endif

/// Traces a line
let traceLine() = trace "---------------------------------------------------------------------"

/// Traces a header
let traceHeader name = 
    trace ""
    traceLine()
    trace name
    traceLine()

/// Puts an opening tag on the internal tag stack
let openTagUnsafe tag description =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    openTags.Value <- (sw, tag) :: openTags.Value
    TraceData.OpenTag(tag, if System.String.IsNullOrEmpty description then None else Some description) |> CoreTracing.postMessage

type ISafeDisposable =
    inherit System.IDisposable
    abstract MarkSuccess : unit -> unit
    abstract MarkFailed : unit -> unit

let private asSafeDisposable f =
    let mutable state = TagStatus.Failed
    let mutable isDisposed = false
    { new ISafeDisposable with
        member __.MarkSuccess () = state <- TagStatus.Success
        member __.MarkFailed () = state <- TagStatus.Failed
        member __.Dispose () =
            if not isDisposed then
              isDisposed <- true
              f state }

/// Puts an opening tag on the internal tag stack
[<System.Obsolete("Consider using traceTag instead and 'use' to properly call closeTag in case of exceptions. To remove this warning use 'openTagUnsafe'.")>]
let openTag tag description = openTagUnsafe tag description

/// Removes an opening tag from the internal tag stack
let closeTagUnsafeEx status tag =
    let time =
        match openTags.Value with
        | (sw, x) :: rest when x = tag -> 
            openTags.Value <- rest
            sw.Elapsed
        | _ -> failwithf "Invalid tag structure. Trying to close %A tag but stack is %A" tag openTags
    TraceData.CloseTag (tag, time, status) |> CoreTracing.postMessage

let closeTagUnsafe tag =
    closeTagUnsafeEx TagStatus.Success tag

/// Removes an opening tag from the internal tag stack
[<System.Obsolete("Consider using traceTag instead and 'use' to properly call closeTag in case of exceptions. To remove this warning use 'closeTagUnsafe'.")>]
let closeTag tag = closeTagUnsafe tag

let traceTag tag description =
    openTagUnsafe tag description
    asSafeDisposable (fun state -> closeTagUnsafeEx state tag)

let setBuildStateWithMessage tag message =
    TraceData.BuildState(tag, Some(message)) |> CoreTracing.postMessage

let setBuildState tag =
    TraceData.BuildState(tag, None) |> CoreTracing.postMessage

let testStatus testName testStatus =
    // TODO: Check if the given test is opened in openTags-stack?
    TraceData.TestStatus (testName, testStatus) |> CoreTracing.postMessage

let testOutput testName out err =
    // TODO: Check if the given test is opened in openTags-stack?
    TraceData.TestOutput (testName, out, err) |> CoreTracing.postMessage

let publish typ path =
    TraceData.ImportData (typ, path) |> CoreTracing.postMessage
let setBuildNumber number =
    TraceData.BuildNumber number |> CoreTracing.postMessage

let closeAllOpenTags() = Seq.iter (fun (_, tag) -> closeTagUnsafeEx TagStatus.Failed tag) openTags.Value

/// Traces the begin of a target
let traceStartTargetUnsafe name description (dependencyString:string) =
    openTagUnsafe (KnownTags.Target name) description

/// Traces the begin of a final target
let traceStartFinalTargetUnsafe name description (dependencyString:string) =
    openTagUnsafe (KnownTags.FinalTarget name) description

/// Traces the begin of a failure target
let traceStartFailureTargetUnsafe name description (dependencyString:string) =
    openTagUnsafe (KnownTags.FailureTarget name) description

/// Traces the begin of a target
[<System.Obsolete("Consider using traceTarget instead and 'use' to properly call traceEndTask in case of exceptions. To remove this warning use 'traceStartTargetUnsafe'.")>]
let traceStartTarget name description dependencyString =
    traceStartTargetUnsafe name description dependencyString

/// Traces the end of a target
let traceEndTargetUnsafeEx state name = 
    closeTagUnsafeEx state (KnownTags.Target name)

/// Traces the end of a final target
let traceEndFinalTargetUnsafeEx state name = 
    closeTagUnsafeEx state (KnownTags.FinalTarget name)

/// Traces the end of a failure target
let traceEndFailureTargetUnsafeEx state name = 
    closeTagUnsafeEx state (KnownTags.FailureTarget name)

/// Traces the end of a target
let traceEndTargetUnsafe name = 
    traceEndTargetUnsafeEx TagStatus.Success name

/// Traces the end of a target
[<System.Obsolete("Consider using traceTarget instead and 'use' to properly call traceEndTask in case of exceptions. To remove this warning use 'traceEndTargetUnsafe'.")>]
let traceEndTarget name = traceEndTargetUnsafe name

let traceTarget name description dependencyString =
    traceStartTargetUnsafe name description dependencyString
    asSafeDisposable (fun state -> traceEndTargetUnsafeEx state name)

let traceFinalTarget name description dependencyString =
    traceStartFinalTargetUnsafe name description dependencyString
    asSafeDisposable (fun state -> traceEndFinalTargetUnsafeEx state name)
    
let traceFailureTarget name description dependencyString =
    traceStartFailureTargetUnsafe name description dependencyString
    asSafeDisposable (fun state -> traceEndFailureTargetUnsafeEx state name)

/// Traces the begin of a task
let traceStartTaskUnsafe task description = 
    openTagUnsafe (KnownTags.Task task) description

/// Traces the begin of a task
[<System.Obsolete("Consider using traceTask instead and 'use' to properly call traceEndTask in case of exceptions. To remove this warning use 'traceStartTaskUnsafe'.")>]
let traceStartTask task description = traceStartTaskUnsafe task description

/// Traces the end of a task
let traceEndTaskUnsafeEx state task = 
    closeTagUnsafeEx state (KnownTags.Task task)

/// Traces the end of a task
let traceEndTaskUnsafe task = traceEndTaskUnsafeEx TagStatus.Success task
   
/// Traces the end of a task
[<System.Obsolete("Consider using traceTask instead and 'use' to properly call traceEndTask in case of exceptions. To remove this warning use 'traceEndTask'.")>]
let traceEndTask task = traceEndTaskUnsafe task

/// Wrap functions in a 'use' of this function     
let traceTask name description =
    traceStartTaskUnsafe name description
    asSafeDisposable (fun state -> traceEndTaskUnsafeEx state name)

/// Allows automatic or manual tracing around a function being run
/// If in automatic success mode and no exception is thrown then trace is marked as success
/// Any exception thrown will result in a mark failed and exception re-thrown 
let inline useWith automaticSuccess func (trace:ISafeDisposable) =
    try
        try 
            let result = func trace
            if automaticSuccess then trace.MarkSuccess()
            result
        with _ -> 
            trace.MarkFailed()
            reraise()
    finally 
        trace.Dispose()

open System.Diagnostics
#if DOTNETCORE
type EventLogEntryType =
  | Error
  | Information
  | Warning
  | Other
#endif
/// Traces the message to the console
let logToConsole (msg, eventLogEntry : EventLogEntryType) =
    let safeMessage = TraceSecrets.guardMessage msg
    match eventLogEntry with
    | EventLogEntryType.Error -> TraceData.ErrorMessage safeMessage
    | EventLogEntryType.Information -> TraceData.TraceMessage(safeMessage, true)
    | EventLogEntryType.Warning -> TraceData.ImportantMessage safeMessage
    | _ -> TraceData.LogMessage(safeMessage, true)
    |> CoreTracing.defaultConsoleTraceListener.Write

/// Logs the given files with the message.
let logItems message items = items |> Seq.iter (log << sprintf "%s%s" message)
