/// Contains a task which allows to run [SpecFlow](http://www.specflow.org/) tests.
[<RequireQualifiedAccess>]
[<System.Obsolete("This API is obsolete after SpecFlow V2.4. Please use the SpecFlowNext module instead.")>]
module Fake.DotNet.Testing.SpecFlow

open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators
open System.IO
open System.Text
open System.Runtime.CompilerServices

/// SpecFlow execution parameter type.
type SpecFlowParams = { 
    SubCommand:         string
    ProjectFile:        string
    ToolName:           string
    ToolPath:           string
    WorkingDir:         string
    BinFolder:          string
    OutputFile:         string
    XmlTestResultFile:  string
    TestOutputFile:     string
    Verbose:            bool
    ForceRegeneration:  bool
    XsltFile:           string
}

let private toolname = "specflow.exe"
let private currentDirectory = Directory.GetCurrentDirectory ()

/// SpecFlow default execution parameters.
let private SpecFlowDefaults = { 
    SubCommand =        "generateall"
    ProjectFile =       null
    ToolName =          toolname
    ToolPath =          Tools.findToolFolderInSubPath toolname (currentDirectory </> "tools" </> "SpecFlow")
    WorkingDir =        null
    BinFolder =         null
    OutputFile =        null
    XmlTestResultFile = null
    TestOutputFile =    null
    Verbose =           false
    ForceRegeneration = false
    XsltFile =          null
}

// Runs SpecFlow on a project.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default SpecFlow parameter value.
let run setParams =    
    let parameters = setParams SpecFlowDefaults

    use __ = Trace.traceTask "SpecFlow " parameters.SubCommand

    let tool = parameters.ToolPath </> parameters.ToolName

    let isMsTest = String.toLower >> ((=) "mstestexecutionreport")

    let yieldIfNotNull paramName value =
        seq {
            match value with
            | null -> ()
            | "" -> ()
            | v -> yield (sprintf "/%s:%s" paramName v)
        }

    let args = 
        [
            yield parameters.SubCommand            
            yield parameters.ProjectFile

            yield! parameters.BinFolder 
                   |> yieldIfNotNull "binFolder" 

            yield! parameters.OutputFile 
                   |> yieldIfNotNull "out"

            yield! parameters.XmlTestResultFile 
                   |> yieldIfNotNull (if isMsTest parameters.SubCommand 
                                      then "testResult" 
                                      else "xmlTestResult")

            yield! parameters.TestOutputFile 
                   |> yieldIfNotNull "testOutput"

            if parameters.Verbose then yield "/verbose"
            if parameters.ForceRegeneration then yield "/force"

            yield! parameters.XsltFile 
                   |> yieldIfNotNull "xsltFile"
        ]
        |> Args.toWindowsCommandLine

    Trace.trace (tool + " " + args)

    let processStartInfo info = 
         { info with FileName = tool
                     WorkingDirectory = parameters.WorkingDir
                     Arguments = args }

    match Process.execSimple processStartInfo System.TimeSpan.MaxValue with
    | 0 -> ()
    | errorNumber -> failwithf "SpecFlow %s failed. Process finished with exit code %i" parameters.SubCommand errorNumber
    __.MarkSuccess()
