/// Contains tasks to run [MSTest](http://en.wikipedia.org/wiki/Visual_Studio_Unit_Testing_Framework/) unit tests.
module Fake.MSTest

open System
open System.Text

/// [omit]
let mstestPaths = 
    [| "[ProgramFilesX86]\Microsoft Visual Studio 12.0\Common7\IDE"; 
       "[ProgramFilesX86]\Microsoft Visual Studio 11.0\Common7\IDE"; 
       "[ProgramFilesX86]\Microsoft Visual Studio 10.0\Common7\IDE" |]

/// [omit]
let mstestexe = 
    if isMono then failwith "MSTest is not supported on mono platform"
    else "mstest.exe"

// TODO: try to use VSTest.Console.exe as well (VS2012 and up only)
/// Option which allow to specify if a MSTest error should break the build.
type ErrorLevel = TestRunnerErrorLevel

/// Parameter type to configure the MSTest.exe.
type MSTestParams = 
    { /// Test category filter  (optional). The test category filter consists of one or more test category names separated by the logical operators '&', '|', '!', '&!'. The logical operators '&' and '|' cannot be used together to create a test category filter.
      Category : string
      /// Test results directory (optional)
      ResultsDir : string
      /// Path to the Test Metadata file (.vdmdi)  (optional)
      TestMetadataPath : string
      /// Working directory (optional)
      WorkingDir : string
      /// A timeout for the test runner (optional)
      TimeOut : TimeSpan
      /// Path to MSTest.exe 
      ToolPath : string
      /// Option which allow to specify if a MSTest error should break the build.
      ErrorLevel : ErrorLevel
      /// Run tests in isolation.
      Isolate : bool }

/// MSTest default parameters.
let MSTestDefaults = 
    { Category = null
      ResultsDir = null
      TestMetadataPath = null
      WorkingDir = null
      TimeOut = TimeSpan.FromMinutes 5.
      ToolPath = 
          match tryFindFile mstestPaths mstestexe with
          | Some path -> path
          | None -> ""
      ErrorLevel = ErrorLevel.Error
      Isolate = false }

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
let buildMSTestArgs parameters assembly = 
    let testResultsFile = 
        if parameters.ResultsDir <> null then 
            sprintf @"%s\%s.trx" parameters.ResultsDir (DateTime.Now.ToString("yyyyMMdd-HHmmss.ff"))
        else null
    new StringBuilder()
    |> appendIfNotNull assembly "/testcontainer:"
    |> appendIfNotNull parameters.Category "/category:"
    |> appendIfNotNull testResultsFile "/resultsfile:"
    |> appendIfFalse parameters.Isolate "/noisolation"
    |> toText

/// Runs MSTest command line tool on a group of assemblies.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default MSTestParams value.
///  - `assemblies` - Sequence of one or more assemblies containing Microsoft Visual Studio Unit Test Framework unit tests.
/// 
/// ## Sample usage
///
///     Target "Test" (fun _ ->
///         !! (testDir + @"\*.Tests.dll") 
///           |> MSTest (fun p -> { p with Category = "group1" })
///     )
let MSTest (setParams : MSTestParams -> MSTestParams) (assemblies : string seq) = 
    let details = assemblies |> separated ", "
    traceStartTask "MSTest" details
    let parameters = MSTestDefaults |> setParams
    let assemblies = assemblies |> Seq.toArray
    if Array.isEmpty assemblies then failwith "MSTest: cannot run tests (the assembly list is empty)."
    let failIfError assembly exitCode = 
        if exitCode > 0 && parameters.ErrorLevel = ErrorLevel.Error then 
            let message = sprintf "%sMSTest test run failed for %s" Environment.NewLine assembly
            traceError message
            failwith message
    for assembly in assemblies do
        let args = buildMSTestArgs parameters assembly
        ExecProcess (fun info -> 
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
        |> failIfError assembly
    traceEndTask "MSTest" details
