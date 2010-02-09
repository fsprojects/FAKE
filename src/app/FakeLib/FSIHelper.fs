[<AutoOpen>]
module Fake.FSIHelper

open System.IO
open System.Configuration
open System.Threading
   
let fsiPath = 
  let ev = environVar "FSI"
  if ev <> null && ev <> "" then ev else
    findFile (ConfigurationManager.AppSettings.["FSIPath"].Split(';')) "fsi.exe" 
      
/// Run the buildscript with fsi
let runBuildScript script args = 
  traceFAKE <| sprintf "Running Buildscript: %s" script
  
  let result = execProcess (fun info ->  
    info.FileName <- fsiPath
    info.Arguments <- script           
    args |> Seq.iter (fun (key,value) -> info.EnvironmentVariables.Add(key, value))
    if not <| info.EnvironmentVariables.ContainsKey("MSBuild") then
      info.EnvironmentVariables.Add("MSBuild", msBuildExe)
    if not <| info.EnvironmentVariables.ContainsKey("FSI") then
      info.EnvironmentVariables.Add("FSI", fsiPath)    )
    
  Thread.Sleep 1000
  result