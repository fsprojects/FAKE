namespace Fake.DotNet.Testing

open Fake.Core
open Fake.IO
open System.IO

/// Contains a task which allows to run [SpecFlow](http://www.specflow.org/) tests with SpecFlow v2.4+.
[<RequireQualifiedAccess>]
module SpecFlow =

    /// The subcommands to execute against SpecFlow
    type SubCommand =
        | GenerateAll
        | StepDefinitionReport
        | NUnitExecutionReport
        | MsTestExecutionReport

        override x.ToString() =
            match x with
            | GenerateAll -> "GenerateAll"
            | StepDefinitionReport -> "StepDefinitionReport"
            | NUnitExecutionReport -> "NUnitExecutionReport"
            | MsTestExecutionReport -> "MsTestExecutionReport"

    /// SpecFlow execution parameter type.
    type SpecFlowParams =
        {
            /// The subcommand to execute, see `SpecFlow.SubCommand` type
            SubCommand: SubCommand

            /// SpecFlow executable path
            ToolPath: string

            /// The working directory to execute SpecFlow in
            WorkingDir: string

            /// The bin folder
            BinFolder: string option

            /// Output file name
            OutputFile: string option

            /// XML test result file name
            XmlTestResultFile: string option

            /// Test output file name
            TestOutputFile: string option

            /// The feature language to use
            FeatureLanguage: string option

            /// Set if SpecFlow command is executed in verbose mode
            Verbose: bool

            /// For regeneration of test results
            ForceRegeneration: bool

            /// the Xslt file to use
            XsltFile: string option
        }

    let private toolName = "specflow.exe"

    let internal toolPath toolName =
        let toolPath =
            ProcessUtils.tryFindLocalTool "TOOL" toolName [ Directory.GetCurrentDirectory() ]

        match toolPath with
        | Some path -> path
        | None -> toolName

    /// SpecFlow default execution parameters.
    let private SpecFlowDefaults =
        { SubCommand = GenerateAll
          ToolPath = toolPath toolName
          WorkingDir = null
          BinFolder = None
          OutputFile = None
          XmlTestResultFile = None
          TestOutputFile = None
          FeatureLanguage = None
          Verbose = false
          ForceRegeneration = false
          XsltFile = None }

    let internal createProcess setParams projectFile =
        if projectFile |> String.isNullOrWhiteSpace then
            Trace.traceError "SpecFlow needs a non empty project file!"
            failwithf "SpecFlow needs a non empty project file!"

        let parameters = setParams SpecFlowDefaults

        let yieldIfSome paramName value =
            seq {
                match value with
                | Some v ->
                    yield sprintf "--%s" paramName
                    yield v
                | _ -> ()
            }

        let args =
            [ yield parameters.SubCommand |> string

              yield "--ProjectFile"
              yield projectFile

              yield! parameters.BinFolder |> yieldIfSome "binFolder"

              yield! parameters.OutputFile |> yieldIfSome "OutputFile"

              yield!
                  parameters.XmlTestResultFile
                  |> yieldIfSome (
                      match parameters.SubCommand with
                      | MsTestExecutionReport -> "TestResult"
                      | _ -> "xmlTestResult"
                  )

              yield! parameters.TestOutputFile |> yieldIfSome "testOutput"

              yield! parameters.FeatureLanguage |> yieldIfSome "FeatureLanguage"

              if parameters.Verbose then
                  yield "--verbose"
              if parameters.ForceRegeneration then
                  yield "--force"

              yield! parameters.XsltFile |> yieldIfSome "XsltFile" ]
            |> Arguments.OfArgs

        parameters,
        CreateProcess.fromCommand (RawCommand(parameters.ToolPath, args))
        |> CreateProcess.withFramework
        |> CreateProcess.withWorkingDirectory parameters.WorkingDir
        |> CreateProcess.ensureExitCode
        |> fun command ->
            Trace.trace command.CommandLine
            command

    // Runs SpecFlow on a project.
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default SpecFlow parameter value.
    ///  - `projectFile` - The required project file.
    let run setParams projectFile =
        let parameters, cp = projectFile |> createProcess setParams
        use __ = Trace.traceTask "SpecFlow " (parameters.SubCommand |> string)
        cp |> Proc.run |> ignore
        __.MarkSuccess()
