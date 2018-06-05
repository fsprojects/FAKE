[<RequireQualifiedAccess>]
module Fake.DotNet.Fsi

open System
open Fake.Core
open Fake.DotNet
open Fake.Tools
open System.IO
open System.Text
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

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
    /// Define a conditional compilation symbols
    Define: string
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
}
with 
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
        let argls s (ls:string list) = stringEmptyMap (sprintf "--%s:\"%s\"" s) (String.concat ";" ls)
        /// format a compiler arg that ends with "+" or "-" with string parameters  "--%s%s:\"%s\""
        let inline toglls s b (ls:'a list) = 
            stringEmptyMap (sprintf "--%s%s:\"%s\"" s  (chk b)) (String.concat ";" (List.map string ls))

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
        ]
        |> List.filter String.isNotNullOrEmpty

    static member Defaults = 
        {
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
        }    

module internal ExternalFsi = 
    (* - FSI External Exe - *)
    let private FSIPath = @".\tools\FSharp\;.\lib\FSharp\;[ProgramFilesX86]\Microsoft SDKs\F#\10.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\4.0\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.0\Framework\v4.0;[ProgramFiles]\Microsoft F#\v4.0\;[ProgramFilesX86]\Microsoft F#\v4.0\;[ProgramFiles]\FSharp-2.0.0.0\bin\;[ProgramFilesX86]\FSharp-2.0.0.0\bin\;[ProgramFiles]\FSharp-1.9.9.9\bin\;[ProgramFilesX86]\FSharp-1.9.9.9\bin\"

    /// The path to the F# Interactive tool.
    let private pathToFsiExe =
        let ev = Environment.environVar "FSI"
        if not (String.isNullOrEmpty ev) then ev else
        if Environment.isUnix then
            let paths = Process.appSettings "FSIPath" FSIPath
            // The standard name on *nix is "fsharpi"
            match Process.tryFindFile paths "fsharpi" with
            | Some file -> file
            | None ->
            // The early F# 2.0 name on *nix was "fsi"
            match Process.tryFindFile paths "fsi" with
            | Some file -> file
            | None -> "fsharpi"
        else
            // let dir = Path.GetDirectoryName fullAssemblyPath
            // let fi = FileInfo.ofPath (Path.Combine(dir, "fsi.exe"))
            // if fi.Exists then fi.FullName else
            Process.findPath "FSIPath" FSIPath "fsi.exe"

    /// Gets the default environment variables and additionally appends user defined vars to it
    let private defaultEnvironmentVars environmentVars = 
        [
            ("MSBuild", MSBuild.msBuildExe)
            ("GIT", Git.CommandHelper.gitPath)
            ("FSI", pathToFsiExe )
        ]
        |> Seq.append environmentVars

    /// Serializes arguments, putting script arguments after an empty "--" arg, which denotes the beginning of script arguments
    let private serializeArgs script (scriptArgs: string list) (fsiParams: FsiParams) = 
        let stringParams = FsiParams.ToArgsList fsiParams//fsiParams.ToArgsList()
        let args = 
            List.concat ([ stringParams; [script;"--"]; scriptArgs]) 
            |> List.toArray
            |> Arguments.OfArgs 
        args.ToWindowsCommandLine

    /// Executes a user supplied Fsi.exe with the option to set args and environment variables
    let execRaw parameters script scriptArgs workingDirectory fsiExe environmentVars = 
        let args = parameters FsiParams.Defaults |> serializeArgs script scriptArgs
        let environmentVars' = defaultEnvironmentVars environmentVars

        Trace.trace <| sprintf "Executing FSI at %s with args %s" fsiExe args

        let r = Process.execWithResult (fun info -> 
                { info with
                    FileName = fsiExe
                    Arguments = args
                    WorkingDirectory = workingDirectory
                }.WithEnvironmentVariables environmentVars' ) TimeSpan.MaxValue        

        if r.ExitCode <> 0 then
            List.iter Trace.traceError r.Errors

        (r.ExitCode, r.Messages)

    /// Locates Fsi.exe and executes 
    let exec fsiParams script scriptArgs = 
        execRaw fsiParams script scriptArgs "" pathToFsiExe []

module internal InternalFsi = 

    let private doExec script allArgs  = 
        // Intialize output and input streams
        let sbOut = new StringBuilder()
        let sbErr = new StringBuilder()
        let inStream = new StringReader("")
        let outStream = new StringWriter(sbOut)
        let errStream = new StringWriter(sbErr)

        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
        let fsiSession = FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream)
        fsiSession.EvalScriptNonThrowing script


    let private serializeArgs fsiParams scriptArgs = 
        let scriptArgs = if List.isEmpty scriptArgs then scriptArgs else List.append ["--"] scriptArgs
        let fsiArgs = FsiParams.Defaults |> fsiParams |> FsiParams.ToArgsList
        List.concat ([["C:\\fsi.exe"]; fsiArgs; scriptArgs]) |> List.toArray


    let private traceErrors (errors: FSharpErrorInfo []) =    
        errors |> Array.iter (fun e -> 
        match e.Severity with
        | FSharpErrorSeverity.Error -> Trace.traceError e.Message
        | FSharpErrorSeverity.Warning -> Trace.traceImportant e.Message)


    let exec fsiParams script scriptArgs = 
        let allArgs = serializeArgs fsiParams scriptArgs
        
        use __ = Trace.traceTask "Fsi " (sprintf "%s with args %A" script allArgs)
        let result, errors = doExec script allArgs      
        traceErrors errors
        __.MarkSuccess()

        //Return error code 0 for success, or 1 with exception message on failure 
        match result with
        | Choice1Of2 _ -> (0,"The script completed successfully")
        | Choice2Of2 e -> (1, e.ToString())


