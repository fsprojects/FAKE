module Fake.DotNet.Fsi.Params

///  Specify debugging type: full, portable, embedded, pdbonly. ('pdbonly' is the default if no debuggging type specified and enables attaching a debugger to a running program, 'portable' is a cross-platform format, 'embedded' is a cross-platform format embedded into the output file).
type DebugTypes = 
    | Full | Portable | Embedded | PdbOnly
    override self.ToString () =
        match self with
        | Full    -> "full" 
        | Portable    -> "portable"
        | Embedded  -> "embedded"
        | PdbOnly  -> "pdbonly"

/// Specify target framework profile of this assembly. 
/// Valid values are mscorlib, netcore or netstandard. Default - mscorlib
type Profile = 
    | MsCorlib | Netcore | NetStandard
    override self.ToString () =
        (function MsCorlib -> "mscorlib" | Netcore -> "netcore" | NetStandard -> "netstandard") self  

type FsiParam = 

(* - INPUT FILES - *)
    /// Use the given file on startup as initial input
    | Use of file:string
    /// Load the given file on startup
    | Load of file:string
    /// Reference an assembly (Short form: -r)
    | Reference of file:string

(* - CODE GENERATION - *)
    /// Emit debug information (Short form: -g)
    | Debug of bool
    /// Specify debugging type: full, portable, embedded, pdbonly.
    | DebugType of DebugTypes
    /// Enable optimizations (Short form: -O)
    | Optimize of bool
    /// Enable or disable tailcalls
    | TailCalls of bool
    /// Produce a deterministic assembly (including module version GUID and timestamp)
    | Deterministic of bool
    /// Enable or disable cross-module optimizations
    | CrossOptimize of bool

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
    /// Output messages in UTF-8 encoding
    | Utf8Output
    /// Specify the preferred output language culture name (e.g. es-ES, ja-JP)
    | PreferredUiLang of string
    /// Output messages with fully qualified paths
    | FullPaths
    /// Specify a directory for the include path which is used to resolve source files and assemblies (Short form: -I)
    | Lib of directories:string list
    /// Resolve assembly references using directory-based rules rather than MSBuild resolution
    | SimpleResolution
    /// Specify target framework profile of this assembly. Valid values are mscorlib, netcore or netstandard. Default - mscorlib
    | TargetProfile of profile:Profile
    /// Do not reference the default CLI assemblies by default
    | NoFramework
    /// Exit fsi after loading the files or running the .fsx script given on the command line
    | Exec
    /// Execute interactions on a Windows Forms event loop (on by default)
    | GUI of bool
    /// Suppress fsi writing to stdout
    | Quiet
    /// Support TAB completion in console (on by default)
    | ReadLine of bool
    /// Emit debug information in quotations
    | QuotationsDebug of bool
    /// Prevents references from being locked by the F# Interactive process
    | ShadowCopyReferences of bool

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
        let stogl s b = if b then sprintf "-%s" s else "" // for short forms       
        /// format a list of compiler args with string parameters "--%s:\"%s\""   
        let argls s (ls:string list) = sprintf "--%s:\"%s\"" s (String.concat ";" ls)
        /// format a compiler arg that ends with "+" or "-" with string parameters  "--%s%s:\"%s\""
        let inline toglls s b (ls:'a list) = 
            sprintf "--%s%s:\"%s\"" s  (chk b) (String.concat ";" (List.map string ls))
        /// format a list of short form complier args using the same symbol 
        let sargmap sym ls = ls |> List.map (sargp sym) |> String.concat ";" 

        match self with
        | Use file -> argp "use" file                             
        | Load file -> argp "load" file                      
        | Reference file -> argp "reference" file                       
        (* - CODE GENERATION - *)
        | Debug on -> togl "debug" on                             
        | DebugType t -> argp "debug" <| string t
        | Optimize on -> togl "optimize" on
        | TailCalls on -> togl "tailcalls" on
        | Deterministic on -> togl "deterministic" on                     
        | CrossOptimize on -> togl "crossoptimize" on                     
        (* - ERRORS AND WARNINGS - *)
        | WarnAsError on -> togl "warnaserror" on
        | WarnAsErrors (on, warningCodes) ->  toglls "warnaserror" on warningCodes
        | Warn lvl -> argp "warn" <| string (if lvl < 0 then 0 elif lvl > 5 then 5 else lvl)
        | NoWarn warningCodes -> argls "nowarn" (List.map string warningCodes)
        | WarnOn warningCodes -> argls "warnon" (List.map string warningCodes)
        | ConsoleColors on -> togl "consolecolors" on
        (* - LANGUAGE - *)
        | Checked on -> togl "checked" on
        | Define symbol -> sargp "d" symbol
        | MLCompatibility -> arg "mlcompatibility"
        (* - MISCELLANEOUS - *)
        | NoLogo -> arg "nologo"
        | Help -> arg "help"
        (* - ADVANCED - *)
        | Codepage n -> argp "codepage" <| string n
        | Utf8Output -> arg "utf8output"
        | PreferredUiLang s -> argp "preferreduilang" s               
        | FullPaths -> arg "fullpaths"
        | Lib directories -> argls "lib" directories
        | SimpleResolution -> arg "simpleresolution"                       
        | TargetProfile profile -> argp "targetprofile" <| string profile
        | NoFramework -> arg "noframework"
        | Exec -> arg "exec"
        | GUI on -> togl "gui" on 
        | Quiet -> arg "quiet"
        | ReadLine on -> togl "readline" on
        | QuotationsDebug on -> togl "quotations-debug" on                  
        | ShadowCopyReferences on -> togl "shadowcopyreferences" on
