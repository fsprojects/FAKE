[<AutoOpen>]
/// This module contains function which allow to trace build output
module Fake.TraceHelper

open System
open System.IO
open System.Reflection
   
/// Gets the path of the current FAKE instance
let fakePath = productName.GetType().Assembly.Location
       
/// Gets the FAKE version no.
let fakeVersion = Fake.AssemblyInfo.AssemblyVersionInformation.Version
    
let mutable private openTags = []

/// Logs the specified string        
let log message = LogMessage(message,true) |> postMessage

/// Logs the specified message
let logfn fmt = Printf.ksprintf log fmt

/// Logs the specified message (without line break)
let logf fmt = Printf.ksprintf (fun text -> postMessage(LogMessage(text,false))) fmt

/// Logs the specified string if the verbose mode is activated.
let logVerbosefn fmt = Printf.ksprintf (if verbose then log else ignore) fmt
    
/// Writes a trace to the command line (in green)
let trace message = postMessage(TraceMessage(message,true))

/// Writes a message to the command line (in green)
let tracefn fmt = Printf.ksprintf trace fmt

/// Writes a message to the command line (in green) and without a line break
let tracef fmt = Printf.ksprintf (fun text -> postMessage(TraceMessage(text,false))) fmt

/// Writes a trace to the command line (in green) if the verbose mode is activated.
let traceVerbose s = if verbose then trace s

/// Writes a trace to stderr (in yellow)  
let traceImportant text = postMessage(ImportantMessage text)
  
/// Writes a trace to the command line (in yellow)
let traceFAKE fmt = Printf.ksprintf (fun text -> postMessage(ImportantMessage text)) fmt

/// Traces an error (in red)
let traceError error = postMessage(ErrorMessage error)
  
/// Traces the EnvironmentVariables
let TraceEnvironmentVariables() = 
    [ EnvironTarget.Machine; 
      EnvironTarget.Process;
      EnvironTarget.User]
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
let traceEndBuild () = postMessage FinishedMessage

/// Puts an opening tag on the internal tag stack
let openTag tag =  openTags <- tag :: openTags

/// Removes an opening tag from the internal tag stack
let closeTag tag =
    match openTags with
    | x::rest when x = tag -> openTags <- rest
    | _ -> failwithf "Invalid tag structure. Trying to close %s tag but stack is %A" tag openTags

    CloseTag tag |> postMessage
  
let closeAllOpenTags() = Seq.iter closeTag openTags

/// Traces the begin of a target
let traceStartTarget name description dependencyString =
    openTag "target"
    OpenTag("target",name) |> postMessage

    tracefn "Starting Target: %s %s" name dependencyString
    if description <> null then
        tracefn "  %s" description

    ReportProgressStart <| sprintf "Target: %s" name
   
/// Traces the end of a target   
let traceEndTarget name =
    tracefn "Finished Target: %s" name
    closeTag "target"

    ReportProgressFinish <| sprintf "Target: %s" name
  
/// Traces the begin of a task
let traceStartTask task description =
    openTag "task"
    OpenTag("task",task) |> postMessage

    ReportProgressStart <| sprintf "Task: %s %s" task description
   
/// Traces the end of a task
let traceEndTask task  description =
    closeTag "task"

    ReportProgressFinish <| sprintf "Task: %s %s" task description

let console = new ConsoleTraceListener(false, colorMap) :> ITraceListener

let logToConsole(msg, eventLogEntry : System.Diagnostics.EventLogEntryType) = 
    match eventLogEntry with
    | System.Diagnostics.EventLogEntryType.Error -> ErrorMessage msg
    | System.Diagnostics.EventLogEntryType.Information -> TraceMessage (msg, true)
    | System.Diagnostics.EventLogEntryType.Warning -> ImportantMessage msg
    | _ -> LogMessage (msg, true)
    |> console.Write