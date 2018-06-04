
/// This module contains helper functions to create [Inno Setup](http://www.jrsoftware.org/isinfo.php) installers.
[<RequireQualifiedAccess>]
module Fake.Installer.InnoSetup

open System
open System.IO
open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators
open System.Text

let private toolPath = 
    let innoExe = "ISCC.exe"
    let toolPath = Tools.findToolInSubPath innoExe (Directory.GetCurrentDirectory() @@ "tools" @@ "Tools.InnoSetup" @@ "tools")
    if File.exists toolPath then toolPath
    else 
        match Process.tryFindFileOnPath innoExe with
        | Some inno when File.exists inno -> inno
        | _ -> toolPath

let private timeout = TimeSpan.FromMinutes 5.

/// Output verbosity
type QuietMode =
    | Default /// Default output when compiling
    | Quiet /// Quiet compile (print error messages only)
    | QuietAndProgress /// Enable quiet compile while still displaying progress

/// InnoSetup build parameters
type InnoSetupParams =
    {
        /// The tool path - FAKE tries to find ISCC.exe automatically in any sub folder.
        ToolPath : string

        /// Specify the process working directory
        WorkingDirectory : string

        /// Specify a timeout for ISCC. Default: 5 min.
        Timeout : TimeSpan

        /// Enable or disable output (overrides Output)
        EnableOutput : bool option

        /// Output files to specified path (overrides OutputDir)
        OutputFolder : string

        /// Overrides OutputBaseFilename with the specified filename
        OutputBaseFilename : string

        /// Specifies output mode when compiling
        QuietMode : QuietMode

        /// Emulate #define public <name> <value>
        Defines : Map<string,string>

        /// Additional parameters
        AdditionalParameters : string option

        /// Path to inno-script file
        ScriptFile : string
    }

    /// InnoSetup default parameters
    static member Create()=
        {
            ToolPath = toolPath
            WorkingDirectory = ""
            Timeout = timeout
            ScriptFile = "innosetup.iss"
            EnableOutput = None
            OutputFolder = ""
            OutputBaseFilename = ""
            QuietMode = Default
            Defines = Map.empty
            AdditionalParameters = None
        }

let private run toolPath workingDirectory timeout command = 
    use __ = Trace.traceTask "InnoSetup" command
    if 0 <> Process.execSimple ((fun info ->
            { info with
                FileName = toolPath
                WorkingDirectory = workingDirectory
                Arguments = command })) timeout
    then failwithf "InnoSetup command %s failed." command
    __.MarkSuccess()
    
let private serializeInnoSetupParams p = 
    let appendDefine (key,value) _ sb =
        if String.isNullOrEmpty value then
            StringBuilder.append (sprintf "/D%s" key) sb
        else 
            StringBuilder.append (sprintf "/D%s=%s" key value) sb      

    StringBuilder()
    |> StringBuilder.appendIfSome p.AdditionalParameters id
    |> StringBuilder.appendIfSome p.EnableOutput (fun enableOutput -> if enableOutput then "/O+" else "/O-")
    |> StringBuilder.appendIfNotNullOrEmpty p.OutputFolder "/O"
    |> StringBuilder.appendIfNotNullOrEmpty p.OutputBaseFilename "/F"
    |> StringBuilder.appendWithoutQuotes 
        (match p.QuietMode with
         | Quiet -> "/Q" 
         | QuietAndProgress -> "/Qp" 
         |_ -> "")
    |> StringBuilder.forEach (p.Defines |> Map.toList) appendDefine "" 
    |> StringBuilder.append p.ScriptFile
    |> StringBuilder.toText

/// Builds the InnoSetup installer.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default build parameters. See `InnoSetupParams.Create()`
/// ## Sample
///
///        InnoSetup.build (fun p -> 
///         { p with 
///             OutputFolder = "build" @@ "installer"
///             ScriptFile = "installer" @@ "setup.iss"    
///         })
let build setParams =
    let p = InnoSetupParams.Create() |> setParams
    p
    |> serializeInnoSetupParams
    |> run p.ToolPath p.WorkingDirectory p.Timeout 
