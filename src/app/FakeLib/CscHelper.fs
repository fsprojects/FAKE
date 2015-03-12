/// Contains tasks to compile C# source files with CSC.EXE (C# Compiler).
module Fake.CscHelper
open System
open System.IO

/// Supported output types
type CscTarget =
    | Exe
    | Winexe
    | Library
    | Module

/// Supported platforms
type CscPlatform =
    | X86
    | Itanium
    | X64
    | AnyCpu32BitPreferred
    | AnyCpu

/// Compiler parameters
type CscParams =
    { /// Specifies the output file name and path.
      Output : string
      /// Specifies the tool path to csc.exe.
      ToolPath : string
      /// Specifies the compiled artifact target type.
      Target : CscTarget
      /// Specifies the compiled artifact target platform.
      Platform : CscPlatform
      /// Specifies assemblies to reference for the compilation.
      References : string list
      /// Specifies whether to emit debug information (default is false).
      Debug : bool
      /// Specifies other params for the compilation. Freeform strings.
      OtherParams : string list }

    /// The default parameters to the compiler.
    static member Default =
        { Output = ""
          Target = Exe
          ToolPath = if isMono then "mcs" else Path.GetDirectoryName(MSBuildHelper.msBuildExe) @@ "csc.exe"
          Platform = AnyCpu
          References = []
          Debug = false
          OtherParams = [] }

let cscExe toolPath (srcFiles : string list) (opts : string list) : int =
    let processResult =
        ExecProcessAndReturnMessages (fun p ->
            p.FileName <- toolPath
            p.Arguments <- [
                opts |> separated " "
                srcFiles |> separated " "
            ] |> separated " "
        ) (TimeSpan.FromMinutes 10.)

    trace <| sprintf "CSC with args:%A" (Array.ofSeq opts)

    processResult.ExitCode

/// Compiles the given C# source files with the specified parameters.
///
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the default CSC parameters.
///  - `inputFiles` - The C# input files.
///
/// ## Returns
///
/// The exit status code of the compile process.
///
/// ## Sample
///
///     ["file1.cs"; "file2.cs"]
///     |> csc (fun parameters ->
///              { parameters with Output = ...
///                                Target = ...
///                                ... })
let csc (setParams : CscParams -> CscParams) (inputFiles : string list) : int =
    let inputFiles = inputFiles |> Seq.toList
    let taskDesc = inputFiles |> separated ", "
    let cscParams = setParams CscParams.Default

    let output = if cscParams.Output <> "" then [sprintf "/out:%s" cscParams.Output] else []
    let target =
        match cscParams.Target with
        | Exe -> [ "/target:exe" ]
        | Winexe -> [ "/target:winexe" ]
        | Library -> [ "/target:library" ]
        | Module -> [ "/target:module" ]
    let platform =
        match cscParams.Platform with
        | X86 -> [ "/platform:x86" ]
        | Itanium -> [ "/platform:itanium" ]
        | X64 -> [ "/platform:x64" ]
        | AnyCpu32BitPreferred -> [ "/platform:anycpu32bitpreferred" ]
        | AnyCpu -> [ "/platform:anycpu" ]
    let references =
        cscParams.References
        |> List.map (fun r -> sprintf "/reference:%s" r)
    let debug = if cscParams.Debug then [ "/debug" ] else []
    let argList =
        output @ target @ platform @ references @ debug @ cscParams.OtherParams
    traceStartTask "Csc " taskDesc
    let res = cscExe cscParams.ToolPath inputFiles argList
    traceEndTask "Csc " taskDesc
    res

/// Compiles one or more C# source files with the specified parameters.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the default CSC parameters.
///  - `inputFiles` - The C# input files.
///
/// ## Sample
///
///     ["file1.cs"; "file2.cs"]
///     |> Csc (fun parameters ->
///                   { parameters with Output = ...
///                                     Target = ...
///                                     ... })
let Csc (setParams : CscParams -> CscParams) (inputFiles : string list) : unit =
    let res = csc setParams inputFiles
    if res <> 0 then raise <| BuildException("Csc: compile failed with exit code", [ string res ])
