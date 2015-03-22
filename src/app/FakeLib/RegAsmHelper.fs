[<AutoOpen>]
/// Contains a task which can be used to run regasm .NET assembly
module Fake.RegAsmHelper

open System
open System.IO

/// Path to newest `regasm.exe`
let regAsmToolPath = !! (TargetPlatformPrefix + "/**/RegAsm.exe")  
                             |> getNewestTool

/// RegAsm parameter type
type RegAsmParams = 
    { ToolPath : string
      WorkingDir : string
      TimeOut : TimeSpan
      ExportTypeLibrary : bool }

/// RegAsm default params
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
let RegAsm setParams lib = 
    traceStartTask "RegAsm" lib
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
    traceEndTask "RegAsm" lib

/// Executes `RegAsm.exe` with the `/codebase` `/tlb` option
///
/// Used to temporarily register any .net dependencies before running 
/// a VB6 build
let public RegisterAssembliesWithCodebase workingDir (assemblies:string seq) =
    traceStartTask "Regasm with codebase" "Registering assemblies with codebase, expect warnings"
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
    traceEndTask "Regasm with codebase" "Registering assemblies with codebase, expect warnings"

/// Executes `Regasm.exe` with the `/codebase /tlb /unregister` options
///
/// Used to unregegister any temporarily registerd .net dependencies
/// _after_ running a VB6 build
let public UnregisterAssemblies workingDir (assemblies:string seq) =
    traceStartTask "Regasm /unregister with codebase" "Registering assemblies with codebase, expect warnings"
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
    traceEndTask "Regasm /unregister with codebase" "Registering assemblies with codebase, expect warnings"