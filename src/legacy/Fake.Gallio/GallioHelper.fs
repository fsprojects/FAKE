[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type ReportArchiveMode = Normal | Zip
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Verbosity = Quiet | Normal | Verbose | Debug 

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type GallioParams = 
      /// Sets whether to load the tests but not run them.
    { DoNotRun: bool
      /// Sets whether to echo results to the screen as tests finish.
      EchoResults: bool
      /// Sets whether to ignore annotations when determining the result code.
      IgnoreAnnotations: bool
      /// Sets the maximum amount of time (in seconds) the tests can run 
      /// before they are canceled.
      RunTimeLimit: TimeSpan option
      /// Sets whether to show generated reports in a window using the default system application
      /// registered to the report file type.
      ShowReports: bool
      /// Specifies option property key/value pairs for the report formatter
      ReportFormatterOptions: (string*string) list
      TestExecutionOptions: (string*string) list
      TestExplorationOptions: (string*string) list
      /// Specifies option property key/value pairs for the test runner.
      TestRunnerOptions: (string*string) list
      Verbosity: Verbosity
      /// The types supported "out of the box" are: Local, IsolatedAppDomain
      /// and IsolatedProcess (default), but more types could be available as plugins.
      RunnerType: string
      /// Specifies the type, assembly, and parameters of custom test runner
      /// extensions to use during the test run.
      /// The value must be in the form '[Namespace.]Type,Assembly[;Parameters]'
      RunnerExtensions: string list // use type + parameters instead of descriptor as string?
      /// The list of directories used for loading referenced assemblies and other dependent resources.
      HintDirectories: string seq
      /// Additional Gallio plugin directories to search recursively.
      PluginDirectories: string seq 
      /// Gets or sets the relative or absolute path of the application base directory,
      /// or null to use a default value selected by the consumer.
      ApplicationBaseDirectory: DirectoryInfo
      /// Gets or sets the relative or absolute path of the working directory
      /// or null to use a default value selected by the consumer.
      WorkingDirectory: DirectoryInfo
      /// Shadow copying allows the original assemblies to be modified while the tests are running.
      /// However, shadow copying may occasionally cause some tests to fail if they depend on their original location.
      ShadowCopy: bool option
      /// Attaches the debugger to the test process when set to true.
      Debug: bool option
      /// Gets or sets the version of the .Net runtime to use for running tests.
      /// For the CLR, this must be the name of one of the framework directories in %SystemRoot%\Microsoft.Net\Framework.  eg. 'v2.0.50727'.
      /// The default is null which uses the most recent installed and supported framework.
      RuntimeVersion: string 
      /// Sets the name of the directory where the reports will be put.
      /// The directory will be created if it doesn't exist. Existing files will be overwritten.
      /// The default report directory is "Reports".
      ReportDirectory: string
      /// Sets the format string to use to generate the reports filenames.
      /// Any occurence of {0} will be replaced by the date, and any occurrence of {1} by the time.
      /// The default format string is test-report-{0}-{1}.
      ReportNameFormat: string 
      /// Test filters (i.e. exclusion rules)
      Filters: string // TODO use a EDSL instead of string descriptors?
      ReportArchive: ReportArchiveMode option
      }


/// Default Gallio parameters
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let GallioDefaults = 
    { DoNotRun = false
      EchoResults = true
      IgnoreAnnotations = false
      RunTimeLimit = None
      ShowReports = true
      ReportFormatterOptions = []
      TestExecutionOptions = []
      TestExplorationOptions = []
      TestRunnerOptions = []
      Verbosity = Normal
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
      Filters = null
      ReportArchive = None }

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
    let v = match param.Verbosity with
            | Quiet -> Logging.Verbosity.Quiet
            | Normal -> Logging.Verbosity.Normal
            | Verbose -> Logging.Verbosity.Verbose
            | Debug -> Logging.Verbosity.Debug
    FilteredLogger(logger, v)

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
    match param.ReportArchive with
    | None -> ()
    | Some v -> 
        project.ReportArchive <- 
            match v with
            | ReportArchiveMode.Normal -> ReportArchive.Normal
            | ReportArchiveMode.Zip -> ReportArchive.Zip

    project

let private createExecutionOptions param = 
    let opt = TestExecutionOptions(FilterSet = createFilters param)
    opt |> addProperties param.TestExecutionOptions

let private createRuntimeSetup param =
    let rtSetup = RuntimeSetup(RuntimePath = Path.GetDirectoryName(AssemblyUtils.GetFriendlyAssemblyLocation(typeof<GallioParams>.Assembly)))
    param.PluginDirectories |> Seq.iter rtSetup.AddPluginDirectory
    rtSetup

let private createLauncher param = 
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
                    RunTimeLimit = (match param.RunTimeLimit with | None -> Nullable() | Some x -> Nullable(x)),
                    RuntimeSetup = createRuntimeSetup param,
                    TestExecutionOptions = createExecutionOptions param,
                    TestExplorationOptions = addProperties param.TestExplorationOptions (TestExplorationOptions()),
                    TestRunnerOptions = addProperties param.TestRunnerOptions (TestRunnerOptions())
                   )
    launcher

/// <summary>
/// Runs tests through Gallio.
/// </summary>
/// <param name="setParam">Function that modifies the default parameters</param>
/// <param name="assemblies">List of test assemblies</param>
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Run (setParam: GallioParams -> GallioParams) assemblies =
    let param = setParam GallioDefaults
    let launcher = createLauncher param
    assemblies |> Seq.iter launcher.AddFilePattern

    let result = launcher.Run()
    log result.ResultSummary
    match result.ResultCode with
    | ResultCode.Success | ResultCode.NoTests -> ()
    | _ -> failwith "Failed tests"
