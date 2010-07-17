[<AutoOpen>]
module Fake.TraceHelper

open System
open System.IO
open System.Reflection
   
/// Gets the path of the current FAKE instance
let fakePath = productName.GetType().Assembly.Location
       
/// Gets the FAKE version no.
let fakeVersion = productName.GetType().Assembly.GetName().Version
    
let mutable private openTags = []

/// Waits until the message queue is empty
let WaitUntilEverythingIsPrinted () = buffer.PostAndReply(fun channel -> ProcessAll channel)

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
let traceImportant text = 
    postMessage(ImportantMessage text)
    WaitUntilEverythingIsPrinted()
  
/// Writes a trace to the command line (in yellow)
let traceFAKE fmt = Printf.ksprintf (fun text -> postMessage(ImportantMessage text)) fmt

/// Traces an error (in red)
let traceError error =
    postMessage(ErrorMessage error)
    WaitUntilEverythingIsPrinted()
  
/// Traces the EnvironmentVariables
let TraceEnvironmentVariables() = 
    [ EnvironTarget.Machine; 
      EnvironTarget.Process;
      EnvironTarget.User]
    |> Seq.iter (fun mode ->    
        tracefn "Environment-Settings (%A):" mode
        environVars mode |> Seq.iter (tracefn "  %A"))        
 
/// Gets the FAKE Version string
let fakeVersionStr = sprintf "FAKE - F# Make - Version %s" <| fakeVersion.ToString()

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
   
let openTag tag =  openTags <- tag :: openTags

let closeTag tag =
    match openTags with
    | x::rest when x = tag -> openTags <- rest
    | _ -> failwith "Invalid Tag-structure"

    CloseTag tag |> postMessage
  
let closeAllOpenTags() = Seq.iter closeTag openTags

/// Traces the begin of a target
let traceStartTarget name dependencyString =
    openTag "target"
    OpenTag("target",name) |> postMessage

    tracefn "Starting Target: %s %s" name dependencyString

    ReportProgressStart <| sprintf "Target: %s" name
   
/// Traces the end of a target   
let traceEndTarget name =
    tracefn "Finished Target: %s" name
    closeTag "target"

    ReportProgressFinish <| sprintf "Target: %s" name
    WaitUntilEverythingIsPrinted()
  
/// Traces the begin of a task
let traceStartTask task description =
    openTag "task"
    OpenTag("task",task) |> postMessage

    ReportProgressStart <| sprintf "Task: %s %s" task description
   
/// Traces the end of a task
let traceEndTask task  description =
    closeTag "task"

    ReportProgressFinish <| sprintf "Task: %s %s" task description