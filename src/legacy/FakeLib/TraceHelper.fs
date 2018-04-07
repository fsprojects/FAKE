[<AutoOpen>]
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
/// This module contains function which allow to trace build output
module Fake.TraceHelper

#nowarn "44"
open System
open System.IO
open System.Reflection
open System.Threading

[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
type FAKEException(msg) =
    inherit System.Exception(msg)

/// Gets the path of the current FAKE instance
[<System.Obsolete("Don't use anymore, there is no equivalent, please open an issue in FAKE if you need help.")>]
let fakePath = productName.GetType().Assembly.Location

/// Gets the FAKE version no.
[<System.Obsolete("use Fake.Runtime.Environment.fakeVersion instead (open Fake.Runtime and use 'Environment.')")>]
let fakeVersion = AssemblyVersionInformation.AssemblyVersion

let private openTags = new ThreadLocal<list<string>>(fun _ -> [])

/// Logs the specified string
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let log message = LogMessage(message, true) |> postMessage

/// Logs the specified message
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let logfn fmt = Printf.ksprintf log fmt

/// Logs the specified message (without line break)
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let logf fmt = Printf.ksprintf (fun text -> postMessage (LogMessage(text, false))) fmt

/// Logs the specified string if the verbose mode is activated.
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let logVerbosefn fmt = 
    Printf.ksprintf (if verbose then log
                     else ignore) fmt

/// Writes a trace to the command line (in green)
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let trace message = postMessage (TraceMessage(message, true))

/// Writes a message to the command line (in green)
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let tracefn fmt = Printf.ksprintf trace fmt

/// Writes a message to the command line (in green) and without a line break
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let tracef fmt = Printf.ksprintf (fun text -> postMessage (TraceMessage(text, false))) fmt

/// Writes a trace to the command line (in green) if the verbose mode is activated.
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceVerbose s = 
    if verbose then trace s

/// Writes a trace to stderr (in yellow)  
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceImportant text = postMessage (ImportantMessage text)

/// Writes a trace to the command line (in yellow)
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceFAKE fmt = Printf.ksprintf (fun text -> postMessage (ImportantMessage text)) fmt

/// Traces an error (in red)
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceError error = postMessage (ErrorMessage error)

open Microsoft.FSharp.Core.Printf

/// Converts an exception and its inner exceptions to a nice string.
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let exceptionAndInnersToString (ex:Exception) =
    let sb = Text.StringBuilder()
    let delimeter = String.replicate 50 "*"
    let nl = Environment.NewLine
    let rec printException (e:Exception) count =
        if (e :? TargetException && e.InnerException <> null)
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
                        if (value <> null)
                        then bprintf sb "%s%s: %s" nl p.Name (value.ToString())
                    with
                    | e2 -> bprintf sb "%s%s: %s" nl p.Name e2.Message
            )
            if (e.StackTrace <> null) then
                bprintf sb "%s%sStackTrace%s%s%s" nl nl nl delimeter nl
                bprintf sb "%s%s" nl e.StackTrace
            if (e.InnerException <> null)
            then printException e.InnerException (count+1)
    printException ex 1
    sb.ToString()

/// Traces an exception details (in red)
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceException (ex:Exception) = exceptionAndInnersToString ex |> traceError

/// Traces the EnvironmentVariables
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let TraceEnvironmentVariables() = 
    [ EnvironTarget.Machine; EnvironTarget.Process; EnvironTarget.User ] 
    |> Seq.iter (fun mode -> 
           tracefn "Environment-Settings (%A):" mode
           environVars mode |> Seq.iter (tracefn "  %A"))

/// Gets the FAKE Version string
[<System.Obsolete("use Fake.Runtime.Environment.fakeVersionStr instead (open Fake.Runtime and use 'Environment.')")>]
let fakeVersionStr = sprintf "FAKE - F# Make %A" fakeVersion

