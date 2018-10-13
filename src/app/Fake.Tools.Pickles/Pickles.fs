/// Contains tasks to run the [Pickles](http://www.picklesdoc.com/) living documentation generator
///
/// ## Sample usage
/// 
/// ```
/// open Fake.Tools
/// 
/// Target "BuildDoc" (fun _ ->
///     Pickles.convert (fun p -> { p with
///                                 FeatureDirectory = currentDirectory @@ "Specs"
///                                 OutputDirectory = currentDirectory @@ "SpecDocs" })
/// )
/// ```
///

[<RequireQualifiedAccess>]
module Fake.Tools.Pickles

open System
open System.IO

open Fake.Core
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.FileSystemOperators

(*
.\packages\Pickles.CommandLine\tools\pickles.exe  --help                                                 
Pickles version 2.19.0.0
  -f, --feature-directory=VALUE
                             directory to start scanning recursively for
                               features
  -o, --output-directory=VALUE
                             directory where output files will be placed
      --trfmt, --test-results-format=VALUE
                             the format of the linked test results
                               (nunit|nunit3|xunit|xunit2|mstest
                               |cucumberjson|specrun|vstest)
      --lr, --link-results-file=VALUE
                             the path to the linked test results file (can be
                               a semicolon-separated list of files)
      --sn, --system-under-test-name=VALUE
                             the name of the system under test
      --sv, --system-under-test-version=VALUE
                             the version of the system under test
  -l, --language=VALUE       the language of the feature files
      --df, --documentation-format=VALUE
                             the format of the output documentation
  -v, --version
  -h, -?, --help
      --exp, --include-experimental-features
                             whether to include experimental features
      --cmt, --enableComments=VALUE
                             whether to enable comments in the output
      --et, --excludeTags=VALUE
                             exclude scenarios that match this tag
      --ht, --hideTags=VALUE Technical tags that shouldn't be displayed
                               (separated by ;)*)

/// Option which allows to specify if failure of pickles should break the build.
type ErrorLevel =
  /// This option instructs FAKE to break the build if pickles fails to execute
  | Error
  /// With this option set, no exception is thrown if pickles fails to execute
  | DontFailBuild

/// The format of the test results
type TestResultsFormat =
  | NUnit
  | NUnit3
  | XUnit
  | XUnit2
  | MSTest
  | CucumberJSON
  | SpecRun
  | VSTest
    
 type DocumentationFormat =
  | DHTML
  | HTML
  | Word
  | JSON
  | Excel
  | CucumberJSON

/// The Pickles parameter type
type PicklesParams =
  { /// The path to the Pickles console tool: 'pickles.exe'      
    ToolPath : string
    /// The working directory
    WorkingDir: string
    /// The directory to start scanning recursively for features
    FeatureDirectory: string
    /// The language of the feature files
    FeatureFileLanguage: string option
    /// The directory where output files will be placed
    OutputDirectory: string
    /// The format of the output documentation 
    OutputFileFormat: DocumentationFormat
    /// the format of the linked test results
    TestResultsFormat: TestResultsFormat
    /// the paths to the linked test results files
    LinkedTestResultFiles: string list
    /// The name of the system under test
    SystemUnderTestName: string option
    /// The version of the system under test
    SystemUnderTestVersion: string option
    /// Maximum time to allow xUnit to run before being killed.
    TimeOut : TimeSpan
    /// Option which allows to specify if failure of pickles should break the build.
    ErrorLevel : ErrorLevel
    /// Option which allows to enable some experimental features
    IncludeExperimentalFeatures : bool option
    /// As of version 2.6, Pickles includes Gherkin #-style comments. As of version 2.7, this inclusion is configurable.
    EnableComments: bool option
    /// exclude scenarios that match this tags
    ExcludeTags: string list
    /// Technical tags that shouldn't be displayed
    HideTags: string list
  }

let private currentDirectory = Directory.GetCurrentDirectory()

/// The Pickles default parameters
///
/// ## Defaults 
///
/// - `ToolPath` - The `pickles.exe` if it exists in a subdirectory of the current directory
/// - `FeatureDirectory` - 'currentDirectory'
/// - `FeatureFileLanguage` - 'None' (defaults to `en`)
/// - `OutputDirectory` - `currentDirectory @@ "Documentation"`
/// - `OutputFileFormat` - `DHTML`
/// - `TestResultsFormat` - `Nunit`
/// - `LinkedTestResultFiles` - []
/// - `SystemUnderTestName` - `None`
/// - `SystemUnderTestVersion` - `None`
/// - `TimeOut` - 5 minutes
/// - `ErrorLevel` - `Error`
/// - `IncludeExperimentalFeatures` - `None` 
/// - `EnableComments` - true
/// - `ExcludeTags` - []
/// - `HideTags` - []
let private PicklesDefaults =
  {
    ToolPath = Tools.findToolInSubPath "pickles.exe" currentDirectory
    WorkingDir = currentDirectory
    FeatureDirectory = null
    FeatureFileLanguage = None
    OutputDirectory = null
    OutputFileFormat = DHTML
    TestResultsFormat = NUnit
    LinkedTestResultFiles = []
    SystemUnderTestName = None
    SystemUnderTestVersion = None
    TimeOut = TimeSpan.FromMinutes 5.
    ErrorLevel = Error
    IncludeExperimentalFeatures = None
    EnableComments = None
    ExcludeTags = []
    HideTags = []
  }
    
