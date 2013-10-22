[<AutoOpen>]
/// Contains tasks which allow to run msiexec in order to install or uninstall msi files.
module Fake.MSIHelper

open System

/// MSI parameter type
type MSIParams =
    { ToolPath: string
      WorkingDir:string
      LogFile:string
      ThrowIfSetupFails: bool;
      TimeOut: TimeSpan}

/// MSI default parameters  
let MSIDefaults =
    { ToolPath = "msiexec "
      WorkingDir = "."
      LogFile = "InstallLog.txt"
      ThrowIfSetupFails = true
      TimeOut = TimeSpan.FromMinutes 5. }

/// Installs a msi 
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default MSI parameters.
///  - `setup` - The setup file name.
let Install setParams setup = 
    traceStartTask "MSI-Install" setup
    let parameters = setParams MSIDefaults
    
    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- sprintf "/qb /l* %s /i %s" parameters.LogFile setup) parameters.TimeOut) && parameters.ThrowIfSetupFails 
    then
        failwith "MSI-Install failed."
                  
    traceEndTask "MSI-Install" setup

/// Uninstalls a msi 
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default MSI parameters.
///  - `setup` - The setup file name.
let Uninstall setParams setup = 
    traceStartTask "MSI-Uninstall" setup
    let parameters = setParams MSIDefaults
    
    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- sprintf "/qb /l* %s /x %s" parameters.LogFile setup) parameters.TimeOut) && parameters.ThrowIfSetupFails 
    then
        failwith "MSI-Uninstall failed."
                  
    traceEndTask "MSI-Uninstall" setup