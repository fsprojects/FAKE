/// This module contains function which allow to trace build output
module Fake.Core.Trace

open Fake.Core
open Fake.Core.Environment
open Fake.Core.BuildServer

open System
open System.Reflection
open System.Threading

type FAKEException(msg) =
    inherit System.Exception(msg)


let private openTags = new ThreadLocal<list<KnownTags>>(fun _ -> [])

/// Logs the specified string        
let log message = LogMessage(message, true) |> CoreTracing.postMessage

/// Logs the specified message
let logfn fmt = Printf.ksprintf log fmt

/// Logs the specified message (without line break)
let logf fmt = Printf.ksprintf (fun text -> CoreTracing.postMessage (LogMessage(text, false))) fmt

/// Logs the specified string if the verbose mode is activated.
let logVerbosefn fmt = 
    Printf.ksprintf (if verbose then log
                     else ignore) fmt

/// Writes a trace to the command line (in green)
let trace message = CoreTracing.postMessage (TraceMessage(message, true))

/// Writes a message to the command line (in green)
let tracefn fmt = Printf.ksprintf trace fmt

/// Writes a message to the command line (in green) and without a line break
let tracef fmt = Printf.ksprintf (fun text -> CoreTracing.postMessage (TraceMessage(text, false))) fmt

/// Writes a trace to the command line (in green) if the verbose mode is activated.
let traceVerbose s = 
    if verbose then trace s

/// Writes a trace to stderr (in yellow)  
let traceImportant text = CoreTracing.postMessage (ImportantMessage text)

/// Writes a trace to the command line (in yellow)
let traceFAKE fmt = Printf.ksprintf (ImportantMessage >> CoreTracing.postMessage) fmt

/// Traces an error (in red)
let traceError error = CoreTracing.postMessage (ErrorMessage error)

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
            e.GetType().GetProperties()
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
let TraceEnvironmentVariables() = 
#if !DOTNETCORE
    [ EnvironTarget.Machine; EnvironTarget.Process; EnvironTarget.User ] 
    |> Seq.iter (fun mode -> 
           tracefn "Environment-Settings (%A):" mode
           environVarsWithMode mode |> Seq.iter (tracefn "  %A"))
#else
    tracefn "Environment-Settings (%A):" "Process"
    environVars () |> Seq.iter (tracefn "  %A")
#endif

/// Traces a line
let traceLine() = trace "---------------------------------------------------------------------"

/// Traces a header
let traceHeader name = 
    trace ""
    traceLine()
    trace name
    traceLine()

/// Traces the begin of the build
let traceStartBuild() = CoreTracing.postMessage StartMessage

/// Traces the end of the build
let traceEndBuild() = CoreTracing.postMessage FinishedMessage

/// Puts an opening tag on the internal tag stack
let openTagUnsafe tag description =
    openTags.Value <- tag :: openTags.Value
    OpenTag(tag, description) |> CoreTracing.postMessage

let private asSafeDisposable f =
    let mutable isDisposed = false
    { new System.IDisposable with
        member __.Dispose () = 
            if not isDisposed then
              isDisposed <- true
              f() }

/// Puts an opening tag on the internal tag stack
[<System.Obsolete("Consider using traceTag instead and 'use' to properly call closeTag in case of exceptions. To remove this warning use 'openTagUnsafe'.")>]
let openTag tag description = openTagUnsafe tag description

/// Removes an opening tag from the internal tag stack
let closeTagUnsafe tag =
    match openTags.Value with
    | x :: rest when x = tag -> openTags.Value <- rest
    | _ -> failwithf "Invalid tag structure. Trying to close %A tag but stack is %A" tag openTags
    CloseTag tag |> CoreTracing.postMessage

/// Removes an opening tag from the internal tag stack
[<System.Obsolete("Consider using traceTag instead and 'use' to properly call closeTag in case of exceptions. To remove this warning use 'closeTagUnsafe'.")>]
let closeTag tag = closeTagUnsafe tag

let traceTag tag description =
    openTagUnsafe tag description
    asSafeDisposable (fun () -> closeTagUnsafe tag)


let closeAllOpenTags() = Seq.iter closeTagUnsafe openTags.Value

/// Traces the begin of a target
let traceStartTargetUnsafe name description dependencyString =
    openTagUnsafe (Target name) description
    tracefn "Starting Target: %s %s" name dependencyString
    if not (isNull description) then tracefn "  %s" description

/// Traces the begin of a target
[<System.Obsolete("Consider using traceTarget instead and 'use' to properly call traceEndTask in case of exceptions. To remove this warning use 'traceStartTargetUnsafe'.")>]
let traceStartTarget name description dependencyString =
    traceStartTargetUnsafe name description dependencyString

/// Traces the end of a target
let traceEndTargetUnsafe name = 
    tracefn "Finished Target: %s" name
    closeTagUnsafe (Target name)

/// Traces the end of a target
[<System.Obsolete("Consider using traceTarget instead and 'use' to properly call traceEndTask in case of exceptions. To remove this warning use 'traceEndTargetUnsafe'.")>]
let traceEndTarget name = traceEndTargetUnsafe name

let traceTarget name description dependencyString =
    traceStartTargetUnsafe name description dependencyString
    asSafeDisposable (fun () -> traceEndTargetUnsafe name )

/// Traces the begin of a task
let traceStartTaskUnsafe task description = 
    openTagUnsafe (Task task) description

/// Traces the begin of a task
[<System.Obsolete("Consider using traceTask instead and 'use' to properly call traceEndTask in case of exceptions. To remove this warning use 'traceStartTaskUnsafe'.")>]
let traceStartTask task description = traceStartTaskUnsafe task description

/// Traces the end of a task
let traceEndTaskUnsafe task = 
    closeTagUnsafe (Task task)
   
/// Traces the end of a task
[<System.Obsolete("Consider using traceTask instead and 'use' to properly call traceEndTask in case of exceptions. To remove this warning use 'traceEndTask'.")>]
let traceEndTask task = traceEndTaskUnsafe task
     
let traceTask name description =
    traceStartTaskUnsafe name description
    asSafeDisposable (fun () -> traceEndTaskUnsafe name)

let console = new ConsoleTraceListener(false, CoreTracing.colorMap) :> ITraceListener

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
    match eventLogEntry with
    | EventLogEntryType.Error -> ErrorMessage msg
    | EventLogEntryType.Information -> TraceMessage(msg, true)
    | EventLogEntryType.Warning -> ImportantMessage msg
    | _ -> LogMessage(msg, true)
    |> console.Write

/// Logs the given files with the message.
let Log message files = files |> Seq.iter (log << sprintf "%s%s" message)
