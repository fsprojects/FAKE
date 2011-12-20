[<AutoOpen>]
module Fake.RegAsmHelper

open System

type RegAsmParams =
    { ToolPath: string
      WorkingDir:string
      TimeOut: TimeSpan}

/// RegAsm default params  
let RegAsmDefaults =
    { ToolPath = @"C:\Windows\Microsoft.NET\Framework\v2.0.50727\regasm.exe"
      WorkingDir = "."
      TimeOut = TimeSpan.FromMinutes 5. }

/// Runs regasm on the given lib
let RegAsm setParams lib = 
    traceStartTask "RegAsm" lib
    let parameters = setParams RegAsmDefaults
    
    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- sprintf "%s /tlb:%s" lib (replace ".dll" ".tlb" lib)) parameters.TimeOut)
    then
        failwith "RegAsm failed."
                  
    traceEndTask "RegAsm" lib