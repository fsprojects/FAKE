[<AutoOpen>]
module Fake.ProcessHelper

open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Threading

let mutable redirectOutputToTrace = false 

/// Runs the given process
/// returns the exit code
let execProcess2 infoAction silent =
    use p = new Process()
    p.StartInfo.UseShellExecute <- false
    infoAction p.StartInfo
    if silent then
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.RedirectStandardError <- true
        p.ErrorDataReceived.Add(fun d -> if d.Data <> null then traceError d.Data)
        p.OutputDataReceived.Add(fun d -> if d.Data <> null then trace d.Data)
    try
        p.Start() |> ignore
    with
    | exn -> failwithf "Could not execute %s. %s" p.StartInfo.FileName exn.Message
    
    if silent then 
        p.BeginErrorReadLine()
        p.BeginOutputReadLine()     
  
    p.WaitForExit()
    
    p.ExitCode  

/// Runs the given process
/// returns the exit code
let execProcessAndReturnExitCode infoAction = execProcess2 infoAction true

/// Runs the given process
/// returns if the exit code was 0
let execProcess3 infoAction = execProcessAndReturnExitCode infoAction = 0   

/// Runs the given process
/// returns the exit code
let ExecProcess infoAction = execProcess2 infoAction redirectOutputToTrace
  
/// sets the environment Settings for the given startInfo
/// existing values will be overrriden
let setEnvironmentVariables (startInfo:ProcessStartInfo) environmentSettings = 
  for key,value in environmentSettings do
    if startInfo.EnvironmentVariables.ContainsKey key then
      startInfo.EnvironmentVariables.[key] <- value
    else
      startInfo.EnvironmentVariables.Add(key, value)
          
/// Runs the given process
/// returns true if the exit code was 0
let execProcess infoAction = ExecProcess infoAction = 0    

/// Adds quotes around the string   
let toParam x = " \"" + x + "\" " 
 
/// Use default Parameters
let UseDefaults = id

/// Searches the given directories for all occurrences of the given file name
let tryFindFile dirs file =
    let files = 
        dirs
          |> Seq.map 
               (fun (path:string) ->
                   let dir =
                     path
                       |> replace "[ProgramFiles]" ProgramFiles
                       |> replace "[ProgramFilesX86]" ProgramFilesX86
                       |> directoryInfo
                   if not dir.Exists then "" else
                   let fi = dir.FullName @@ file |> fileInfo
                   if fi.Exists then fi.FullName else "")
          |> Seq.filter ((<>) "")
          |> Seq.cache

    if not (Seq.isEmpty files) then
        Some (Seq.head files)
    else
        None

/// Searches the given directories for the given file, failing if not found
let findFile dirs file =
    match tryFindFile dirs file with
    | Some found -> found
    | None -> failwithf "%s not found in %A." file dirs

/// Returns the AppSettings for the key - Splitted on ;
let appSettings (key:string) = System.Configuration.ConfigurationManager.AppSettings.[key].Split(';')

/// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable. 
let findPath settingsName tool = 
    let paths = appSettings settingsName
    match tryFindFile paths tool with
    | Some file -> file
    | None -> tool
