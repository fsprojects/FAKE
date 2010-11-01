[<AutoOpen>]
module Fake.ILMergeHelper

open System

type AllowDuplicateTypes = 
    | NoDuplicateTypes
    | AllPublicTypes
    | DuplicateTypes of string list

type InternalizeTypes =
    | NoInternalize
    | Internalize
    | InternalizeExcept of string

type ILMergeParams =
 { ToolPath: string
   Version: string
   TimeOut: TimeSpan
   Libraries : string seq
   AllowDuplicateTypes: AllowDuplicateTypes
   AllowMultipleAssemblyLevelAttributes: bool
   AllowWildcards: bool
   AllowZeroPeKind: bool 
   AttributeFile: string
   Closed: bool 
   CopyAttributes: bool 
   DebugInfo: bool 
   Internalize: InternalizeTypes
   FileAlignment: int option
   // KeyFile / DelaySign
   // Log / LogFile
   // PublicKeyTokens
   SearchDirectories: string seq
   // TargetPlatform: v1 or v1.1 or v2 or v4 or version,platform
   // TargetKind: Dll or Exe or WinExe // /target 
   UnionMerge: bool 
   XmlDocs: bool 
   }

/// ILMerge default params  
let ILMergeDefaults : ILMergeParams =
    { ToolPath = currentDirectory @@ "tools" @@ "ILMerge" @@ "ilmerge.exe"
      Version = ""
      TimeOut = TimeSpan.FromMinutes 5.
      Libraries = [] 
      AllowDuplicateTypes = NoDuplicateTypes
      AllowMultipleAssemblyLevelAttributes = false 
      AllowWildcards = false
      AllowZeroPeKind = false 
      AttributeFile = null
      Closed = false 
      CopyAttributes = false 
      DebugInfo = true 
      Internalize = NoInternalize
      FileAlignment = None
      SearchDirectories = []
      UnionMerge = false 
      XmlDocs = false }
   
/// Use ILMerge to merge some .NET assemblies.
let ILMerge setParams outputFile primaryAssembly = 
    traceStartTask "ILMerge" primaryAssembly
    let parameters = ILMergeDefaults |> setParams    

    let args =  
        let output = Some("out", outputFile)
        let version = 
            if parameters.Version <> "" 
                then Some("ver", parameters.Version)
                else None
        let attrFile = 
            if isNullOrEmpty parameters.AttributeFile
                then None
                else Some("attr", quote parameters.AttributeFile)
        let fileAlign = 
            match parameters.FileAlignment with
            | Some a -> Some("align", a.ToString())
            | None -> None
        let allowDup = 
            match parameters.AllowDuplicateTypes with
            | NoDuplicateTypes -> [None]
            | AllPublicTypes -> [Some("allowDup", null)]
            | DuplicateTypes types -> types |> List.map (fun t -> Some("allowDup", t))
        let libDirs = 
            parameters.SearchDirectories
            |> Seq.map (fun d -> Some("lib", quote d))
            |> Seq.toList
        let internalize = 
            match parameters.Internalize with
            | NoInternalize -> None
            | Internalize -> Some("internalize", null)
            | InternalizeExcept excludeFile -> Some("internalize", quote excludeFile)
        let booleans = 
            [ parameters.AllowMultipleAssemblyLevelAttributes, "allowMultiple"
              parameters.AllowWildcards, "wildcards"
              parameters.AllowZeroPeKind, "zeroPeKind" 
              parameters.Closed, "closed" 
              parameters.CopyAttributes, "copyattrs"
              parameters.UnionMerge, "union"
              parameters.XmlDocs, "xmldocs" ]
            |> List.map (fun (v,d) -> if v then Some(d, null) else None)
        let notbooleans =
            [ parameters.DebugInfo, "ndebug" ]
            |> List.map (fun (v,d) -> if v then None else Some(d, null))
        let allParameters = 
            [output; attrFile; fileAlign; version; internalize] @ booleans @ notbooleans @ allowDup @ libDirs
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