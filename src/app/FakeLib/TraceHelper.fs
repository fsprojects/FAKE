[<AutoOpen>]
module Fake.TraceHelper

open System
open System.IO
open System.Reflection

/// Trace verbose output
let mutable verbose = hasBuildParam "verbose"
   
/// Gets the path of the current FAKE instance
let fakePath = productName.GetType().Assembly.Location
       
/// Gets the FAKE version no.
let fakeVersion = productName.GetType().Assembly.GetName().Version
    
let mutable private openTags = []

/// Writes a XML message to the bufffer.
let xmlMessage message =
    { defaultMessage with Text = sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" message }
      |> buffer.Post 
    
/// Logs the specified string (via message buffer)
let logMessage important newLine message =
    match traceMode with
    | Console ->
        { Text = message; Important = important; Newline = newLine; Color = ConsoleColor.White }
          |> buffer.Post
    | Xml     -> xmlMessage message

/// Logs the specified string        
let log = logMessage false true

/// Logs the specified message
let logfn fmt = Printf.ksprintf log fmt

/// Logs the specified message (without line break)
let logf fmt = Printf.ksprintf (logMessage false false) fmt

/// Logs the specified string if the verbose mode is activated.
let logVerbosefn fmt = Printf.ksprintf (if verbose then log else ignore) fmt

/// Writes a trace output to the message buffer (in the given color)
let logColored important color newLine message =
    match traceMode with
    | Console ->        
        { Text = message
          Color = color
          Important = important
          Newline = newLine }
          |> buffer.Post
    | Xml     -> xmlMessage message
    
/// Writes a trace to the command line (in green)
let trace s = logColored false ConsoleColor.Green true s

/// Writes a message to the command line (in green)
let tracefn fmt = Printf.ksprintf trace fmt

/// Writes a message to the command line (in green) and without a line break
let tracef fmt = Printf.ksprintf (logColored false ConsoleColor.Green false) fmt

/// Writes a trace to the command line (in green) if the verbose mode is activated.
let traceVerbose s = if verbose then trace s

/// Writes a trace to stderr (in green)  
let traceImportant s = logColored true ConsoleColor.Green true s
  
/// Writes a trace to the command line (in yellow)
let traceFAKE fmt = Printf.ksprintf (logColored true ConsoleColor.Yellow true) fmt

/// Traces an error (in red)
let traceError error = 
    match traceMode with
    | Console -> logColored true ConsoleColor.Red true error
    | Xml     -> 
        { defaultMessage with 
            Text = sprintf "<failure><builderror><message level=\"Error\"><![CDATA[%s]]></message></builderror></failure>" error }
          |> buffer.Post
  

/// Traces the EnvironmentVariables
let TraceEnvironmentVariables() = 
    [ EnvironTarget.Machine; 
      EnvironTarget.Process;
      EnvironTarget.User]
    |> List.iter (fun mode ->    
        traceFAKE "Environment-Settings (%A):" mode
        environVars mode |> List.iter (tracefn "  %A"))        
 
/// Gets the FAKE Version string
let fakeVersionStr = sprintf "FAKE - F# Make - Version %s" <| fakeVersion.ToString()

/// Traces the begin of the build
let traceStartBuild () =
    match traceMode with
    | Console -> ()
    | Xml     ->         
        let fi = fileInfo xmlOutputFile
        if fi.Exists then
            fi.IsReadOnly <- false
            fi.Delete()
        if not fi.Directory.Exists then fi.Directory.Create()
        
        buffer.Post { defaultMessage with Text = "<buildresults>" }

/// Traces the end of the build
let traceEndBuild () =
    match traceMode with
    | Console -> ()
    | Xml     -> buffer.Post { defaultMessage with Text = "</buildresults>" }

let openTag tag =  openTags <- tag :: openTags

let closeTag tag =
    match openTags with
    | x::rest when x = tag -> openTags <- rest
    | _ -> failwith "Invalid Tag-structure"

    buffer.Post { defaultMessage with Text = sprintf "</%s>" tag }
  
let closeAllOpenTags() = openTags |> Seq.iter closeTag

/// Traces the begin of a target
let traceStartTarget name dependencyString =
    match traceMode with
    | Console -> tracefn "Starting Target: %s %s" name dependencyString
    | Xml     -> 
        openTag "target"
        buffer.Post { defaultMessage with Text = sprintf "<target name=\"%s\">" name }
        xmlMessage dependencyString

    ReportProgressStart <| sprintf "Target: %s" name
   
/// Traces the end of a target   
let traceEndTarget name =
    match traceMode with
    | Console -> tracefn "Finished Target: %s" name
    | Xml     -> closeTag "target"

    ReportProgressFinish <| sprintf "Target: %s" name
  
/// Traces the begin of a task
let traceStartTask task description =
    match traceMode with
    | Console -> ()
    | Xml     -> 
        openTag "task"
        buffer.Post { defaultMessage with Text = sprintf "<task name=\"%s\">" task }

    ReportProgressStart <| sprintf "Task: %s %s" task description
   
/// Traces the end of a task
let traceEndTask task  description =
    match traceMode with
    | Console -> ()
    | Xml     -> closeTag "task"

    ReportProgressFinish <| sprintf "Task: %s %s" task description       

/// Waits until the message queue is empty
let WaitUntilEverythingIsPrinted () =
     waitFor 
        MessageBoxIsEmpty
        (System.TimeSpan.FromSeconds 5.0) 
        100
        ignore
       |> ignore