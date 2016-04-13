[<AutoOpen>]
/// This module contains function which allow to trace build output
module Fake.TraceHelper

open System
open System.IO
open System.Reflection
open System.Threading

type FAKEException(msg) =
    inherit System.Exception(msg)

/// Gets the path of the current FAKE instance
let fakePath = productName.GetType().Assembly.Location

/// Gets the FAKE version no.
let fakeVersion = AssemblyVersionInformation.Version

let private openTags = new ThreadLocal<list<string>>(fun _ -> [])

/// Logs the specified string        
let log message = LogMessage(message, true) |> postMessage

/// Logs the specified message
let logfn fmt = Printf.ksprintf log fmt

/// Logs the specified message (without line break)
let logf fmt = Printf.ksprintf (fun text -> postMessage (LogMessage(text, false))) fmt

/// Logs the specified string if the verbose mode is activated.
let logVerbosefn fmt = 
    Printf.ksprintf (if verbose then log
                     else ignore) fmt

/// Writes a trace to the command line (in green)
let trace message = postMessage (TraceMessage(message, true))

/// Writes a message to the command line (in green)
let tracefn fmt = Printf.ksprintf trace fmt

/// Writes a message to the command line (in green) and without a line break
let tracef fmt = Printf.ksprintf (fun text -> postMessage (TraceMessage(text, false))) fmt

/// Writes a trace to the command line (in green) if the verbose mode is activated.
let traceVerbose s = 
    if verbose then trace s

/// Writes a trace to stderr (in yellow)  
let traceImportant text = postMessage (ImportantMessage text)

/// Writes a trace to the command line (in yellow)
let traceFAKE fmt = Printf.ksprintf (fun text -> postMessage (ImportantMessage text)) fmt

/// Traces an error (in red)
let traceError error = postMessage (ErrorMessage error)

open Microsoft.FSharp.Core.Printf

/// Converts an exception and its inner exceptions to a nice string.
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
let traceException (ex:Exception) = exceptionAndInnersToString ex |> traceError

/// Traces the EnvironmentVariables
let TraceEnvironmentVariables() = 
    [ EnvironTarget.Machine; EnvironTarget.Process; EnvironTarget.User ] 
    |> Seq.iter (fun mode -> 
           tracefn "Environment-Settings (%A):" mode
           environVars mode |> Seq.iter (tracefn "  %A"))

/// Gets the FAKE Version string
let fakeVersionStr = sprintf "FAKE - F# Make %A" fakeVersion

/// Traces a line
let traceLine() = trace "---------------------------------------------------------------------"

/// Traces a header
let traceHeader name = 
    trace ""
    traceLine()
    trace name
    traceLine()

/// Traces the begin of the build
let traceStartBuild() = postMessage StartMessage

/// Traces the end of the build
let traceEndBuild() = postMessage FinishedMessage

/// Puts an opening tag on the internal tag stack
let openTag tag = openTags.Value <- tag :: openTags.Value

/// Removes an opening tag from the internal tag stack
let closeTag tag = 
    match openTags.Value with
    | x :: rest when x = tag -> openTags.Value <- rest
    | _ -> failwithf "Invalid tag structure. Trying to close %s tag but stack is %A" tag openTags
    CloseTag tag |> postMessage

let closeAllOpenTags() = Seq.iter closeTag openTags.Value

/// Traces the begin of a target
let traceStartTarget name description dependencyString = 
    sendOpenBlock name
    ReportProgressStart <| sprintf "Target: %s" name
    openTag "target"
    OpenTag("target", name) |> postMessage
    tracefn "Starting Target: %s %s" name dependencyString
    if description <> null then tracefn "  %s" description

/// Traces the end of a target   
let traceEndTarget name = 
    tracefn "Finished Target: %s" name
    closeTag "target"
    ReportProgressFinish <| sprintf "Target: %s" name
    sendCloseBlock name

/// Traces the begin of a task
let traceStartTask task description = 
    openTag "task"
    OpenTag("task", task) |> postMessage
    ReportProgressStart <| sprintf "Task: %s %s" task description

/// Traces the end of a task
let traceEndTask task description = 
    closeTag "task"
    ReportProgressFinish <| sprintf "Task: %s %s" task description

let console = new ConsoleTraceListener(false, colorMap) :> ITraceListener

open System.Diagnostics

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
