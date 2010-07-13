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

/// Writes a XML message to the bufffer.
let xmlMessage message =
    { defaultMessage with 
        Text = sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" message
        Target = Xml }
         |> buffer.Post 
    
/// Logs the specified string (via message buffer)
let logMessage important newLine message =
    if traceMode = Xml then xmlMessage message

    { Target = TraceMode.Console
      Text = message
      Important = important
      Newline = newLine
      Color = ConsoleColor.White }
          |> buffer.Post

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
    if traceMode = Xml then xmlMessage message

    { Target = TraceMode.Console
      Text = message
      Color = color
      Important = important
      Newline = newLine }
          |> buffer.Post
    
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
    if traceMode = Xml then
        { defaultMessage with 
            Text = sprintf "<failure><builderror><message level=\"Error\"><![CDATA[%s]]></message></builderror></failure>" error
            Target = Xml }
             |> buffer.Post

    logColored true ConsoleColor.Red true error
  

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
let traceStartBuild () =
    match traceMode with
    | Console -> ()
    | Xml     ->         
        let fi = fileInfo xmlOutputFile
        if fi.Exists then
            fi.IsReadOnly <- false
            fi.Delete()
        if not fi.Directory.Exists then fi.Directory.Create()
        
        { defaultMessage with 
            Text = "<?xml version=\"1.0\"?>\r\n<buildresults>" 
            Target = Xml }
                |> buffer.Post

/// Traces the end of the build
let traceEndBuild () =
    match traceMode with
    | Console -> ()
    | Xml     -> 
        { defaultMessage with 
            Text = "</buildresults>"
            Target = Xml }
                |> buffer.Post

let openTag tag =  openTags <- tag :: openTags

let closeTag tag =
    match openTags with
    | x::rest when x = tag -> openTags <- rest
    | _ -> failwith "Invalid Tag-structure"

    { defaultMessage with 
        Text = sprintf "</%s>" tag
        Target = Xml }
            |> buffer.Post
  
let closeAllOpenTags() = Seq.iter closeTag openTags

/// Traces the begin of a target
let traceStartTarget name dependencyString =
    if traceMode = Xml then
        openTag "target"
        { defaultMessage with 
            Text = sprintf "<target name=\"%s\">" name
            Target = Xml }
                |> buffer.Post

    tracefn "Starting Target: %s %s" name dependencyString

    ReportProgressStart <| sprintf "Target: %s" name

/// Waits until the message queue is empty
let WaitUntilEverythingIsPrinted () =
     waitFor 
        MessageBoxIsEmpty
        (System.TimeSpan.FromSeconds 5.0) 
        100
        ignore
       |> ignore
   
/// Traces the end of a target   
let traceEndTarget name =
    tracefn "Finished Target: %s" name
    if traceMode = Xml then closeTag "target"

    ReportProgressFinish <| sprintf "Target: %s" name
    WaitUntilEverythingIsPrinted()
  
/// Traces the begin of a task
let traceStartTask task description =
    match traceMode with
    | Console -> ()
    | Xml     -> 
        openTag "task"
        { defaultMessage with 
            Text = sprintf "<task name=\"%s\">" task
            Target = Xml }
                |> buffer.Post

    ReportProgressStart <| sprintf "Task: %s %s" task description
   
/// Traces the end of a task
let traceEndTask task  description =
    match traceMode with
    | Console -> ()
    | Xml     -> closeTag "task"

    ReportProgressFinish <| sprintf "Task: %s %s" task description