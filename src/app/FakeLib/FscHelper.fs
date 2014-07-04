module Fake.FscHelper

open Microsoft.FSharp.Compiler.SimpleSourceCodeServices

/// 'fsc.exe' output target types
type FscTarget = 
    | Exe
    | Winexe
    | Library
    | Module

/// 'fsc.exe' output platforms
type FscPlatform = 
    | X86
    | Itanium
    | X64
    | AnyCpu32BitPreferred
    | AnyCpu

/// 'fsc.exe' command line parameters
type FscParams = 
    { /// Specifies the output file name and path.
      Output : string
      /// Specifies the compiled artifact target type.
      FscTarget : FscTarget
      /// Specifies the compiled artifact target platform.
      Platform : FscPlatform
      /// Specifies files to reference for the compilation.
      References : string list
      /// Specifies whether to emit debug information (default is false).
      Debug : bool
      /// Specifies other params for the compilation. Freeform strings.
      OtherParams : string list }
    /// The default parameters to the compiler service.
    static member Default = 
        { Output = ""
          FscTarget = Exe
          Platform = AnyCpu
          References = []
          Debug = false
          OtherParams = [] }

/// Compiles the given source file with the given options. If no options
/// given (i.e. the second argument is an empty list), by default tries
/// to behave the same way as would the command-line 'fsc.exe' tool.
///
/// Example usage:
/// Target "MyFile" (fun _ ->
///   fscList ["MyFile.fs"] ["-a"; "-r"; "Common.dll"])
let fscList (srcFiles : string list) (opts : string list) : int = 
    let scs = SimpleSourceCodeServices()
    let optsArr = 
        // If output file name is specified, pass it on to fsc.
        if Seq.exists (fun e -> e = "-o" || e.StartsWith("--out:")) opts then opts @ srcFiles
        // But if it's not, then figure out what it should be.
        else 
            let outExt = 
                if Seq.exists (fun e -> e = "-a" || e = "--target:library") opts then ".dll"
                else ".exe"
            "-o" :: FileHelper.changeExt outExt (List.head srcFiles) :: opts @ srcFiles
        |> Array.ofList
    
    let errors, exitCode = scs.Compile(optsArr)
    // Better compile reporting thanks to:
    // https://github.com/jbtule/ComposableExtensions/blob/5b961b30668bb7f4d17238770869b5a884bc591f/tools/CompilerHelper.fsx#L233
    for e in errors do
        let errMsg = e.ToString()
        match e.Severity with
        | Microsoft.FSharp.Compiler.Warning -> traceImportant errMsg
        | Microsoft.FSharp.Compiler.Error -> traceError errMsg
    exitCode

/// Compiles one or more F# source files with the specified parameters.
/// Can be called as:
///
/// ["file1.fs"; "file2.fs"]
/// |> fsc (fun parameters ->
///   { parameters with Output = ...
///                     FscTarget = ...
///                     ... })
///
/// Returns the exit code of the compile run.
let fsc (fscParamSetter : FscParams -> FscParams) (inputFiles : string list) : int = 
    let inputFiles = inputFiles |> Seq.toList
    let fscParams = fscParamSetter FscParams.Default
    let output = fscParams.Output
    
    let argList = 
        if output <> "" then [ "-o"; output ]
        else [] 
        @ match fscParams.FscTarget with
          | Exe -> [ "--target:exe" ]
          | Winexe -> [ "--target:winexe" ]
          | Library -> [ "-a" ]
          | Module -> [ "--target:module" ] 
          @ match fscParams.Platform with
            | X86 -> [ "--platform:x86" ]
            | Itanium -> [ "--platform:itanium" ]
            | X64 -> [ "--platform:x64" ]
            | AnyCpu32BitPreferred -> [ "--platform:anycpu32bitpreferred" ]
            | AnyCpu -> [ "--platform:anycpu" ] 
            @ List.map (fun r -> "--reference:" + r) fscParams.References @ if fscParams.Debug then [ "-g" ]
                                                                            else [] @ fscParams.OtherParams
    traceStartTask "Fsc " (inputFiles |> separated ", ")
    let res = fscList inputFiles argList
    traceEndTask "Fsc " (inputFiles |> separated ", ")
    res

/// Compiles one or more F# source files with the specified parameters.
/// Can be called as:
///
/// ["file1.fs"; "file2.fs"]
/// |> Fsc (fun parameters ->
///   { parameters with Output = ...
///                     FscTarget = ...
///                     ... })
let Fsc (fscParamSetter : FscParams -> FscParams) (inputFiles : string list) : unit = 
    let res = fsc fscParamSetter inputFiles
    if res <> 0 then raise (Fake.MSBuildHelper.BuildException("Fsc: compile failed with exit code", [ string res ]))
    ()
