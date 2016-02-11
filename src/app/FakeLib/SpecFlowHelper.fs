[<AutoOpen>]
/// Contains a task which allows to run [SpecFlow](http://www.specflow.org/) tests.
module Fake.SpecFlowHelper

open System
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

let toolname = "specflow.exe"

/// SpecFlow default execution parameters.
let SpecFlowDefaults = { 
    SubCommand =        "generateall"
    ProjectFile =       null
    ToolName =          toolname
    ToolPath =          findToolFolderInSubPath toolname (currentDirectory @@ "tools" @@ "SpecFlow")
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
let SpecFlow setParams =    
    let parameters = setParams SpecFlowDefaults

    traceStartTask "SpecFlow " parameters.SubCommand

    let tool = parameters.ToolPath @@ parameters.ToolName

    let isMsTest = toLower >> ((=) "mstestexecutionreport")

    let commandLineBuilder = 
        new StringBuilder()
        |> append           parameters.SubCommand
        |> append           parameters.ProjectFile
        |> appendIfNotNull  parameters.BinFolder "/binFolder:"
        |> appendIfNotNull  parameters.OutputFile "/out:"
        |> appendIfNotNull  parameters.XmlTestResultFile 
                                (if isMsTest parameters.SubCommand then "/testResult:" else "/xmlTestResult:")
        |> appendIfNotNull  parameters.TestOutputFile "/testOutput:"
        |> appendIfTrue     parameters.Verbose "/verbose"
        |> appendIfTrue     parameters.ForceRegeneration "/force"
        |> appendIfNotNull  parameters.XsltFile "/xsltFile:"

    let args = commandLineBuilder.ToString()

    trace (tool + " " + args)

    let result =
        ExecProcess (fun info ->
            info.FileName <- tool
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) System.TimeSpan.MaxValue

    match result with
    | 0 -> traceEndTask "SpecFlow " parameters.SubCommand
    | _ -> failwithf "SpecFlow %s failed. Process finished with exit code %i" parameters.SubCommand result
