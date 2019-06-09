/// Contains task a task which allows to merge .NET assemblies with [ILMerge](http://research.microsoft.com/en-us/people/mbarnett/ilmerge.aspx).
///
/// ### Sample
///
///        Target.create "ILMerge" (fun _ ->
///             let target = !!"./bin/Release/*.exe" |> Seq.head
///             let out = "./bin" @@ (Path.GetFileName target)
///             ILMerge.run
///               { ILMerge.Params.Create() with DebugInfo = true
///                                              TargetKind = ILMerge.TargetKind.Exe
///                                              Internalize = ILMerge.InternalizeTypes.Internalize
///                                              Libraries =
///                                                 Seq.concat
///                                                   [ !!"./bin/Release/Mono.C*.dll"
///                                                     !!"./bin/Release/Newton*.dll" ]
///                                              AttributeFile = target } out target)
///
[<RequireQualifiedAccess>]
module Fake.DotNet.ILMerge

open System
open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open System.Globalization

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
[<NoComparison>]
type Params =
    { /// Path to ILMerge.exe
      ToolPath : string
      /// Version to use for the merged assembly
      Version : Version option
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
    static member Create() =
        { ToolPath = Tools.findToolInSubPath "ilmerge.exe" <| Shell.pwd()
          Version = None
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
let internal getArguments outputFile primaryAssembly parameters =
    let Item a x =
        if x |> String.IsNullOrWhiteSpace then []
        else [ sprintf a x ]

    let ItemList a x =
        if x |> isNull then []
        else
            x
            |> Seq.collect (fun i -> [ sprintf a i ])
            |> Seq.toList

    let Flag a predicate =
        if predicate then [ a ]
        else []

    [ Item "/out:%s" outputFile
      Item "/ver:%s" (match parameters.Version with
                      | Some v -> v.ToString()
                      | None -> String.Empty)
      Item "/attr:%s" parameters.AttributeFile
      Item "/keyfile:%s" parameters.KeyFile
      Item "/log:%s" parameters.LogFile
      Item "/target:%s" <| (sprintf "%A" parameters.TargetKind).ToLower()
      Item "/targetplatform:%s" parameters.TargetPlatform
      Item "/align:%s" (match parameters.FileAlignment with
                        | Some i -> i.ToString(CultureInfo.InvariantCulture)
                        | None -> String.Empty)
      (match parameters.Internalize with
       | NoInternalize -> []
       | Internalize -> Flag "/internalize" true
       | InternalizeExcept excludeFile -> Item "/internalize:%s" excludeFile)
      Flag "/allowMultiple" parameters.AllowMultipleAssemblyLevelAttributes
      Flag "/wildcards" parameters.AllowWildcards
      Flag "/zeroPeKind" parameters.AllowZeroPeKind
      Flag "/closed" parameters.Closed
      Flag "/copyattrs" parameters.CopyAttributes
      Flag "/union" parameters.UnionMerge
      Flag "/ndebug" (not parameters.DebugInfo)
      Flag "/xmldocs" parameters.XmlDocs
      (match parameters.AllowDuplicateTypes with
       | NoDuplicateTypes -> []
       | AllPublicTypes -> Flag "/allowDup" true
       | DuplicateTypes types -> ItemList "/allowDup:%s" types)
      ItemList "/lib:%s" parameters.SearchDirectories
      (primaryAssembly :: (parameters.Libraries |> Seq.toList)) ]
    |> List.concat

/// Builds the process to run for the ILMerge task
/// [omit]

let internal createProcess parameters outputFile primaryAssembly =
    let args = getArguments outputFile primaryAssembly parameters
    CreateProcess.fromRawCommand parameters.ToolPath args

/// Uses ILMerge to merge .NET assemblies.
/// ## Parameters
///
///  - `parameters` - An ILMerge.Params value with your required settings.
///  - `outputFile` - Output file path for the merged assembly.
///  - `primaryAssembly` - The assembly you want ILMerge to consider as the primary.
let run parameters outputFile primaryAssembly =
    // The type initializer for 'System.Compiler.CoreSystemTypes' throws on Mono.
    // So let task be a no-op on non-Windows platforms
    if Environment.isWindows then
        use __ = Trace.traceTask "ILMerge" primaryAssembly
        let run = createProcess parameters outputFile primaryAssembly |> Proc.run
        if 0 <> run.ExitCode then
            let args = getArguments outputFile primaryAssembly parameters
            failwithf "'ILMerge %s' failed." (String.separated " " args)
        __.MarkSuccess()
    else
        raise <| NotSupportedException("ILMerge is currently not supported on mono")
