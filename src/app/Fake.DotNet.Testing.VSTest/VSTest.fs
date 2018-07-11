/// Contains tasks to run [VSTest](https://msdn.microsoft.com/en-us/library/ms182486.aspx) unit tests.
[<RequireQualifiedAccess>]
module Fake.DotNet.Testing.VSTest

open Fake.Core
open Fake.Testing.Common
open System
open System.Text

/// [omit]
let private vsTestPaths = 
    [|
        @"[ProgramFilesX86]\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow"
        @"[ProgramFilesX86]\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow"
        @"[ProgramFilesX86]\Microsoft Visual Studio 12.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow"
        @"[ProgramFilesX86]\Microsoft Visual Studio 11.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow"
    |]

/// [omit]
let private vsTestExe = 
    if Environment.isMono then failwith "VSTest is not supported on the mono platform"
    else "vstest.console.exe"

/// Option which allow to specify if a VSTest error should break the build.
type ErrorLevel = TestRunnerErrorLevel

/// Parameter type to configure [VSTest.Console.exe](https://msdn.microsoft.com/en-us/library/jj155800.aspx)
type VSTestParams = 
    { /// Path to the run settings file to run tests with additional settings such as data collectors (optional).
      SettingsPath : string
      /// Names of the tests that should be run (optional).
      Tests : seq<string>
      /// Enables code coverage collection (optional).
      EnableCodeCoverage : bool
      /// Run the tests in an isolated process (optional).
      InIsolation : bool
      /// Use installed VSIX extensions in VSTest (optional).
      UseVsixExtensions : bool
      /// Target platform architecture for test execution (optional). Valid options include "x86", "x64" and "ARM".
      Platform : string
      /// Target .NET framework version to use for test execution (optional).
      Framework : string
      /// Run tests that match the given expression (optional). Cannot be used with the Tests argument
      TestCaseFilter : string
      /// The logger to use for test results (optional).
      Logger : string
      /// List discovered tests from the given container path (optional).
      ListTestsPath : string
      /// List installed test discoverers (optional).
      ListDiscoverers : bool
      /// List installed test executors (optional).
      ListExecutors : bool
      /// List installed loggers (optional).
      ListLoggers : bool
      /// List installed settings providers (optional).
      ListSettingsProviders : bool 
      /// Path to VSTest.Console.exe (optional). By default the default install location is searched.
      ToolPath : string
      /// Working directory (optional).
      WorkingDir : string
      /// A timeout for the test runner (optional).
      TimeOut : TimeSpan
      /// Error level for controlling how VSTest failures should break the build (optional).
      ErrorLevel : ErrorLevel 
      /// Path to test adapter e.g. xUnit (optional)
      TestAdapterPath: string}

/// VSTest default parameters.
let private VSTestDefaults = 
    { SettingsPath = null
      Tests = []
      EnableCodeCoverage = false
      InIsolation = true
      UseVsixExtensions = false
      Platform = null
      Framework = null
      TestCaseFilter = null
      Logger = null
      ListTestsPath = null
      ListDiscoverers = false
      ListExecutors = false
      ListLoggers = false
      ListSettingsProviders = false
      ToolPath = 
          match Process.tryFindFile vsTestPaths vsTestExe with
          | Some path -> path
          | None -> ""
      WorkingDir = null
      TimeOut = TimeSpan.MaxValue
      ErrorLevel = ErrorLevel.Error
      TestAdapterPath = null }

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// [omit]
let private buildVSTestArgs (parameters : VSTestParams) assembly = 
    let testsToRun = 
        if not (Seq.isEmpty parameters.Tests) then 
            sprintf @"/Tests:%s" (parameters.Tests |> String.separated ",")
        else null
    new StringBuilder()
    |> StringBuilder.appendIfTrue (assembly <> null) assembly
    |> StringBuilder.appendIfNotNull parameters.SettingsPath "/Settings:"
    |> StringBuilder.appendIfTrue (testsToRun <> null) testsToRun
    |> StringBuilder.appendIfTrue parameters.EnableCodeCoverage "/EnableCodeCoverage"
    |> StringBuilder.appendIfTrue parameters.InIsolation "/InIsolation"
    |> StringBuilder.appendIfTrue parameters.UseVsixExtensions "/UseVsixExtensions:true"
    |> StringBuilder.appendIfNotNull parameters.Platform "/Platform:"
    |> StringBuilder.appendIfNotNull parameters.Framework "/Framework:"
    |> StringBuilder.appendIfNotNull parameters.TestCaseFilter "/TestCaseFilter:"
    |> StringBuilder.appendIfNotNull parameters.Logger "/Logger:"
    |> StringBuilder.appendIfNotNull parameters.ListTestsPath "/ListTests:"
    |> StringBuilder.appendIfTrue parameters.ListDiscoverers "/ListDiscoverers"
    |> StringBuilder.appendIfTrue parameters.ListExecutors "/ListExecutors"
    |> StringBuilder.appendIfTrue parameters.ListLoggers "/ListLoggers"
    |> StringBuilder.appendIfTrue parameters.ListSettingsProviders "/ListSettingsProviders"
    |> StringBuilder.appendIfNotNull parameters.TestAdapterPath "/TestAdapterPath:"
    |> StringBuilder.toText

/// Runs the VSTest command line tool (VSTest.Console.exe) on a group of assemblies.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default VSTestParams values.
///  - `assemblies` - Sequence of one or more assemblies containing Microsoft Visual Studio Unit Test Framework unit tests.
/// 
/// ## Sample usage
///
///     Target.create "Test" (fun _ ->
///         !! (testDir + @"\*.Tests.dll") 
///           |> VSTest.run (fun p -> { p with SettingsPath = "Local.RunSettings" })
///     )
let run (setParams : VSTestParams -> VSTestParams) (assemblies : string seq) = 
    let details = assemblies |> String.separated ", "
    use __ = Trace.traceTask "VSTest" details
    let parameters = VSTestDefaults |> setParams
    if String.IsNullOrEmpty parameters.ToolPath then failwith "VSTest: No tool path specified, or it could not be found automatically."
    let assemblies = assemblies |> Seq.toArray
    if Array.isEmpty assemblies then failwith "VSTest: cannot run tests (the assembly list is empty)."
    let failIfError assembly exitCode = 
        if exitCode > 0 && parameters.ErrorLevel <> ErrorLevel.DontFailBuild then 
            let message = sprintf "%sVSTest test run failed for %s" Environment.NewLine assembly
            Trace.traceError message
            failwith message
    for assembly in assemblies do
        let args = buildVSTestArgs parameters assembly
        Process.execSimple (fun info -> 
            { info with 
                FileName = parameters.ToolPath
                WorkingDirectory = parameters.WorkingDir
                Arguments = args }) parameters.TimeOut
        |> failIfError assembly
