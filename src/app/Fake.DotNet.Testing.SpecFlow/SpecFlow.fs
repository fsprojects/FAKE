/// Contains a task which allows to run [SpecFlow](http://www.specflow.org/) tests.
[<RequireQualifiedAccess>]
module Fake.DotNet.Testing.SpecFlow

open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators
open System.IO
open System.Text

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

    use _ = Trace.traceStart "SpecFlow " parameters.SubCommand

    let tool = parameters.ToolPath </> parameters.ToolName

    let isMsTest = String.toLower >> ((=) "mstestexecutionreport")

    let commandLineBuilder = 
        new StringBuilder()
        |> StringBuilder.append           parameters.SubCommand
        |> StringBuilder.append           parameters.ProjectFile
        |> StringBuilder.appendIfNotNull  parameters.BinFolder "/binFolder:"
        |> StringBuilder.appendIfNotNull  parameters.OutputFile "/out:"
        |> StringBuilder.appendIfNotNull  parameters.XmlTestResultFile 
                                          (if isMsTest parameters.SubCommand then "/testResult:" else "/xmlTestResult:")
        |> StringBuilder.appendIfNotNull  parameters.TestOutputFile "/testOutput:"
        |> StringBuilder.appendIfTrue     parameters.Verbose "/verbose"
        |> StringBuilder.appendIfTrue     parameters.ForceRegeneration "/force"
        |> StringBuilder.appendIfNotNull  parameters.XsltFile "/xsltFile:"

    let args = commandLineBuilder.ToString()

    Trace.trace (tool + " " + args)

    let processStartInfo info = 
         { info with FileName = tool
                     WorkingDirectory = parameters.WorkingDir
                     Arguments = args }

    match Process.execSimple processStartInfo System.TimeSpan.MaxValue with
    | 0 -> ()
    | errorNumber -> failwithf "SpecFlow %s failed. Process finished with exit code %i" parameters.SubCommand errorNumber
