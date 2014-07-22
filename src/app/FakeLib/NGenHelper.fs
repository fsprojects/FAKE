/// This module contains helper function for the ngen.exe
module Fake.NGenHelper

open System

/// NGen parameters
type NGenParams = 
    { /// (Required) Path to the NGenutil
      ToolPath : string
      /// The timeout for the process.
      TimeOut : TimeSpan
      /// The directory where the process will be started.
      WorkingDir : string }

let private winDir = Environment.GetFolderPath Environment.SpecialFolder.Windows
let mutable NGen32 = winDir @@ "Microsoft.NET/Framework/4.0.30319/ngen.exe"
let mutable NGen64 = winDir @@ "Microsoft.NET/Framework64/4.0.30319/ngen.exe"

/// NGen default parameters
let NGenDefaults = 
    { ToolPath = NGen32
      TimeOut = TimeSpan.FromMinutes 5.
      WorkingDir = currentDirectory }

/// Runs ngen.exe with the given command.
let NGen setParams command = 
    let taskName = "NGen"
    traceStartTask taskName command
    let param = setParams NGenDefaults
    
    let ok = 
        execProcess (fun info -> 
            info.FileName <- param.ToolPath
            if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
            info.Arguments <- command) param.TimeOut
    if not ok then failwithf "NGenutil reported errors."

    traceEndTask taskName command