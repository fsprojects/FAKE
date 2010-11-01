[<AutoOpen>]
module Fake.ILMergeHelper

open System

type AllowDuplicateTypes = 
    | No
    | All
    | Types of string list

type ILMergeParams =
 { ToolPath: string
   Version: string
   TimeOut: TimeSpan
   Libraries : string seq
   AllowDuplicateTypes: AllowDuplicateTypes }

/// ILMerge default params  
let ILMergeDefaults : ILMergeParams =
    { ToolPath = currentDirectory @@ "tools" @@ "ILMerge" @@ "ilmerge.exe"
      Version = ""
      TimeOut = TimeSpan.FromMinutes 5.
      Libraries = [] 
      AllowDuplicateTypes = No }
   
/// Use ILMerge to merge some .NET assemblies.
let ILMerge setParams outputFile primaryAssembly = 
    traceStartTask "ILMerge" primaryAssembly
    let parameters = ILMergeDefaults |> setParams    

    let args =  
        let version = 
            if parameters.Version <> "" 
                then Some("ver", parameters.Version)
                else None
        let output = Some("out", outputFile)
        let allowDup = 
            match parameters.AllowDuplicateTypes with
            | No -> [None]
            | All -> [Some("allowDup", null)]
            | Types types -> types |> List.map (fun t -> Some("allowDup", t))
        let allParameters = 
            [output; version] @ allowDup
            |> Seq.choose id
            |> Seq.map (fun (k,v) -> "/" + k + (if isNullOrEmpty v then "" else ":" + v))
            |> separated " "
        let libraries = primaryAssembly + " " + (separated " " parameters.Libraries)
        allParameters + " " + libraries

    tracefn "%s %s" parameters.ToolPath args
    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- null
        info.Arguments <- args) parameters.TimeOut)
    then
        failwith "ILMerge failed."
                    
    traceEndTask "ILMerge" primaryAssembly