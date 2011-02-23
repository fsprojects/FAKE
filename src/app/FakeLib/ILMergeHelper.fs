[<AutoOpen>]
module Fake.ILMergeHelper

open System

type AllowDuplicateTypes = 
    /// No duplicates of public types allowed
    | NoDuplicateTypes
    /// All public types are allowed to be duplicate and renamed
    | AllPublicTypes
    /// List of types to allow to be duplicate
    | DuplicateTypes of string list

type InternalizeTypes =
    | NoInternalize
    | Internalize
    | InternalizeExcept of string

type ILMergeParams =
   /// Path to ILMerge.exe
 { ToolPath: string
   /// Version to use for the merged assembly
   Version: string
   TimeOut: TimeSpan
   /// Assemblies to merge with the primary assembly
   Libraries : string seq
   /// Duplicate types policy
   AllowDuplicateTypes: AllowDuplicateTypes
   /// Assembly-level attributes names that have the same type are copied over into the target directory
   AllowMultipleAssemblyLevelAttributes: bool
   /// Wild cards in file names are expanded and all matching files will be used as input.
   AllowWildcards: bool
   AllowZeroPeKind: bool 
   /// Path to an assembly that will be used to get all of the assembly-level attributes
   AttributeFile: string
   /// True -> transitive closure of the input assemblies is computed and added to the list of input assemblies.
   Closed: bool 
   CopyAttributes: bool
   /// True (default) -> creates a .pdb file for the output assembly and merges into it any .pdb files found for input assemblies.
   DebugInfo: bool 
   Internalize: InternalizeTypes
   FileAlignment: int option
   KeyFile: string
   // DelaySign
   LogFile : string
   // PublicKeyTokens
   /// Directories to be used to search for input assemblies
   SearchDirectories: string seq
   /// v1 or v1.1 or v2 or v4 or version,platform
   TargetPlatform: string
   // TargetKind: Dll or Exe or WinExe // /target 
   /// True -> types with the same name are all merged into a single type in the target assembly.
   UnionMerge: bool
   /// True -> XML documentation files are merged to produce an XML documentation file for the target assembly.
   XmlDocs: bool 
   }

/// ILMerge default params  
let ILMergeDefaults : ILMergeParams =
    { ToolPath = @".\tools\ILMerge\ilmerge.exe"
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
      KeyFile = null
      TargetPlatform = null
      LogFile = null
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
        let keyFile = 
            if isNullOrEmpty parameters.KeyFile
                then None
                else Some("keyfile", quote parameters.KeyFile)
        let logFile = 
            if isNullOrEmpty parameters.LogFile
                then None
                else Some("log", quote parameters.KeyFile)
        let fileAlign = 
            match parameters.FileAlignment with
            | Some a -> Some("align", a.ToString())
            | None -> None
        let targetPlatform = 
            if isNullOrEmpty parameters.TargetPlatform
                then None
                else Some("targetplatform", quote parameters.TargetPlatform)
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
            [output; attrFile; keyFile; logFile; fileAlign; version; internalize; targetPlatform] 
               @ booleans @ notbooleans @ allowDup @ libDirs
            |> Seq.choose id
            |> Seq.map (fun (k,v) -> "/" + k + (if isNullOrEmpty v then "" else ":" + v))
            |> separated " "
        let libraries = primaryAssembly + " " + (separated " " parameters.Libraries)
        allParameters + " " + libraries

    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- null
        info.Arguments <- args) parameters.TimeOut)
    then
        failwith "ILMerge failed."
                    
    traceEndTask "ILMerge" primaryAssembly