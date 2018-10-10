/// Contains a task which allows to run [SpecFlow](http://www.specflow.org/) tests with SpecFlow v2.4+.
[<RequireQualifiedAccess>]
module Fake.DotNet.Testing.SpecFlowNext

open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators
open System.IO
open System.Text

type SubCommand =
    | GenerateAll
    | StepDefinitionReport
    | NUnitExecutionReport
    | MsTestExecutionReport

    override x.ToString () =
        match x with
        | GenerateAll -> "GenerateAll"
        | StepDefinitionReport -> "StepDefinitionReport"
        | NUnitExecutionReport -> "NUnitExecutionReport"
        | MsTestExecutionReport -> "MsTestExecutionReport"

/// SpecFlow execution parameter type.
type SpecFlowParams = { 
    SubCommand:         SubCommand
    ProjectFile:        string
    ToolName:           string
    ToolPath:           string
    WorkingDir:         string
    BinFolder:          string option
    OutputFile:         string option
    XmlTestResultFile:  string option
    TestOutputFile:     string option
    FeatureLanguage:    string option
    Verbose:            bool
    ForceRegeneration:  bool
    XsltFile:           string option
}

let private toolname = "specflow.exe"
let private currentDirectory = Directory.GetCurrentDirectory ()

/// SpecFlow default execution parameters.
let private SpecFlowDefaults = { 
    SubCommand =        GenerateAll
    ProjectFile =       null
    ToolName =          toolname
    ToolPath =          Tools.findToolFolderInSubPath toolname (currentDirectory </> "tools" </> "SpecFlow")
    WorkingDir =        null
    BinFolder =         None
    OutputFile =        None
    XmlTestResultFile = None
    TestOutputFile =    None
    FeatureLanguage =   None
    Verbose =           false
    ForceRegeneration = false
    XsltFile =          None
}

let internal createProcess setParams =
    let parameters = setParams SpecFlowDefaults
    let tool = parameters.ToolPath </> parameters.ToolName

    let yieldIfSome paramName value =
        seq { match value with
              | Some v ->
                yield sprintf "--%s" paramName
                yield v
              | _ -> () }

    let args = 
        [
            yield parameters.SubCommand |> string
            
            if not (isNull parameters.ProjectFile) then
                yield "--ProjectFile"
                yield parameters.ProjectFile
            
            yield! parameters.BinFolder 
                   |> yieldIfSome "binFolder"

            yield! parameters.OutputFile 
                   |> yieldIfSome "OutputFile"

            yield! parameters.XmlTestResultFile 
                   |> yieldIfSome (match parameters.SubCommand with
                                   | MsTestExecutionReport -> "TestResult"
                                   | _ -> "xmlTestResult")

            yield! parameters.TestOutputFile
                   |> yieldIfSome "testOutput"

            yield! parameters.FeatureLanguage 
                   |> yieldIfSome "FeatureLanguage"

            if parameters.Verbose then yield "verbose"
            if parameters.ForceRegeneration then yield "force"

            yield! parameters.XsltFile
                   |> yieldIfSome "XsltFile"
        ]
        |> Arguments.OfArgs
        //|> Args.toWindowsCommandLine

    Trace.trace (tool + " " + args.ToStartInfo)

    parameters,
    CreateProcess.fromCommand (RawCommand(tool, args))
    |> CreateProcess.withWorkingDirectory parameters.WorkingDir
    |> CreateProcess.ensureExitCode

// Runs SpecFlow on a project.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default SpecFlow parameter value.
let run setParams =
    let parameters, cp = createProcess setParams
    use __ = Trace.traceTask "SpecFlow " (parameters.SubCommand |> string)
    cp
    |> Proc.run
    |> ignore
    __.MarkSuccess()
