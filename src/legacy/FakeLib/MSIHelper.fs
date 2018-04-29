[<AutoOpen>]
/// Contains tasks which allow to run msiexec in order to install or uninstall msi files.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.MSIHelper

open System

/// MSI parameter type
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<CLIMutable>]
type MSIParams =
    { ToolPath: string
      WorkingDir:string
      LogFile:string
      ThrowIfSetupFails: bool
      Silent: bool
      TimeOut: TimeSpan}

/// MSI default parameters  
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
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
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Install setParams setup = 
    use __ = traceStartTaskUsing "MSI-Install" setup
    let parameters = setParams MSIDefaults
    let args = sprintf "%s /l* %s /i %s" (if parameters.Silent then "/qn" else "/qb") parameters.LogFile setup

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut && parameters.ThrowIfSetupFails 
    then
        failwithf "MSI-Install %s failed." args

/// Uninstalls a msi.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default MSI parameters.
///  - `setup` - The setup file name.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Uninstall setParams setup = 
    use __ = traceStartTaskUsing "MSI-Uninstall" setup
    let parameters = setParams MSIDefaults
    let args = sprintf "%s /l* %s /x %s" (if parameters.Silent then "/qn" else "/qb") parameters.LogFile setup
    
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut && parameters.ThrowIfSetupFails 
    then
        failwithf "MSI-Uninstall %s failed." args
