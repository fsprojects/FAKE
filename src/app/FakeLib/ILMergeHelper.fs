[<AutoOpen>]
/// Contains task a task which allows to merge .NET assemblies with [ILMerge](http://research.microsoft.com/en-us/people/mbarnett/ilmerge.aspx).
module Fake.ILMergeHelper

open System

/// Option type to configure ILMerge's processing of duplicate types.
type AllowDuplicateTypes = 
    /// No duplicates of public types allowed
    | NoDuplicateTypes
    /// All public types are allowed to be duplicated and renamed
    | AllPublicTypes
    /// List of types to allow to be duplicated
    | DuplicateTypes of string list

/// Option type to configure ILMerge's processing of internal types.
type InternalizeTypes = 
    | NoInternalize
    | Internalize
    | InternalizeExcept of string

/// Option type to configure ILMerge's target output.
type TargetKind = 
    | Library
    | Exe
    | WinExe

/// Parameter type for ILMerge
type ILMergeParams = 
    { /// Path to ILMerge.exe
      ToolPath : string
      /// Version to use for the merged assembly
      Version : string
      TimeOut : TimeSpan
      /// Assemblies to merge with the primary assembly
      Libraries : string seq
      /// Duplicate types policy
      AllowDuplicateTypes : AllowDuplicateTypes
      /// Assembly-level attributes names that have the same type are copied over into the target directory
      AllowMultipleAssemblyLevelAttributes : bool
      /// Wild cards in file names are expanded and all matching files will be used as input.
      AllowWildcards : bool
      AllowZeroPeKind : bool
      /// Path to an assembly that will be used to get all of the assembly-level attributes
      AttributeFile : string
      /// True -> transitive closure of the input assemblies is computed and added to the list of input assemblies.
      Closed : bool
      CopyAttributes : bool
      /// True (default) -> creates a .pdb file for the output assembly and merges into it any .pdb files found for input assemblies.
      DebugInfo : bool
      Internalize : InternalizeTypes
      FileAlignment : int option
      KeyFile : string
      // DelaySign
      LogFile : string
      // PublicKeyTokens
      /// Directories to be used to search for input assemblies
      SearchDirectories : string seq
      /// v1 or v1.1 or v2 or v4 or version,platform
      TargetPlatform : string
      TargetKind : TargetKind
      /// True -> types with the same name are all merged into a single type in the target assembly.
      UnionMerge : bool
      /// True -> XML documentation files are merged to produce an XML documentation file for the target assembly.
      XmlDocs : bool }

/// ILMerge default parameters. Tries to automatically locate ilmerge.exe in a subfolder.
let ILMergeDefaults : ILMergeParams = 
    { ToolPath = findToolInSubPath "ilmerge.exe" (currentDirectory @@ "tools" @@ "ILMerge")
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
/// [omit]
let getArguments outputFile primaryAssembly parameters = 
    let stringParams = 
        [ "out", outputFile
          "ver", parameters.Version
          "attr", parameters.AttributeFile
          "keyfile", parameters.KeyFile
          "log", parameters.LogFile
          "target", (sprintf "%A" parameters.TargetKind).ToLower()
          "targetplatform", parameters.TargetPlatform ]
        |> List.map stringParam
    
    let fileAlign = optionParam ("align", parameters.FileAlignment)
    
    let allowDup = 
        match parameters.AllowDuplicateTypes with
        | NoDuplicateTypes -> [ None ]
        | AllPublicTypes -> [ Some("allowDup", null) ]
        | DuplicateTypes types -> multipleStringParams "allowDup" types
    
    let libDirs = multipleStringParams "lib" parameters.SearchDirectories
    
    let internalize = 
        match parameters.Internalize with
        | NoInternalize -> None
        | Internalize -> Some("internalize", null)
        | InternalizeExcept excludeFile -> Some("internalize", quote excludeFile)
    
    let flags = 
        [ "allowMultiple", parameters.AllowMultipleAssemblyLevelAttributes
          "wildcards", parameters.AllowWildcards
          "zeroPeKind", parameters.AllowZeroPeKind
          "closed", parameters.Closed
          "copyattrs", parameters.CopyAttributes
          "union", parameters.UnionMerge
          "ndebug", not parameters.DebugInfo
          "xmldocs", parameters.XmlDocs ]
        |> List.map boolParam
    
    let allParameters = stringParams @ [ fileAlign; internalize ] @ flags @ allowDup @ libDirs
                        |> parametersToString "/" ":"
    let libraries = primaryAssembly :: (parameters.Libraries |> Seq.toList)
                    |> separated " "
    allParameters + " " + libraries

/// Uses ILMerge to merge .NET assemblies.
/// ## Parameters
///
///  - `setParams` - Function used to create an ILMergeParams value with your required settings. Called with an ILMergeParams value configured with the defaults.
///  - `outputFile` - Output file path for the merged assembly.
///  - `primaryAssembly` - The assembly you want ILMerge to consider as the primary.
let ILMerge setParams outputFile primaryAssembly = 
    traceStartTask "ILMerge" primaryAssembly
    let parameters = setParams ILMergeDefaults
    let args = getArguments outputFile primaryAssembly parameters
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath
                info.WorkingDirectory <- null
                info.Arguments <- args) parameters.TimeOut
    then failwithf "ILMerge %s failed." args
    traceEndTask "ILMerge" primaryAssembly
