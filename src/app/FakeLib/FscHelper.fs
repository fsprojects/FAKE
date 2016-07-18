/// Contains tasks to compiles F# source file with the [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service).
/// There is also a tutorial about the [F# compiler tasks](../fsc.html) available.
module Fake.FscHelper

open System
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SimpleSourceCodeServices

// OBSOLETE

/// The 'fsc.exe' output target types
[<Obsolete "Use the FscHelper.TargetType instead">]
type FscTarget = 
    | Exe
    | Winexe
    | Library
    | Module

/// The 'fsc.exe' output platforms
[<Obsolete "Use FscHelper.PlatformType instead">]
type FscPlatform = 
    | X86
    | Itanium
    | X64
    | AnyCpu32BitPreferred
    | AnyCpu

/// 'fsc.exe' command line parameters
[<Obsolete "Use FscHelper.FscParam instead">]
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
[<Obsolete "Use FscHelper.compileFiles instead">]
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
        | FSharpErrorSeverity.Warning -> traceImportant errMsg
        | FSharpErrorSeverity.Error -> traceError errMsg
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
[<Obsolete "Use FscHelper.compile instead">]
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
[<Obsolete "Use FscHelper.Compile instead">]
let Fsc (setParams : FscParams -> FscParams) (inputFiles : string list) : unit = 
    let res = fsc setParams inputFiles
    if res <> 0 then raise <| BuildException("Fsc: compile failed with exit code", [ string res ])


// FSCHELPER vNEXT


type TargetType = 
    /// Build a console executable
    | Exe
    ///  Build a Windows executable
    | Winexe
    /// Build a library
    | Library
    /// Build a module that can be added to another assembly (.netmodule)
    | Module
    override self.ToString () =
        match self with
        | Exe     -> "exe"
        | Winexe  -> "winexe"
        | Library -> "library"
        | Module  -> "module"


/// Limit which platforms the compiled code can run on: 
///     x86, Itanium, x64, anycpu32bitpreferred, or anycpu. 
/// The default is anycpu.
type PlatformType = 
    | X86 | Itanium | X64 | AnyCpu32BitPreferred | AnyCpu
    override self.ToString () =
        match self with
        | X86                  -> "x86" 
        | Itanium              -> "Itanium"
        | X64                  -> "x64"
        | AnyCpu32BitPreferred -> "anycpu32bitpreferred"
        | AnyCpu               -> "anycpu"

/// Specify debugging type: full, pdbonly. 
/// ('full' is the default and enables attaching a debugger to a running program).
type DebugType = 
    | Full | PdbOnly
    override self.ToString () =
        (function Full -> "full" | PdbOnly -> "pdbonly") self

/// Specify target framework profile of this assembly. 
/// Valid values are mscorlib or netcore. Default - mscorlib
type Profile = 
    | MsCorlib | Netcore
    override self.ToString () =
        (function MsCorlib -> "mscorlib" | Netcore -> "netcore") self

/// Used to set the Accessiblity of an embeded or linked resource 
type Access = 
    | Public | Private
    override self.ToString () =
        (function Public -> "public" | Private -> "private") self


/// Optimization options that can be disabled or enabled selectively by listing them
/// with the optimize compiler flag
type Optimization =
    | NoJitOptimize | NoJitTracking | NoLocalOptimize | NoCrossoptimize | NoTailcalls
    override self.ToString () =
        match self with
        | NoJitOptimize    -> "nojitoptimize" 
        | NoJitTracking    -> "nojittracking"
        | NoLocalOptimize  -> "nolocaloptimize"
        | NoCrossoptimize  -> "nocrossoptimize"
        | NoTailcalls      -> "notailcalls"


/// Specified path of a managed resource with an optional name alias and accessiblity flag
/// resinfo format is <file>[,<stringname>[,public|private]]
/// e.g. `resource.dat,rezName,public`
type ResourceInfo = string * string option * Access option

let resourceStr ((file, name, access):ResourceInfo) =
    match file, name, access with
    | f, None   , None      -> f
    | f, Some n , None      -> sprintf "%s,%s" f n
    | f, None   , Some a    -> sprintf "%s,%s" f (string a)
    | f, Some n , Some a    -> sprintf "%s,%s,%s" f n (string a)

type FscParam =
(* - OUTPUT FILES - *)
    /// Name of the output file
    | Out of file:string
    /// The 'fsc.exe' output target types : exe, winexe, library, module
    | Target of TargetType
    /// Delay-sign the assembly using only the public portion of the strong name key
    | DelaySign of on:bool
    /// Write the xmldoc of the assembly to the given file
    | Doc of file:string
    /// Specify a strong name key file
    | KeyFile of file:string
    /// Specify a strong name key container
    | KeyContainer of name:string
    /// Limit which platforms the compiled code can run on: 
    | Platform of platform:PlatformType
    /// Only include optimization information essential for implementing inlined constructs. 
    /// Inhibits cross-module inlining but improves binary compatibility.
    | NoOptimizationData
    /// Don't add a resource to the generated assembly containing F#-specific metadata
    | NoInterfacedata
    /// Print the inferred interface of the assembly to a file
    | Sig of file:string

