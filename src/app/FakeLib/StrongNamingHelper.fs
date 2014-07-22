/// This module contains helper function for Microsoft's sn.exe
module Fake.StrongNamingHelper

open System


/// Strong naming parameters
type StrongNameParams = 
    { /// (Required) Path to the sn.exe
      ToolPath : string
      /// The timeout for the Strong naming process.
      TimeOut : TimeSpan
      /// The directory where the Strong naming process will be started.
      WorkingDir : string }

let mutable SN32 = ProgramFilesX86 @@ "Microsoft SDKs/Windows/v8.0A/bin/NETFX 4.0 Tools/sn.exe"
let mutable SN64 = ProgramFilesX86 @@ "Microsoft SDKs/Windows/v8.0A/bin/NETFX 4.0 Tools/x64/sn.exe"

/// Strong naming default parameters
let StrongNameDefaults = 
    { ToolPath = SN32
      TimeOut = TimeSpan.FromMinutes 5.
      WorkingDir = currentDirectory }

/// Runs sn.exe with the given command.
let StrongName setParams command = 
    let taskName = "StrongName"
    traceStartTask taskName command
    let param = setParams StrongNameDefaults
    
    let ok = 
        execProcess (fun info -> 
            info.FileName <- param.ToolPath
            if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
            info.Arguments <- command) param.TimeOut
    if not ok then failwithf "SN.exe reported errors."

    traceEndTask taskName command

/// Registers the given assembly for verification skipping.
let DisableVerification assembly key =
    let command = sprintf "-Vr %s,%s" assembly key

    StrongName id command

    // For 64-bit versions of Windows, we also need to run the 64-bit version of the strong-name tool.
    if Environment.Is64BitOperatingSystem then
        StrongName (fun p -> { p with ToolPath = SN64 }) command