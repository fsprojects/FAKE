[<AutoOpen>]
module Fake.SpecFlowHelper

open System
open System.IO
open System.Text

// SpecFlow execution params type
type SpecFlowParams = { 
    SubCommand:         string
    ProjectFile:        string
    ToolName:           string
    ToolPath:           string
    WorkingDir:         string
    BinFolder:          string
    OutputFile:         string
    XmlTestResultFile:  string
    Verbose:            bool
    ForceRegeneration:  bool
    XsltFile:           string
}

// SpecFlow defalt execution params
let SpecFlowDefaults = { 
    SubCommand =        "generateall"
    ProjectFile =       null
    ToolName =          "specflow.exe"
    ToolPath =          currentDirectory @@ "tools" @@ "SpecFlow"
    WorkingDir =        null
    BinFolder =         null
    OutputFile =        null
    XmlTestResultFile = null
    Verbose =           false
    ForceRegeneration = false
    XsltFile =          null
}

// Run SpecFlow on a set of params.
let SpecFlow setParams =    

    // get default params, and push our set of params in.
    let parameters = SpecFlowDefaults |> setParams

    // write trace
    traceStartTask "SpecFlow " parameters.SubCommand

    // build the command line args
    let commandLineBuilder = 
        new StringBuilder()
            |> append           parameters.SubCommand
            |> append           parameters.ProjectFile
            |> appendIfNotNull  parameters.BinFolder "/binFolder:"
            |> appendIfNotNull  parameters.OutputFile "/out:"
            |> appendIfNotNull  parameters.XmlTestResultFile "/xmlTestResult:"
            |> appendIfTrue     parameters.Verbose "/verbose"
            |> appendIfTrue     parameters.ForceRegeneration "/force"
            |> appendIfNotNull  parameters.XsltFile "/xsltFile:"

    // build the command line executable
    let tool = parameters.ToolPath @@ parameters.ToolName

    // args = parameters to string
    let args = commandLineBuilder.ToString()

    // write trace
    trace (tool + " " + args)

    // execute (with max value timeout)!
    let result =
        execProcessAndReturnExitCode (fun info ->
            info.FileName <- tool
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) System.TimeSpan.MaxValue

    // handle result
    match result with
        | 0 -> traceEndTask "SpecFlow " parameters.SubCommand
        | _ -> failwithf "SpecFlow %s failed. Process finished with exit code %i" parameters.SubCommand result