/// Traces a line
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceLine() = trace "---------------------------------------------------------------------"

/// Traces a header
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceHeader name = 
    trace ""
    traceLine()
    trace name
    traceLine()

/// Traces the begin of the build
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceStartBuild() = postMessage StartMessage

/// Traces the end of the build
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let traceEndBuild() = postMessage FinishedMessage

/// Puts an opening tag on the internal tag stack
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let openTag tag = openTags.Value <- tag :: openTags.Value

/// Removes an opening tag from the internal tag stack
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let closeTag tag = 
    match openTags.Value with
    | x :: rest when x = tag -> openTags.Value <- rest
    | _ -> failwithf "Invalid tag structure. Trying to close %s tag but stack is %A" tag openTags
    CloseTag tag |> postMessage
    
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let closeAllOpenTags() = Seq.iter closeTag openTags.Value

/// Traces the begin of a target
[<System.Obsolete("This method is unsafe, please use nuget 'Fake.Core.Trace', open Fake.Core and use 'use _ = Trace.traceTarget' to properly finish the target on errors or use traceStartTargetUnsafe if you know what you do")>]
let traceStartTarget name description dependencyString = 
    sendOpenBlock name
    ReportProgressStart <| sprintf "Target: %s" name
    openTag "target"
    OpenTag("target", name) |> postMessage
    tracefn "Starting Target: %s %s" name dependencyString
    if description <> null then tracefn "  %s" description

/// Traces the end of a target   
[<System.Obsolete("This method is unsafe, please use nuget 'Fake.Core.Trace', open Fake.Core and use 'use _ = Trace.traceTarget' to properly finish the target on errors or use traceStartTargetUnsafe if you know what you do")>]
let traceEndTarget name = 
    tracefn "Finished Target: %s" name
    closeTag "target"
    ReportProgressFinish <| sprintf "Target: %s" name
    sendCloseBlock name

/// Traces the begin of a task
[<System.Obsolete("This method is unsafe, please use nuget 'Fake.Core.Trace', open Fake.Core and use 'use _ = Trace.traceTask' to properly finish the task on errors or use traceStartTaskUnsafe if you know what you do")>]
let traceStartTask task description = 
    openTag "task"
    OpenTag("task", task) |> postMessage
    ReportProgressStart <| sprintf "Task: %s %s" task description

/// Traces the end of a task
[<System.Obsolete("This method is unsafe, please use nuget 'Fake.Core.Trace', open Fake.Core and use 'use _ = Trace.traceTask' to properly finish the task on errors or use traceStartTaskUnsafe if you know what you do")>]
let traceEndTask task description = 
    closeTag "task"
    ReportProgressFinish <| sprintf "Task: %s %s" task description

/// Traces the begin of a task and closes it again after disposing of the return value
/// (call it with 'use')
[<System.Obsolete("Please use nuget 'Fake.Core.Trace', open Fake.Core and use 'use _ = Trace.traceTask' to properly finish the task on errors or use traceStartTaskUnsafe if you know what you do")>]
let traceStartTaskUsing task description = 
    traceStartTask task description
    { new IDisposable with member x.Dispose() = traceEndTask task description }
    
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let console = new ConsoleTraceListener(false, colorMap, false) :> ITraceListener

open System.Diagnostics

/// Traces the message to the console
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.')")>]
let logToConsole (msg, eventLogEntry : EventLogEntryType) = 
    match eventLogEntry with
    | EventLogEntryType.Error -> ErrorMessage msg
    | EventLogEntryType.Information -> TraceMessage(msg, true)
    | EventLogEntryType.Warning -> ImportantMessage msg
    | _ -> LogMessage(msg, true)
    |> console.Write

/// Logs the given files with the message.
[<System.Obsolete("use nuget 'Fake.Core.Trace' instead (open Fake.Core and use 'Trace.logItems')")>]
let Log message files = files |> Seq.iter (log << sprintf "%s%s" message)