let private buildPicklesArgs parameters =
  let experimentalFeatures =
    seq {
      match parameters.IncludeExperimentalFeatures with
      | Some true -> yield "--exp"
      | _ -> ()
    }
                                   
  let enableComments =
    seq {
      match parameters.EnableComments with
      | Some true -> yield "--enableComments=true"
      | Some false -> yield "--enableComments=false"
      | _ -> ()
    }
  
  let yieldIfNotNullOrWhitespace paramName value =
    seq {
      if String.isNullOrWhiteSpace value
      then ()
      else 
        yield sprintf "-%s" paramName
        yield value
    }

  let yieldIfSome paramName value =
    seq {
      match value with
      | Some v ->
          yield sprintf "--%s" paramName
          yield v
      | _ -> ()
    }

  let yieldTags paramName value =
    seq {
      match value with
      | [] -> ()
      | tags ->
          yield sprintf "--%s" paramName
          yield tags |> String.concat ";" 
    }
    
  [
    yield! parameters.FeatureDirectory |> yieldIfNotNullOrWhitespace "f"
    yield! parameters.OutputDirectory |> yieldIfNotNullOrWhitespace "o"
    yield! parameters.SystemUnderTestName |> yieldIfSome "sn"
    yield! parameters.SystemUnderTestVersion |> yieldIfSome "sv"
    yield! parameters.FeatureFileLanguage |> yieldIfSome "l"
    yield! match parameters.OutputFileFormat |> string |> String.toLower with
           | "html" -> None
           | v -> Some v 
           |> yieldIfSome "df"
    yield! match parameters.LinkedTestResultFiles with
           | [] -> None
           | _  -> parameters.TestResultsFormat 
                   |> string 
                   |> String.toLower 
                   |> Some 
           |> yieldIfSome "trfmt"
    yield! match parameters.LinkedTestResultFiles with
           | [] -> None
           | _ -> parameters.LinkedTestResultFiles
                  |> String.concat ";"
                  |> Some
           |> yieldIfSome "lr"
    yield! experimentalFeatures
    yield! enableComments
    yield! parameters.ExcludeTags |> yieldTags "et"
    yield! parameters.HideTags |> yieldTags "ht"
  ]
  |> Arguments.OfArgs

module internal ResultHandling =
  let (|OK|Failure|) = function
  | 0 -> OK
  | x -> Failure x
        
  let buildErrorMessage = function
  | OK -> None
  | Failure errorCode ->
      Some (sprintf "Pickles reported an error (Error code %d)" errorCode)
            
  let failBuildWithMessage = function
  | DontFailBuild -> Trace.traceImportant
  | _ -> failwith
        
  let failBuildIfPicklesReportedError errorLevel =
    buildErrorMessage
    >> Option.iter (failBuildWithMessage errorLevel)
        

/// Builds the report generator command line arguments and process from the given parameters and reports
/// [omit]
let internal createProcess setParams =
  let parameters = setParams PicklesDefaults
  let args = buildPicklesArgs parameters
  let tool = parameters.ToolPath
  
  CreateProcess.fromCommand (RawCommand(tool, args))
  |> CreateProcess.withFramework
  |> CreateProcess.withWorkingDirectory parameters.WorkingDir
  |> CreateProcess.withTimeout parameters.TimeOut
  |> CreateProcess.addOnExited 
      (fun data exitCode ->
        ResultHandling.failBuildIfPicklesReportedError parameters.ErrorLevel exitCode
        data)
  |> fun command ->
    Trace.trace command.CommandLine
    command


/// Runs pickles living documentation generator via the given tool
/// Will fail if the pickles command line tool terminates with a non zero exit code.
///
/// The pickles command line tool terminates with a non-zero exit code if there
/// is any error.
///
/// ## Parameters
///  - `setParams` - Function used to manipulate the default `PicklesParams` value
let convert setParams =
  use __ = Trace.traceTask "Pickles" "Generating documentations"

  let result =
    createProcess setParams
    |> Proc.run
    |> ignore
    
  __.MarkSuccess()
