[<AutoOpen>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
/// Contains a task which can be used to run regasm .NET assembly
module Fake.RegAsmHelper

open System
open System.IO

/// Path to newest `regasm.exe`
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let regAsmToolPath = !! (TargetPlatformPrefix + "/**/RegAsm.exe")  
                             |> getNewestTool

/// RegAsm parameter type
[<CLIMutable>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type RegAsmParams = 
    { ToolPath : string
      WorkingDir : string
      TimeOut : TimeSpan
      ExportTypeLibrary : bool }

/// RegAsm default params
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let RegAsmDefaults = 
    { ToolPath = regAsmToolPath
      WorkingDir = "."
      TimeOut = TimeSpan.FromMinutes 5.
      ExportTypeLibrary = true }

/// Runs regasm on the given lib
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default RegAsm parameters.
///  - `lib` - The assembly file name.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let RegAsm setParams lib = 
    use __ = traceStartTaskUsing "RegAsm" lib
    let parameters = setParams RegAsmDefaults
    let args = if parameters.ExportTypeLibrary then
                    sprintf "\"%s\" /tlb:\"%s\"" lib (replace ".dll" ".tlb" lib)
                else
                    sprintf "\"%s\"" lib
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- parameters.WorkingDir
                info.Arguments <- args) parameters.TimeOut
    then failwithf "RegAsm %s failed." args

/// Executes `RegAsm.exe` with the `/codebase` `/tlb` option
///
/// Used to temporarily register any .net dependencies before running 
/// a VB6 build
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let public RegisterAssembliesWithCodebase workingDir (assemblies:string seq) =
    use __ = traceStartTaskUsing "Regasm with codebase" "Registering assemblies with codebase, expect warnings"
    let registerAssemblyWithCodebase assembly =
        async {
            let! regAsmResult = 
                asyncShellExec {defaultParams with 
                                    Program = regAsmToolPath
                                    WorkingDirectory = workingDir
                                    CommandLine = (sprintf "\"%s\" /tlb:%s /codebase" assembly ((Path.GetFileName assembly) + ".tlb"))
                                }
            if regAsmResult <> 0 then failwith (sprintf "Register %s with codebase failed" assembly)
            return ()
        }
    assemblies
    |> Seq.map registerAssemblyWithCodebase
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore

/// Executes `Regasm.exe` with the `/codebase /tlb /unregister` options
///
/// Used to unregegister any temporarily registerd .net dependencies
/// _after_ running a VB6 build
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let public UnregisterAssemblies workingDir (assemblies:string seq) =
    use __ = traceStartTaskUsing "Regasm /unregister with codebase" "Registering assemblies with codebase, expect warnings"
    let registerAssemblyWithCodebase assembly =
        async {
            let! regAsmResult = 
                asyncShellExec {defaultParams with 
                                    Program = regAsmToolPath
                                    WorkingDirectory = workingDir
                                    CommandLine = (sprintf "\"%s\" /tlb:%s /codebase /unregister"  assembly ((Path.GetFileName assembly) + ".tlb"))
                                }
            if regAsmResult <> 0 then failwith (sprintf "Unregister %s with codebase failed" assembly)
            return ()
        }
    assemblies
    |> Seq.map registerAssemblyWithCodebase
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
