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
let logMessageToConsole important s =     
  if important && buildServer <> CCNet then
    eprintfn "%s" (toRelativePath s)  
  else
    printfn "%s" (toRelativePath s)
    
let xmlMessage s =
  sprintf "<message level=\"Info\"><![CDATA[%s]]></message>" s |> appendXML
    
/// logs the specified string        
let logMessage important s =
    match traceMode with
    | Console -> logMessageToConsole important s
    | Xml     -> xmlMessage s

/// Logs the specified string        
let log = logMessage false

/// Logs the specified string if the verbose mode is activated.
let logVerbose s = if verbose then log s

/// Writes a trace to the command line (in the given color)
let logColored important s color =
  Console.ForegroundColor <- color    
  logMessage important s
  Console.ForegroundColor <- ConsoleColor.White     
    
/// Writes a trace to the command line (in green)
let trace s = 
  logColored false s ConsoleColor.Green    

/// Writes a trace to the command line (in green) if the verbose mode is activated.
let traceVerbose s = if verbose then trace s

/// Writes a trace to stderr (in green)  
let traceImportant s = 
  logColored true s ConsoleColor.Green    
  
/// Writes a trace to the command line (in yellow)
let traceFAKE s = 
  logColored true s ConsoleColor.Yellow

/// Traces an error (in red)
let traceError s = 
  match traceMode with
  | Console -> logColored true s ConsoleColor.Red 
  | Xml     ->  
     appendXML "<failure><builderror>"
     xmlMessage s
     appendXML "</builderror></failure>"
  

/// Traces the EnvironmentVariables
let TraceEnvironmentVariables() = 
  [ EnvironTarget.Machine; 
    EnvironTarget.Process;
    EnvironTarget.User]
    |> List.iter (fun mode ->    
        traceFAKE (sprintf "Environment-Settings (%A):" mode)
        environVars mode |> List.iter (sprintf "  %A" >> trace))        
 
/// Gets the FAKE Version string
let fakeVersionStr = sprintf "FAKE - F# Make - Version %s" (fakeVersion.ToString())

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
      trace <| sprintf "Starting Target: %s %s" name dependencyString
  | Xml     -> 
      openTag "target"
      appendXML <| sprintf "<target name=\"%s\">" name
      xmlMessage dependencyString

  ReportProgressStart <| sprintf "Target: %s" name
   
/// Traces the end of a target   
let traceEndTarget name =
  match traceMode with
  | Console -> sprintf "Finished Target: %s" name |> trace
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
   
    
       