(* - INPUT FILES - *)
    /// Reference an assembly
    | Reference of dllPath:string
    /// Reference assemblies in the order listed
    | References of dllPaths:string list

(* - RESOURCES - *)
    /// Specify a Win32 resource file (.res)
    | Win32res of file:string
    /// Specify a Win32 manifest file
    | Win32Manifest of file:string
    /// Do not include the default Win32 manifest
    | NoWin32Manifest
    /// Embed the specified managed resource
    | Resource of resInfo:ResourceInfo
    /// Link the specified resource to this assembly
    | LinkResource of resInfo:ResourceInfo

(* - CODE GENERATION - *)
    /// Emit debug information
    | Debug of on:bool
    /// Specify debugging type: full, pdbonly. 
    /// ('full' is the default and enables attaching a debugger to a running program).
    | DebugType of debugType:DebugType
    /// Enable optimizations
    | Optimize of on:bool * optimizations:Optimization list
    /// Enable or disable tailcalls
    | Tailcalls of on:bool
    /// Enable or disable cross-module optimizations
    | CrossOptimize of on:bool

(* - ERRORS AND WARNINGS - *)
    /// Report all warnings as errors
    | WarnAsError of on:bool
    /// Report specific warnings as errors
    | WarnAsErrors of on:bool * warningCodes:int list
    /// Set a warning level (0-5)
    | Warn of level:int
    /// Disable specific warning messages
    | NoWarn of warningCodes:int list
    /// Enable specific warnings that may be off by default
    | WarnOn of warningCodes:int list
    /// Output warning and error messages in color
    | ConsoleColors of on:bool

(* - LANGUAGE - *)
    /// Generate overflow checks
    | Checked of on:bool
    /// Define a conditional compilation symbols
    | Define of symbol:string
    /// Define a list of conditional compilation symbols
    | Definitions of symbols: string list
    /// Ignore ML compatibility warnings
    | MLCompatibility

(* - MISCELLANEOUS - *)
    /// Suppress compiler copyright message
    | NoLogo
    ///  Display the commandline flags and their usage
    | Help

