[<AutoOpen>]
/// Contains tasks which allow to run msiexec in order to install or uninstall msi files.
module Fake.MSIHelper

open System

/// MSI parameter type
type MSIParams =
    { ToolPath: string
      WorkingDir:string
      LogFile:string
      ThrowIfSetupFails: bool
      Silent: bool
      TimeOut: TimeSpan}

/// MSI default parameters  
let MSIDefaults =
    { ToolPath = "msiexec "
      WorkingDir = "."
      LogFile = "InstallLog.txt"
      ThrowIfSetupFails = true
      Silent = false
      TimeOut = TimeSpan.FromMinutes 5. }

/// Installs a msi.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default MSI parameters.
///  - `setup` - The setup file name.
let Install setParams setup = 
    traceStartTask "MSI-Install" setup
    let parameters = setParams MSIDefaults
    let args = sprintf "%s /l* %s /i %s" (if parameters.Silent then "/qn" else "/qb") parameters.LogFile setup

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut && parameters.ThrowIfSetupFails 
    then
        failwithf "MSI-Install %s failed." args
                  
    traceEndTask "MSI-Install" setup

/// Uninstalls a msi.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default MSI parameters.
///  - `setup` - The setup file name.
let Uninstall setParams setup = 
    traceStartTask "MSI-Uninstall" setup
    let parameters = setParams MSIDefaults
    let args = sprintf "%s /l* %s /x %s" (if parameters.Silent then "/qn" else "/qb") parameters.LogFile setup
    
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut && parameters.ThrowIfSetupFails 
    then
        failwithf "MSI-Uninstall %s failed." args
                  
    traceEndTask "MSI-Uninstall" setup