/// This module contains helper function for the GAC
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.GACHelper

open System

/// Path to newest `gacutil.exe`
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let gacutilToolPath = !! (sdkBasePath + "/**/gacutil.exe")  
                             |> getNewestTool

/// GAC parameters
[<CLIMutable>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type GACParams = 
    { /// (Required) Path to the gacutil
      ToolPath : string
      /// The timeout for the process.
      TimeOut : TimeSpan
      /// The directory where the process will be started.
      WorkingDir : string }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let mutable GACUtil = 
    if isMono then "gacutil" else gacutilToolPath

/// GACutil default parameters
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let GACDefaults = 
    { ToolPath = GACUtil
      TimeOut = TimeSpan.FromMinutes 5.
      WorkingDir = currentDirectory }

/// Runs gacutil with the given command.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let GAC setParams command = 
    let taskName = "GAC"
    use __ = traceStartTaskUsing taskName command
    let param = setParams GACDefaults
    
    let ok = 
        execProcess (fun info -> 
            info.FileName <- param.ToolPath
            if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
            info.Arguments <- command) param.TimeOut
    if not ok then failwithf "gacutil reported errors."