(* - ADVANCED - *)
    /// Specify the codepage used to read source files
    | Codepage of n:int 
    /// Displays timing information for compilation.
    | Times
    /// Output messages in UTF-8 encoding
    | Utf8Output
    /// Output messages with fully qualified paths
    | FullPaths
    ///  Specify a directory for the include path which is used to resolve source files and assemblies
    | Lib of directories:string list
    /// Base address for the library to be built
    | BaseAddress of address:string
    /// Do not reference the default CLI assemblies by default
    | NoFramework
    /// Statically link the F# library and all referenced DLLs 
    /// that depend on it into the assembly being generated
    | Standalone
    /// Statically link the given assembly and all referenced DLLs that depend on this assembly. 
    /// Use an assembly name e.g. mylib, not a DLL name.
    | StaticLink of assemblyName:string
    /// Name the output debug file
    | Pdb of debugFile:string
    /// Resolve assembly references using directory-based rules rather than MSBuild resolution
    | SimpleResolution
    /// Enable high-entropy ASLR
    | HighEntropyVA of on:bool
    /// Specifies the version of the OS subsystem to be used by the generated executable. 
    /// Use 6.02 for Windows 8, 6.01 for Windows 7, 6.00 for Windows Vista. 
    /// This option only applies to executables, not DLL  and need only be used if your application 
    /// depends on specific security features available only on certain versions of the OS
    | SubsystemVersion of version:string
    /// Specify target framework profile of this assembly.
    | TargetProfile of profile:Profile
    /// Emit debug information in quotations
    | QuotationsDebug of on:bool

    override self.ToString () =
        // commandline formatting helper functions 
        /// format a standalone compiler arg: "--%s"
        let arg s = sprintf "--%s" s
        /// format a compiler arg with a parameter: "--%s:%s"
        let argp s p = sprintf "--%s:%s" s p            
        /// format a short form compiler arg with a parameter: "-%s:%s"
        let sargp s p = sprintf "-%s:%s" s p  // for short forms          
        /// helper function to convert a bool to a "+" or "-"
        let inline chk b = if b then "+" else "-"
        /// format a compiler arg that ends with "+" or "-": "--%s%s"
        let togl s b = sprintf "--%s%s" s (chk b)
        /// format a short form compiler arg that ends with "+" or "-": "-%s%s"
        let stogl s b = sprintf "-%s%s" s (chk b) // for short forms       
        /// format a list of compiler args with string parameters "--%s:\"%s\""   
        let argls s (ls:string list) = sprintf "--%s:\"%s\"" s (String.concat ";" ls)
        /// format a complier arg that ends with "+" or "-" with string parameters  "--%s%s:\"%s\""
        let inline toglls s b (ls:'a list) = 
            sprintf "--%s%s:\"%s\"" s  (chk b) (String.concat ";" (List.map string ls))
        /// format a list of short form complier args using the same symbol 
        let sargmap sym ls = ls |> List.map (sargp sym) |> String.concat ";" 

        match self with
        | Out file -> argp "out" file
        | Target t -> argp "target" <| string t
        | DelaySign on -> togl "delaysign" on
        | Doc file ->  argp "doc" file
        | KeyFile file -> argp "keyfile" file
        | KeyContainer name -> argp "keycontainer" name
        | Platform p -> argp "platform" <| string p
        | NoOptimizationData -> arg "nooptimizationdata"
        | NoInterfacedata -> arg "nointerfacedata"
        | Sig file -> argp "sig" file
        | Reference dllPath -> sargp "r" dllPath
        | References dllPaths -> dllPaths |> List.map (sargp "r") |> String.concat " "
        | Win32res file -> argp "win32res" file
        | Win32Manifest file -> argp "win32manifest" file
        | NoWin32Manifest -> arg "nowin32manifest"
        | Resource rinfo -> argp "resource" <| resourceStr rinfo
        | LinkResource rinfo -> argp "linkresource" <| resourceStr rinfo
        | Debug on -> stogl "g" on
        | DebugType dt -> argp "debug" <| string dt
        | Optimize (on,opts)-> 
            match opts with
            | [] -> stogl "O" on
            | _ -> toglls "O" on opts
        | Tailcalls on -> togl "tailcalls" on
        | CrossOptimize on -> togl "crossoptimize" on
        | WarnAsError on -> togl "warnaserror" on
        | WarnAsErrors (on, warningCodes) ->  toglls "warnaserror" on warningCodes
        | Warn lvl -> argp "warn" <| string (if lvl < 0 then 0 elif lvl > 5 then 5 else lvl)
        | NoWarn warningCodes -> argls "nowarn" (List.map string warningCodes)
        | WarnOn warningCodes -> argls "warnon" (List.map string warningCodes)
        | ConsoleColors on -> togl "consolecolors" on
        | Checked on -> togl "checked" on
        | Define symbol -> sargp "d" symbol
        | Definitions symbols -> sargmap "d" symbols
        | MLCompatibility -> arg "mlcompatibility"
        | NoLogo -> arg "nologo"
        | Help -> arg "help"
        | Codepage n -> argp "codepage" <| string n
        | Utf8Output -> arg "utf8output"
        | FullPaths -> arg "fullpaths"
        | Lib directories -> argls "lib" directories
        | BaseAddress address -> argp "baseaddress" address
        | NoFramework -> arg "noframework"
        | Standalone -> arg "standalone"
        | StaticLink file -> argp "staticlink" file
        | Pdb debugFile -> argp "pdb" debugFile
        | SimpleResolution -> arg "simpleresolution"
        | HighEntropyVA on -> togl "highentropyva" on
        | SubsystemVersion version -> argp "subsystemversion" version
        | TargetProfile profile -> argp "targetprofile" <| string profile
        | QuotationsDebug on -> togl "quotations-debug" on
        | Times -> arg "times"

    static member Defaults =
        [   Out "" 
            Target Exe
            Platform AnyCpu
            References []
            Debug false        
        ]

/// Compiles the given source file with the given options. If no options
/// given (i.e. the second argument is an empty list), by default tries
/// to behave the same way as would the command-line 'fsc.exe' tool.
let compileFiles (srcFiles : string list) (opts : string list) : int = 
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
    let errors, exitCode = scs.Compile optsArr
    // Better compile reporting thanks to:
    // https://github.com/jbtule/ComposableExtensions/blob/5b961b30668bb7f4d17238770869b5a884bc591f/tools/CompilerHelper.fsx#L233
    for e in errors do
        let errMsg = e.ToString()
        match e.Severity with
        | FSharpErrorSeverity.Warning -> traceImportant errMsg
        | FSharpErrorSeverity.Error -> traceError errMsg
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
///     |> compile [Out "" 
///                 Target Exe
///                 Platform AnyCpu
///                 References []
///                 Debug false 
///             ]
let compile (fscParams : FscParam list) (inputFiles : string list) : int = 
    let inputFiles = inputFiles |> Seq.toList
    let taskDesc = inputFiles |> separated ", "
    let fscParams = if fscParams = [] then FscParam.Defaults else fscParams
    let argList = fscParams |> List.map string
    traceStartTask "Fsc " taskDesc
    let res = compileFiles inputFiles argList
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
///     |> Compile [Out "" 
///                 Target Exe
///                 Platform AnyCpu
///                 References []
///                 Debug false 
///             ]
let Compile (fscParams : FscParam list) (inputFiles : string list) : unit = 
    let res = compile fscParams inputFiles
    if res <> 0 then raise <| BuildException("Fsc: compile failed with exit code", [ string res ])
