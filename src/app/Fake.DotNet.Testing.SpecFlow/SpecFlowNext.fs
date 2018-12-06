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

let internal createProcess setParams projectFile =
    if projectFile |> String.isNullOrWhiteSpace
    then
        Trace.traceError "SpecFlow needs a non empty project file!"
        failwithf "SpecFlow needs a non empty project file!"

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
            
            yield "--ProjectFile"
            yield projectFile
            
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

            if parameters.Verbose then yield "--verbose"
            if parameters.ForceRegeneration then yield "--force"

            yield! parameters.XsltFile
                   |> yieldIfSome "XsltFile"
        ]
        |> Arguments.OfArgs

    parameters,
    CreateProcess.fromCommand (RawCommand(tool, args))
    |> CreateProcess.withFramework
    |> CreateProcess.withWorkingDirectory parameters.WorkingDir
    |> CreateProcess.ensureExitCode
    |> fun command ->
        Trace.trace command.CommandLine
        command

// Runs SpecFlow on a project.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default SpecFlow parameter value.
///  - `projectFile` - The required project file.
let run setParams projectFile =
    let parameters, cp = projectFile |> createProcess setParams
    use __ = Trace.traceTask "SpecFlow " (parameters.SubCommand |> string)
    cp
    |> Proc.run
    |> ignore
    __.MarkSuccess()
