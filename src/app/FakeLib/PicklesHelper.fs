/// Contains tasks to run the [Pickles](http://www.picklesdoc.com/) living documentation generator
module Fake.PicklesHelper

open System
open System.IO
open System.Text
open Fake

(*
.\packages\Pickles.CommandLine\tools\pickles.exe  --help                                                 
Pickles version 2.6.1.0
  -f, --feature-directory=VALUE
                             directory to start scanning recursively for
                               features
  -o, --output-directory=VALUE
                             directory where output files will be placed
      --trfmt, --test-results-format=VALUE
                             the format of the linked test results
                               (nunit|xunit)
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
*)

/// Option which allows to specify if failure of pickles should break the build.
type PicklesErrorLevel = 
    /// This option instructs FAKE to break the build if pickles fails to execute
    | Error
    /// With this option set, no exception is thrown if pickles fails to execute
    | DontFailBuild

/// The format of the test results
type TestResultsFormat =
    | Nunit
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
  
/// The Pickles parameter type
type PicklesParams =
    { /// The path to the Pickles console tool: 'pickles.exe'      
      ToolPath : string
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
      ErrorLevel : PicklesErrorLevel
      /// Option which allows to enable some experimental features
      IncludeExperimentalFeatures : bool option
    }

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
let PicklesDefaults =
    {
      ToolPath = findToolInSubPath "pickles.exe" currentDirectory
      FeatureDirectory = currentDirectory
      FeatureFileLanguage = None
      OutputDirectory = currentDirectory @@ "Documentation"
      OutputFileFormat = DHTML
      TestResultsFormat = NUnit
      LinkedTestResultFiles = []
      SystemUnderTestName = None
      SystemUnderTestVersion = None
      TimeOut = TimeSpan.FromMinutes 5.
      ErrorLevel = Error
      IncludeExperimentalFeatures = None
    }
    
let buildPicklesArgs parameters =
    let outputFormat = match parameters.OutputFileFormat with
                       | DHTML -> "dhtml"
                       | HTML -> "html"
                       | Word -> "word"
                       | JSON -> "json"
                       | Excel -> "excel"
                       
    let testResultFormat = match parameters.LinkedTestResultFiles with
                           | [] -> None
                           | _  -> match parameters.TestResultsFormat with
                                   | Nunit -> Some "nunit"
                                   | NUnit -> Some "nunit"
                                   | NUnit3 -> Some "nunit3"
                                   | XUnit -> Some "xunit"
                                   | XUnit2 -> Some "xunit2"
                                   | MSTest -> Some "mstest"
                                   | CucumberJSON -> Some "cucumberjson"
                                   | SpecRun -> Some "specrun"
                                   | VSTest -> Some "vstest"
    
    let linkedResultFiles = match parameters.LinkedTestResultFiles with
                            | [] -> None
                            | _ -> parameters.LinkedTestResultFiles
                                   |> Seq.map (fun f -> sprintf "\"%s\"" f) 
                                   |> String.concat ";"
                                   |> Some
    let experimentalFeatures = match parameters.IncludeExperimentalFeatures with
                               | Some true -> Some "--exp"
                               | _ -> None
                                   
    new StringBuilder()
    |> appendWithoutQuotes (sprintf " -f \"%s\"" parameters.FeatureDirectory)
    |> appendWithoutQuotes (sprintf " -o \"%s\"" parameters.OutputDirectory)
    |> appendIfSome parameters.SystemUnderTestName (sprintf " --sn %s")
    |> appendIfSome parameters.SystemUnderTestVersion (sprintf " --sv %s")
    |> appendIfSome parameters.FeatureFileLanguage (sprintf " -l %s")
    |> appendWithoutQuotes (sprintf " --df %s" outputFormat)
    |> appendIfSome testResultFormat (sprintf " --trfmt %s")
    |> appendIfSome linkedResultFiles (sprintf " --lr %s")
    |> appendIfSome experimentalFeatures (sprintf "%s")
    |> toText
    
module internal ResultHandling = 
    let (|OK|Failure|) = function
        | 0 -> OK
        | x -> Failure x
        
    let buildErrorMessage = function
        | OK -> None
        | Failure errorCode ->
            Some (sprintf "Pickles reported an error (Error code %d)" errorCode)
            
    let failBuildWithMessage = function
        | DontFailBuild -> traceImportant
        | _ -> failwith
        
    let failBuildIfPicklesReportedError errorLevel =
        buildErrorMessage
        >> Option.iter (failBuildWithMessage errorLevel)
        
        
/// Runs pickles living documentation generator via the given tool
/// Will fail if the pickles command line tool terminates with a non zero exit code.
///
/// The pickles command line tool terminates with a non-zero exit code if there
/// is any error.
///
/// ## Parameters
///  - `setParams` - Function used to manipulate the default `PicklesParams` value
///
/// ## Sample usage
///
///     Target "BuildDoc" (fun _ ->
///        Pickles (fun p -> { p with
///                            FeatureDirectory = currentDirectory @@ "Specs"
///                            OutputDirectory = currentDirectory @@ "SpecDocs" })
///     )
let Pickles setParams =
    traceStartTask "Pickles" ""
    let parameters = setParams PicklesDefaults
    let result = 
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- "."
            info.Arguments <- parameters |> buildPicklesArgs) parameters.TimeOut
    
    ResultHandling.failBuildIfPicklesReportedError parameters.ErrorLevel result
    
    traceEndTask "Pickles" ""