(* - Public Facing API - *)
/// Executes the internal fsi within FSC on the given script
/// Returns error code and an exception message if any exceptions were thrown
/// 
/// e.g: Passing some arguemnts to fsi, along with the script and some args to be passed to the script
///     ```
///     let script = "MyScript.fsx"
///     let (exitcode,msgs) = Fsi.execInternal (fun p -> 
///                 { p with 
///                     TargetProfile = Fsi.Profile.NetStandard } ) script ["stuff";"10"]
///     ```
// let execInternal fsiParams script scriptArgs = 
    // InternalFsi.execFsi fsiParams script scriptArgs

/// Executes a user supplied Fsi.exe with the option to set args and environment variables and 
/// runs the specified script.
/// Returns error code and any messages from the process
/// 
/// e.g: Passing some arguemnts to fsi, along with the script and some args to be passed to the script
///     ```
///     let workingDir = "path/to/WorkingDir"
///     let environmentVars = [("SOME_VAR","55");("GIT","path/to/git")]
///     let fsiExe = "path/to/fsi.exe"
///     let script = "MyScript.fsx"
///     let (exitcode,msgs) = Fsi.execExternalRaw (fun p -> 
///                 { p with 
///                     TargetProfile = Fsi.Profile.NetStandard } ) 
///                                 script ["stuff";"10"] workingDir fsiExe environmentVars
///     ```
let execExternalRaw fsiParams script scriptArgs workingDirectory fsiExe environmentVars = 
    ExternalFsi.execRaw fsiParams script scriptArgs workingDirectory fsiExe environmentVars

/// Looks for Fsi.exe in standard install locations and executes fsi on the specified script.
/// Returns error code and any messages from the process
/// 
/// e.g: Passing some arguemnts to fsi, along with the script and some args to be passed to the script
///     ```
///     let script = "MyScript.fsx"
///     let (exitcode,msgs) = Fsi.execExternal (fun p -> 
///                 { p with 
///                     TargetProfile = Fsi.Profile.NetStandard } ) script ["stuff";"10"]
///     ```
let execExternal fsiParams script scriptArgs = 
    ExternalFsi.exec fsiParams script scriptArgs
