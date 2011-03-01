[<AutoOpen>]
module Fake.ILMergeHelper

open System

type AllowDuplicateTypes = 
/// No duplicates of public types allowed
| NoDuplicateTypes
/// All public types are allowed to be duplicated and renamed
| AllPublicTypes
/// List of types to allow to be duplicated
| DuplicateTypes of string list

type InternalizeTypes =
| NoInternalize
| Internalize
| InternalizeExcept of string

type TargetKind =
| Library
| Exe
| WinExe

type ILMergeParams = {
   /// Path to ILMerge.exe
   ToolPath: string
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
   TargetKind: TargetKind
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
      TargetKind = Library
      UnionMerge = false 
      XmlDocs = false }

/// Builds the arguments for the ILMerge task
let getArguments outputFile primaryAssembly parameters =
    let stringParams =
        ["out", outputFile
         "ver", parameters.Version
         "attr", parameters.AttributeFile
         "keyfile", parameters.KeyFile
         "log", parameters.LogFile
         "target", parameters.TargetKind.ToString().ToLower()
         "targetplatform", parameters.TargetPlatform]
          |> List.map stringParam

    let fileAlign = optionParam("align",parameters.FileAlignment)

    let allowDup = 
        match parameters.AllowDuplicateTypes with
        | NoDuplicateTypes -> [None]
        | AllPublicTypes -> [Some("allowDup", null)]
        | DuplicateTypes types -> multipleStringParams "allowDup" types

    let libDirs = multipleStringParams "lib" parameters.SearchDirectories

    let internalize = 
        match parameters.Internalize with
        | NoInternalize -> None
        | Internalize -> Some("internalize", null)
        | InternalizeExcept excludeFile -> Some("internalize", quote excludeFile)

    let flags = 
        ["allowMultiple", parameters.AllowMultipleAssemblyLevelAttributes
         "wildcards", parameters.AllowWildcards
         "zeroPeKind" , parameters.AllowZeroPeKind
         "closed", parameters.Closed
         "copyattrs", parameters.CopyAttributes
         "union", parameters.UnionMerge
         "ndebug", not parameters.DebugInfo
         "xmldocs", parameters.XmlDocs ]
           |> List.map boolParam

    let allParameters = 
        stringParams @ [fileAlign; internalize] @ flags @ allowDup @ libDirs
            |> parametersToString "/" ":"

    let libraries = primaryAssembly :: (parameters.Libraries |> Seq.toList) |> separated " "
    allParameters + " " + libraries
   
/// Use ILMerge to merge some .NET assemblies.
let ILMerge setParams outputFile primaryAssembly = 
    traceStartTask "ILMerge" primaryAssembly
    let parameters = setParams ILMergeDefaults

    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- null
        info.Arguments <- getArguments outputFile primaryAssembly parameters) parameters.TimeOut)
    then
        failwith "ILMerge failed."
                    
    traceEndTask "ILMerge" primaryAssembly