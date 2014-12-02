/// Contains tasks to compiles F# source file with the [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service).
/// There is also a tutorial about the [F# compiler tasks](../fsc.html) available.
module Fake.FscHelper

open Microsoft.FSharp.Compiler.SimpleSourceCodeServices

/// The 'fsc.exe' output target types
type FscTarget = 
    | Exe
    | Winexe
    | Library
    | Module

/// The 'fsc.exe' output platforms
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

    trace <| sprintf "FSC with args:%A" optsArr
    // Always prepend "fsc.exe" since fsc compiler skips the first argument
    let optsArr = Array.append [|"fsc.exe"|] optsArr
    let errors, exitCode = scs.Compile(optsArr)
    // Better compile reporting thanks to:
    // https://github.com/jbtule/ComposableExtensions/blob/5b961b30668bb7f4d17238770869b5a884bc591f/tools/CompilerHelper.fsx#L233
    for e in errors do
        let errMsg = e.ToString()
        match e.Severity with
        | Microsoft.FSharp.Compiler.Warning -> traceImportant errMsg
        | Microsoft.FSharp.Compiler.Error -> traceError errMsg
    exitCode

/// Compiles the given F# source files with the specified parameters.
///
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the default Fsc parameters.
///  - `inputFiles` - The F# input files.
///
/// ## Returns
///
/// The exit status code of the compile process.
///
/// ## Sample
///
///     ["file1.fs"; "file2.fs"]
///     |> fsc (fun parameters ->
///              { parameters with Output = ...
///                                FscTarget = ...
///                                ... })
let fsc (setParams : FscParams -> FscParams) (inputFiles : string list) : int = 
    let inputFiles = inputFiles |> Seq.toList
    let taskDesc = inputFiles |> separated ", "
    let fscParams = setParams FscParams.Default

    let output = if fscParams.Output <> "" then [ "-o"; fscParams.Output ] else []
    let target =
        match fscParams.FscTarget with
        | Exe -> [ "--target:exe" ]
        | Winexe -> [ "--target:winexe" ]
        | Library -> [ "-a" ]
        | Module -> [ "--target:module" ]
    let platform =
        match fscParams.Platform with
        | X86 -> [ "--platform:x86" ]
        | Itanium -> [ "--platform:itanium" ]
        | X64 -> [ "--platform:x64" ]
        | AnyCpu32BitPreferred -> [ "--platform:anycpu32bitpreferred" ]
        | AnyCpu -> [ "--platform:anycpu" ]
    let references =
        let refs =
            fscParams.References
            |> List.map (fun r -> sprintf "--reference:%s" r)
        let isNonDefaultFramework =
            fscParams.References
            |> List.exists (fun r->
                r.IndexOf("FSharp.Core.dll", System.StringComparison.InvariantCultureIgnoreCase) >= 0
                || r.IndexOf("mscorlib.dll", System.StringComparison.InvariantCultureIgnoreCase) >= 0)
        if isNonDefaultFramework then "--noframework"::refs else refs
    let debug = if fscParams.Debug then [ "-g" ] else []
    let argList =
        output @ target @ platform @ references @ debug @ fscParams.OtherParams
    traceStartTask "Fsc " taskDesc
    let res = fscList inputFiles argList
    traceEndTask "Fsc " taskDesc
    res

/// Compiles one or more F# source files with the specified parameters.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the default Fsc parameters.
///  - `inputFiles` - The F# input files.
///
/// ## Sample
///
///     ["file1.fs"; "file2.fs"]
///     |> Fsc (fun parameters ->
///                   { parameters with Output = ...
///                                     FscTarget = ...
///                                     ... })
let Fsc (setParams : FscParams -> FscParams) (inputFiles : string list) : unit = 
    let res = fsc setParams inputFiles
    if res <> 0 then raise <| BuildException("Fsc: compile failed with exit code", [ string res ])
