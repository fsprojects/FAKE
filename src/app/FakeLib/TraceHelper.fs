[<AutoOpen>]
module Fake.TraceHelper

open System
open System.IO
open System.Reflection

/// The trace Mode type.
type TraceMode =
| Console
| Xml

/// The actual trace mode.
let mutable traceMode = 
    match buildServer with
    | TeamCity   -> Console
    | CCNet      -> Xml
    | LocalBuild -> Console
    
let appendXML line = AppendToFile xmlOutputFile [line]

let mutable openTags = []

/// Trace verbose output
let mutable verbose = hasBuildParam "verbose"
   
/// Gets the path of the current FAKE instance
let fakePath = productName.GetType().Assembly.Location
       
/// Gets the FAKE version no.
let fakeVersion = productName.GetType().Assembly.GetName().Version 

/// logs the specified string to the console
let logMessageToConsole important newLine s =     
  if important && buildServer <> CCNet then
    if newLine then
      eprintfn "%s" (toRelativePath s)
    else 
      eprintf "%s" (toRelativePath s)
  else
    if newLine then
      printfn "%s" (toRelativePath s)
    else 
      printf "%s" (toRelativePath s)

let xmlMessage message =
    message
      |> sprintf "<message level=\"Info\"><![CDATA[%s]]></message>"
      |> appendXML
    
/// logs the specified string        
let logMessage important newLine message =
    match traceMode with
    | Console -> logMessageToConsole important newLine message
    | Xml     -> xmlMessage message

/// Logs the specified string        
let log = logMessage false true

/// Logs the specified message
let logfn fmt = Printf.ksprintf log fmt

/// Logs the specified message (without line break)
let logf fmt = Printf.ksprintf (logMessage false false) fmt

/// Logs the specified string if the verbose mode is activated.
let logVerbosefn fmt = Printf.ksprintf (if verbose then log else ignore) fmt

/// Writes a trace to the command line (in the given color)
let logColored important color newLine message =
  Console.ForegroundColor <- color    
  logMessage important newLine message
  Console.ForegroundColor <- ConsoleColor.White     
    
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
     appendXML "<failure><builderror>"
     xmlMessage error
     appendXML "</builderror></failure>"
  

/// Traces the EnvironmentVariables
let TraceEnvironmentVariables() = 
  [ EnvironTarget.Machine; 
    EnvironTarget.Process;
    EnvironTarget.User]
    |> List.iter (fun mode ->    
        traceFAKE "Environment-Settings (%A):" mode
        environVars mode |> List.iter (tracefn "  %A"))        
 
/// Gets the FAKE Version string
let fakeVersionStr = 
    fakeVersion.ToString()
      |> sprintf "FAKE - F# Make - Version %s" 

/// Traces the begin of the build
let traceStartBuild _ =
  match traceMode with
  | Console -> ()
  | Xml     ->         
      let fi = new FileInfo(xmlOutputFile)
      if fi.Exists then
        fi.IsReadOnly <- false
        fi.Delete()
      if not fi.Directory.Exists then fi.Directory.Create()
        
      appendXML "<buildresults>"

/// Traces the end of the build
let traceEndBuild _=
  match traceMode with
  | Console -> ()
  | Xml     -> appendXML "</buildresults>"

let openTag tag =  openTags <- tag :: openTags

let closeTag tag =
  match openTags with
  | x::rest when x = tag -> openTags <- rest
  | _ -> failwith "Invalid Tag-structure"
  appendXML <| sprintf "</%s>" tag
  
let closeAllOpenTags() = openTags |> Seq.iter closeTag

/// Traces the begin of a target
let traceStartTarget name dependencyString =
  match traceMode with
  | Console -> 
      tracefn "Starting Target: %s %s" name dependencyString
  | Xml     -> 
      openTag "target"
      appendXML <| sprintf "<target name=\"%s\">" name
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
    appendXML <| sprintf "<task name=\"%s\">" task
  ReportProgressStart <| sprintf "Task: %s %s" task description
   
/// Traces the end of a task
let traceEndTask task  description =
  match traceMode with
  | Console -> ()
  | Xml     -> closeTag "task" 
  ReportProgressFinish <| sprintf "Task: %s %s" task description       