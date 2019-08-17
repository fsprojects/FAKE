[<RequireQualifiedAccess>]
module Fake.DotNet.Fsi

open System
open Fake.Core
open Fake.DotNet
open Fake.Tools
open System.IO
open System.Text
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Interactive.Shell

(* - Fsi Types - *)
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

[<RequireQualifiedAccess>]
type FsiTool =
    | External of string
    | Internal
    | Default

type FsiParams = {

(* - INPUT FILES - *)
    /// Use the given file on startup as initial input
    Use: string
    /// Load the given file on startup
    Load: string
    /// Reference an assembly (Short form: -r)
    Reference: string

(* - CODE GENERATION - *)
    /// Emit debug information (Short form: -g)
    Debug: bool option
    /// Specify debugging type: full, portable, embedded, pdbonly. PdbOnly is the default
    DebugType: DebugTypes
    /// Enable optimizations (Short form: -O)
    Optimize: bool option
    /// Enable or disable tailcalls
    TailCalls: bool option
    /// Produce a deterministic assembly (including module version GUID and timestamp)
    Deterministic: bool option
    /// Enable or disable cross-module optimizations
    CrossOptimize: bool option

(* - ERRORS AND WARNINGS - *)
    /// Report all warnings as errors
    WarnAsError: bool option
    /// Report specific warnings as errors
    WarnAsErrors: bool * int list
    /// Set a warning level (0-5)
    Warn: int option
    /// Disable specific warning messages
    NoWarn: int list
    /// Enable specific warnings that may be off by default
    WarnOn: int list
    /// Output warning and error messages in color
    ConsoleColors: bool option

(* - LANGUAGE - *)
    /// Generate overflow checks
    Checked: bool option
    /// (Obsolete) Define a conditional compilation symbol (use FsiParams.Definitions instead)
    Define: string
    /// Define a list of conditional compilation symbols
    Definitions: string list
    /// Ignore ML compatibility warnings
    MLCompatibility: bool 

(* - MISCELLANEOUS - *)
    /// Suppress compiler copyright message
    NoLogo: bool
    ///  Display the commandline flags and their usage
    Help: bool

(* - ADVANCED - *)
    /// Specify the codepage used to read source files
    Codepage: int option
    /// Output messages in UTF-8 encoding
    Utf8Output: bool
    /// Specify the preferred output language culture name (e.g. es-ES, ja-JP)
    PreferredUiLang: string
    /// Output messages with fully qualified paths
    FullPaths: bool
    /// Specify a directory for the include path which is used to resolve source files and assemblies (Short form: -I)
    Lib: string list
    /// Resolve assembly references using directory-based rules rather than MSBuild resolution
    SimpleResolution: bool
    /// Specify target framework profile of this assembly. Valid values are mscorlib, netcore or netstandard. Default - mscorlib
    TargetProfile: Profile
    /// Do not reference the default CLI assemblies by default
    NoFramework: bool
    /// Exit fsi after loading the files or running the .fsx script given on the command line
    Exec: bool
    /// Execute interactions on a Windows Forms event loop (on by default)
    GUI: bool option
    /// Suppress fsi writing to stdout
    Quiet: bool
    /// Support TAB completion in console (on by default)
    ReadLine: bool option
    /// Emit debug information in quotations
    QuotationsDebug: bool option
    /// Prevents references from being locked by the F# Interactive process
    ShadowCopyReferences: bool option

    /// Sets the path to the fsharpi / fsi.exe to use
    ToolPath : FsiTool
    /// Environment variables
    Environment : Map<string, string>
    /// When UseShellExecute is true, the fully qualified name of the directory that contains the process to be started. When the UseShellExecute property is false, the working directory for the process to be started. The default is an empty string ("").
    WorkingDirectory : string
}
with 
    /// Sets the current environment variables.
    member x.WithEnvironment map =
        { x with Environment = map }
    static member ToArgsList p = 

        let stringEmptyMap f s = 
            if String.isNullOrWhiteSpace s then "" else f s

        /// format a standalone compiler arg: "--%s"
        let arg s b = if b then sprintf "--%s" s else ""
        /// format a compiler arg with a parameter: "--%s:%s"
        let argp s p = stringEmptyMap (sprintf "--%s:%s" s) p            
        /// format a short form compiler arg with a parameter: "-%s:%s"
        let sargp s p = stringEmptyMap (sprintf "-%s:%s" s) p  // for short forms          
        /// helper function to convert a bool to a "+" or "-"
        let inline chk b = if b then "+" else "-"
        /// format a compiler arg that ends with "+" or "-": "--%s%s"
        let togl s b = if Option.isNone b then "" else sprintf "--%s%s" s (chk b.Value)
        /// format a list of compiler args with string parameters "--%s:\"%s\""   
        let argls s (ls:string list) = stringEmptyMap (sprintf "--%s:%s" s) (String.concat ";" ls)
        /// format a compiler arg that ends with "+" or "-" with string parameters  "--%s%s:\"%s\""
        let inline toglls s b (ls:'a list) = 
            stringEmptyMap (sprintf "--%s%s:%s" s  (chk b)) (String.concat ";" (List.map string ls))
        /// format a list of short form complier args using the same symbol 
        let sargmap sym ls = ls |> List.map (sargp sym)

        [
            argp "use" p.Use
            argp "load" p.Load                      
            argp "reference" p.Reference

            (* - CODE GENERATION - *)
            togl "debug" p.Debug
            argp "debug" <| string p.DebugType
            togl "optimize" p.Optimize
            togl "tailcalls" p.TailCalls
            togl "deterministic" p.Deterministic                     
            togl "crossoptimize" p.CrossOptimize         

            (* - ERRORS AND WARNINGS - *)
            togl "warnaserror" p.WarnAsError
            toglls "warnaserror" (fst p.WarnAsErrors) (snd p.WarnAsErrors)
            argp "warn" <| Option.defaultValue "" (Option.map (fun warn -> if warn < 0 then "0" elif warn > 5 then "5" else warn.ToString()) p.Warn)
            argls "nowarn" (List.map string p.NoWarn)
            argls "warnon" (List.map string p.WarnOn)
            togl "consolecolors" p.ConsoleColors

            // (* - LANGUAGE - *)
            togl "checked" p.Checked
            sargp "d" p.Define
            arg "mlcompatibility" p.MLCompatibility

            // (* - MISCELLANEOUS - *)
            arg "nologo" p.NoLogo
            arg "help" p.Help

            // (* - ADVANCED - *)
            argp "codepage" <| string p.Codepage
            arg "utf8output" p.Utf8Output
            argp "preferreduilang" p.PreferredUiLang               
            arg "fullpaths" p.FullPaths
            argls "lib" p.Lib
            arg "simpleresolution" p.SimpleResolution                       
            argp "targetprofile" <| string p.TargetProfile
            arg "noframework" p.NoFramework
            arg "exec" p.Exec
            togl "gui" p.GUI 
            arg "quiet" p.Quiet
            togl "readline" p.ReadLine
            togl "quotations-debug" p.QuotationsDebug       
            togl "shadowcopyreferences" p.ShadowCopyReferences
        ] @ (sargmap "d" p.Definitions)

        |> List.filter String.isNotNullOrEmpty

    static member Create() =
        {
            Environment =
                Process.createEnvironmentMap()
            WorkingDirectory =  Directory.GetCurrentDirectory()
            Use = null
            Load = null
            Reference = null
            Debug = None
            DebugType = DebugTypes.PdbOnly
            Optimize = None
            TailCalls = None
            Deterministic = None
            CrossOptimize = None
            WarnAsError = None
            WarnAsErrors = false, []
            Warn = None
            NoWarn = []
            WarnOn = []
            ConsoleColors = None
            Checked = None
            Define = null
            Definitions = []
            MLCompatibility = false 
            NoLogo = false
            Help = false
            Codepage = None 
            Utf8Output = false
            PreferredUiLang = null
            FullPaths = false
            Lib = []
            SimpleResolution = false
            TargetProfile = Profile.MsCorlib
            NoFramework = false
            Exec = false
            GUI = None
            Quiet = false
            ReadLine = None
            QuotationsDebug = None
            ShadowCopyReferences = None
            ToolPath = FsiTool.Default
        }    

module internal ExternalFsi = 
    (* - FSI External Exe - *)
    let private FSIPath = @".\tools\FSharp\;.\lib\FSharp\;[ProgramFilesX86]\Microsoft SDKs\F#\10.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.0\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.0\Framework\v4.0;[ProgramFiles]\Microsoft F#\v4.0\;[ProgramFilesX86]\Microsoft F#\v4.0\;[ProgramFiles]\FSharp-2.0.0.0\bin\;[ProgramFilesX86]\FSharp-2.0.0.0\bin\;[ProgramFiles]\FSharp-1.9.9.9\bin\;[ProgramFilesX86]\FSharp-1.9.9.9\bin\"

    /// The path to the F# Interactive tool.
    let internal pathToFsiExe =
        let ev = Environment.environVar "FSI"
        if not (String.isNullOrEmpty ev) then ev else
        if Environment.isUnix then
            // The standard name on *nix is "fsharpi"
            match Process.tryFindFileOnPath "fsharpi" with
            | Some file -> file
            | None ->
            // The early F# 2.0 name on *nix was "fsi"
            match Process.tryFindFileOnPath "fsi" with
            | Some file -> file
            | None -> "fsharpi"
        else
            // let dir = Path.GetDirectoryName fullAssemblyPath
            // let fi = FileInfo.ofPath (Path.Combine(dir, "fsi.exe"))
            // if fi.Exists then fi.FullName else
            Process.findPath "FSIPath" FSIPath "fsi.exe"

    /// Gets the default environment variables and additionally appends user defined vars to it
    let private defaultEnvironmentVars = 
        [
            ("MSBuild", MSBuild.msBuildExe)
            ("GIT", Git.CommandHelper.gitPath)
            ("FSI", pathToFsiExe )
        ]

    /// Executes a user supplied Fsi.exe with the option to set args and environment variables
    let execRaw fsiExe (parameters:FsiParams) (allArgs:string list) = 
        let args = allArgs |> Args.toWindowsCommandLine

        use __ = Trace.traceTask "Fsi " (sprintf "%s with args %s" fsiExe args)

        let r = Process.execWithResult (fun info -> 
                { info.WithEnvironmentVariables defaultEnvironmentVars with
                    FileName = fsiExe
                    Arguments = args
                    WorkingDirectory = parameters.WorkingDirectory
                }.WithEnvironmentVariables (parameters.Environment |> Map.toSeq)) TimeSpan.MaxValue        

        if r.ExitCode <> 0 then
            List.iter Trace.traceError r.Errors
        __.MarkSuccess()

        (r.ExitCode, r.Messages)

    /// Locates Fsi.exe and executes 
    let exec fsiExe parameters (allArgs:string list) =
        execRaw fsiExe parameters allArgs



module internal InternalFsi = 
    let private doExec script allArgs  = 
        // Intialize output and input streams
        let sbOut = new StringBuilder()
        let sbErr = new StringBuilder()
        let inStream = new StringReader("")
        let outStream = new StringWriter(sbOut)
        let errStream = new StringWriter(sbErr)

        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
        let fsiSession = FsiEvaluationSession.Create(fsiConfig, List.toArray allArgs, inStream, outStream, errStream)
        fsiSession.EvalScriptNonThrowing script

    let private traceErrors (errors: FSharpErrorInfo []) =    
        errors |> Array.iter (fun e -> 
        match e.Severity with
        | FSharpErrorSeverity.Error -> Trace.traceError e.Message
        | FSharpErrorSeverity.Warning -> Trace.traceImportant e.Message)


    let exec script allArgs =
        use __ = Trace.traceTask "Fsi " (sprintf "internal fsi with args %A" allArgs)
        let result, errors = doExec script allArgs      
        traceErrors errors
        __.MarkSuccess()

        //Return error code 0 for success, or 1 with exception message on failure
        //Note -- Returning a string list just for consistency with the external version
        match result with
        | Choice1Of2 _ -> (0,["The script completed successfully"])
        | Choice2Of2 e -> (1, [e.ToString()])

let internal execRaw fsiParams script scriptArgs =
    let param = FsiParams.Create() |> fsiParams 
    
    let stringParams = FsiParams.ToArgsList param//fsiParams.ToArgsList()
    
    match param.ToolPath with
    | FsiTool.External fsiPath ->
        let args = List.concat ([ stringParams; [script;"--"]; scriptArgs]) 
        ExternalFsi.exec fsiPath param args
    | FsiTool.Internal ->
        let args = List.concat ([ ["C:\\fsi.exe"]; stringParams; ["--"]; scriptArgs]) 
        InternalFsi.exec script args
    | FsiTool.Default ->
        let args = List.concat ([ stringParams; [script;"--"]; scriptArgs]) 
        ExternalFsi.exec ExternalFsi.pathToFsiExe param args
        

(* - Public Facing API - *)
/// Executes the internal fsi within FSC on the given script
/// Returns error code and an exception message if any exceptions were thrown
/// 
/// ## Sample
///
/// e.g: Passing some arguments to fsi, along with the script and some args to be passed to the script
///
///     let fsiExe = "path/to/fsi.exe"
///     let script = "MyScript.fsx"
///     let (exitcode,msgs) =
///         Fsi.exec (fun p -> 
///             { p with 
///                 TargetProfile = Fsi.Profile.NetStandard
///                 WorkingDirectory = "path/to/WorkingDir"
///                 ToolPath = FsiTool.External fsiExe
///             }
///             |> Process.setEnvironmentVariable "SOME_VAR" "55"
///             |> Process.setEnvironmentVariable "GIT" "path/to/git") script ["stuff";"10"]
///     ```
let exec fsiParams script scriptArgs = 
    execRaw fsiParams script scriptArgs
