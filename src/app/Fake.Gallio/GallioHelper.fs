module Fake.Gallio

// this module is largely based on Gallio's NAnt task implementation

open System
open System.IO
open Gallio.Runner
open Gallio.Runner.Projects
open Gallio.Runner.Reports
open Gallio.Runtime
open Gallio.Runtime.Debugging
open Gallio.Runtime.Logging
open Gallio.Runtime.ProgressMonitoring
open Gallio.Common.Reflection
open Gallio.Model
open Gallio.Model.Filters
open FSharp.Nullable

type GallioParams = 
    { DoNotRun: bool
      EchoResults: bool
      IgnoreAnnotations: bool
      RunTimeLimit: TimeSpan option
      ConfigurationFilePath: string
      InstallationConfiguration: string
      ShowReports: bool
      ReportFormatterOptions: (string*string) list
      TestExecutionOptions: (string*string) list
      TestExplorationOptions: (string*string) list
      TestRunnerOptions: (string*string) list
      Verbosity: Verbosity

      /// The types supported "out of the box" are: Local, IsolatedAppDomain
      /// and IsolatedProcess (default), but more types could be available as plugins.
      RunnerType: string

      /// Specifies the type, assembly, and parameters of custom test runner
      /// extensions to use during the test run.
      /// The value must be in the form '[Namespace.]Type,Assembly[;Parameters]'
      RunnerExtensions: string list // use type + parameters instead of descriptor as string?
      HintDirectories: string seq
      PluginDirectories: string seq 
      ApplicationBaseDirectory: DirectoryInfo
      WorkingDirectory: DirectoryInfo
      ShadowCopy: bool option
      Debug: bool option
      RuntimeVersion: string 
      ReportDirectory: string 

      /// Sets the format string to use to generate the reports filenames.
      /// Any occurence of {0} will be replaced by the date, and any occurrence of {1} by the time.
      /// The default format string is test-report-{0}-{1}.
      ReportNameFormat: string 
      Filters: string // TODO use something more strongly-typed
      }

let GallioDefaults = 
    { DoNotRun = false
      EchoResults = true
      IgnoreAnnotations = false
      RunTimeLimit = None
      ConfigurationFilePath = null
      InstallationConfiguration = null
      ShowReports = true
      ReportFormatterOptions = []
      TestExecutionOptions = []
      TestExplorationOptions = []
      TestRunnerOptions = []
      Verbosity = Verbosity.Normal
      RunnerType = null
      RunnerExtensions = []
      HintDirectories = [] 
      PluginDirectories = []
      ApplicationBaseDirectory = null
      WorkingDirectory = null
      ShadowCopy = None
      Debug = None
      RuntimeVersion = null
      ReportDirectory = null 
      ReportNameFormat = null 
      Filters = null }

let inline private addProperties properties propertyContainer =
    properties
    |> Seq.iter (fun (k,v) -> (^a: (member AddProperty: string*string -> unit) (propertyContainer,k,v)))
    propertyContainer

let private createFilters param = 
    if isNullOrEmpty param.Filters 
        then FilterSet.Empty
        else FilterUtils.ParseTestFilterSet param.Filters        

let private createPackage param = 
    let package = TestPackage()
    if param.ApplicationBaseDirectory <> null then
        package.ApplicationBaseDirectory <- param.ApplicationBaseDirectory
    if param.WorkingDirectory <> null then
        package.WorkingDirectory <- param.WorkingDirectory
    match param.ShadowCopy with
    | Some v -> package.ShadowCopy <- v
    | _ -> ()
    match param.Debug with
    | Some v when v -> package.DebuggerSetup <- DebuggerSetup()
    | _ -> ()
    if param.RuntimeVersion <> null then
        package.RuntimeVersion <- param.RuntimeVersion
    param.HintDirectories
    |> Seq.map (fun x -> DirectoryInfo(x))
    |> Seq.iter package.AddHintDirectory
    package

let private createLogger param = 
    let logger = { new BaseLogger() with
                    override x.LogImpl(severity, message, exceptionData) = log message }
    FilteredLogger(logger, param.Verbosity)

let private createProject param package = 
    let runnerFactoryName = 
        match param.RunnerType with
        | null -> TestProject.DefaultTestRunnerFactoryName
        | x -> x
    let project = TestProject(
                    TestRunnerFactoryName = runnerFactoryName,
                    TestPackage = package
                  )
    param.RunnerExtensions |> Seq.iter project.AddTestRunnerExtensionSpecification
    if param.ReportDirectory <> null then
        project.ReportDirectory <- param.ReportDirectory
    if param.ReportNameFormat <> null then
        project.ReportNameFormat <- param.ReportNameFormat
    project

let private createExecutionOptions param = 
    let opt = TestExecutionOptions(FilterSet = createFilters param)
    opt |> addProperties param.TestExecutionOptions

let private createRuntimeSetup param =
    let rtSetup = RuntimeSetup(RuntimePath = Path.GetDirectoryName(AssemblyUtils.GetFriendlyAssemblyLocation(typeof<GallioParams>.Assembly)))
    param.PluginDirectories |> Seq.iter rtSetup.AddPluginDirectory
    rtSetup

let Run (setParam: GallioParams -> GallioParams) assemblies =
    let param = setParam GallioDefaults
    let package = createPackage param
    let project = createProject param package
    let logger = createLogger param

    let launcher = TestLauncher(
                    TestProject = project,
                    Logger = logger,
                    ProgressMonitorProvider = LogProgressMonitorProvider(logger),
                    DoNotRun = param.DoNotRun,
                    EchoResults = param.EchoResults,
                    ShowReports = param.ShowReports,
                    IgnoreAnnotations = param.IgnoreAnnotations,
                    ReportFormatterOptions = addProperties param.ReportFormatterOptions (ReportFormatterOptions()),
                    RunTimeLimit = Option.toNullable param.RunTimeLimit,
                    RuntimeSetup = createRuntimeSetup param,
                    TestExecutionOptions = createExecutionOptions param,
                    TestExplorationOptions = addProperties param.TestExplorationOptions (TestExplorationOptions()),
                    TestRunnerOptions = addProperties param.TestRunnerOptions (TestRunnerOptions())
                   )
    assemblies |> Seq.iter launcher.AddFilePattern

    let result = launcher.Run()
    log result.ResultSummary
    match result.ResultCode with
    | ResultCode.Success | ResultCode.NoTests -> ()
    | _ -> failwith "Failed tests"