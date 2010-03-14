[<AutoOpen>]
module Fake.ILMergeHelper

open System

type ILMergeParams =
 { ToolPath: string;
   Version: string;
   Libraries : string seq}

/// ILMerge default params  
let ILMergeDefaults : ILMergeParams =
 { ToolPath = @".\tools\ILMerge\ilmerge.exe";
   Version = "";
   Libraries = []; }
   
let ILMerge setParams outputFile primaryAssembly = 
    traceStartTask "ILMerge" primaryAssembly
    let parameters = ILMergeDefaults |> setParams    

    let args =  
        let version = if parameters.Version <> "" then "/ver:" + parameters.Version else ""
        sprintf "/out:%s /allowDup %s %s %s" 
            outputFile version primaryAssembly
              (separated " " parameters.Libraries)

    trace (parameters.ToolPath + " "  + args)
    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- null
        info.Arguments <- args))
    then
        failwith "ILMerge failed."
                    
    traceEndTask "ILMerge" primaryAssembly