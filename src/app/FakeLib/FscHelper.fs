module Fake.FscHelper

let private scs =
  Microsoft.FSharp.Compiler.SimpleSourceCodeServices.SimpleSourceCodeServices()

/// 'fsc.exe' output target types
type FscTarget = Exe | Winexe | Library | Module
type FscPlatform = X86 | Itanium | X64 | AnyCpu32BitPreferred | AnyCpu

/// 'fsc.exe' command line parameters
type FscParams =
  { /// Specifies the output file name and path.
    Output: string
    /// Specifies the compiled artifact target type.
    FscTarget: FscTarget
    /// Specifies the compiled artifact target platform.
    Platform: FscPlatform
    /// Specifies files to reference for the compilation.
    References: string list
    /// Specifies whether to emit debug information (default is false).
    Debug: bool
    /// Specifies other params for the compilation. Freeform strings.
    OtherParams: string list }

let FscDefaultParams =    
    // These are the default params to the compiler service.
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
///   fsc ["MyFile.fs"] ["-a"; "-r"; "Common.dll"])
let fscList (srcFiles: string list) (opts: string list): int =
  let optsArr =
    // If output file name is specified, pass it on to fsc.
    if Seq.exists (fun e -> e = "-o" || e.StartsWith("--out:")) opts
      then opts @ srcFiles
      // But if it's not, then figure out what it should be.
      else
        let outExt =
          if Seq.exists
            (fun e -> e = "-a" || e = "--target:library")
            opts
            then ".dll"
            else ".exe"
        "-o" :: FileHelper.changeExt outExt (List.head srcFiles) :: opts @ srcFiles
    |> Array.ofList
  let errors, exitCode = scs.Compile(optsArr)

  errors |> Seq.iter (fun (e: Microsoft.FSharp.Compiler.ErrorInfo) ->
    traceError e.Message)
  exitCode

/// Compiles one or more F# source files with the specified parameters.
/// Can be called as:
///
/// Fsc (fun params ->
///   { params with
///     Inputs = ...
///     Output = ...
///     FscTarget = ...
///     ... })
///   ["file1.fsx"; "file2.fsx"]
///
/// Returns the exit code of the compilation process.
let Fsc (fscParamSetter: FscParams -> FscParams) inputFiles =
  let fscParams = fscParamSetter FscDefaultParams
  let output = fscParams.Output
  let argList =
    if output <> "" then ["-o"; output] else []
    @ match fscParams.FscTarget with
      | Exe -> ["--target:exe"]
      | Winexe -> ["--target:winexe"]
      | Library -> ["-a"]
      | Module -> ["--target:module"]
    @ match fscParams.Platform with
      | X86 -> ["--platform:x86"]
      | Itanium -> ["--platform:itanium"]
      | X64 -> ["--platform:x64"]
      | AnyCpu32BitPreferred -> ["--platform:anycpu32bitpreferred"]
      | AnyCpu -> ["--platform:anycpu"]
    @ List.map (fun r -> "--reference:" + r) fscParams.References
    @ if fscParams.Debug then ["-g"] else []
    @ fscParams.OtherParams

  fscList (inputFiles |> Seq.toList) argList |> ignore
