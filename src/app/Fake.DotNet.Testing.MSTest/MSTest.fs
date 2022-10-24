namespace Fake.DotNet.Testing

/// <summary>
/// Contains tasks to run
/// <a href="https://en.wikipedia.org/wiki/Visual_Studio_Unit_Testing_Framework">MSTest</a> unit tests.
/// </summary>
module MSTest =

    open System
    open System.Text
    open Fake.Core
    open Fake.Testing.Common

    let internal mstestPaths =
        [| @"[ProgramFilesX86]\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\"
           @"[ProgramFilesX86]\Microsoft Visual Studio\2019\Professional\Common7\IDE\"
           @"[ProgramFilesX86]\Microsoft Visual Studio\2019\Community\Common7\IDE\"
           @"[ProgramFilesX86]\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\"
           @"[ProgramFilesX86]\Microsoft Visual Studio\2017\Professional\Common7\IDE\"
           @"[ProgramFilesX86]\Microsoft Visual Studio\2017\Community\Common7\IDE\"
           @"[ProgramFilesX86]\Microsoft Visual Studio 14.0\Common7\IDE"
           @"[ProgramFilesX86]\Microsoft Visual Studio 12.0\Common7\IDE"
           @"[ProgramFilesX86]\Microsoft Visual Studio 11.0\Common7\IDE"
           @"[ProgramFilesX86]\Microsoft Visual Studio 10.0\Common7\IDE" |]

    let internal msTestExe =
        if Environment.isWindows then
            "mstest.exe"
        else
            failwith "MSTest is only supported on Windows platform"

    /// <summary>
    /// Option which allow to specify if a MSTest error should break the build.
    /// </summary>
    type ErrorLevel = TestRunnerErrorLevel

    /// <summary>
    /// Parameter type to configure the MSTest.exe.
    /// </summary>
    [<CLIMutable>]
    type MSTestParams =
        {
            /// Test category filter  (optional). The test category filter consists of one or more test category names
            /// separated by the logical operators '&', '|', '!', '&!'. The logical operators '&' and '|' cannot be
            /// used together to create a test category filter.
            Category: string

            /// Test results directory (optional)
            ResultsDir: string

            /// Path to the Test Metadata file (.vsmdi)  (optional)
            TestMetadataPath: string

            /// Path to the Test Settings file (.testsettings)  (optional)
            TestSettingsPath: string

            /// Working directory (optional)
            WorkingDir: string

            /// List of tests be run (optional)
            Tests: string list

            /// A timeout for the test runner (optional)
            TimeOut: TimeSpan

            /// Path to MSTest.exe
            ToolPath: string

            /// List of additional test case properties to display, if they exist (optional)
            Details: string list

            /// Option which allow to specify if a MSTest error should break the build.
            ErrorLevel: ErrorLevel

            /// Run tests in isolation (optional).
            NoIsolation: bool
        }

    /// MSTest default parameters.
    let MSTestDefaults =
        { Category = null
          ResultsDir = null
          TestMetadataPath = null
          TestSettingsPath = null
          WorkingDir = null
          Tests = []
          TimeOut = TimeSpan.FromMinutes 5.
          ToolPath =
            match ProcessUtils.tryFindLocalTool "TOOL" msTestExe mstestPaths with
            | Some path -> path
            | None -> ""
          Details = []
          ErrorLevel = ErrorLevel.Error
          NoIsolation = true }

    /// <summary>
    /// Builds the command line arguments from the given parameter record and the given assemblies.
    /// </summary>
    let buildArgs (parameters: MSTestParams) (assembly: string) =
        let testResultsFile =
            if parameters.ResultsDir <> null then
                sprintf @"%s\%s.trx" parameters.ResultsDir (DateTime.Now.ToString("yyyyMMdd-HHmmss.ff"))
            else
                null

        StringBuilder()
        |> StringBuilder.appendIfNotNull assembly "/testcontainer:"
        |> StringBuilder.appendIfNotNull parameters.Category "/category:"
        |> StringBuilder.appendIfNotNull parameters.TestMetadataPath "/testmetadata:"
        |> StringBuilder.appendIfNotNull parameters.TestSettingsPath "/testsettings:"
        |> StringBuilder.appendIfNotNull testResultsFile "/resultsfile:"
        |> StringBuilder.appendIfTrue parameters.NoIsolation "/noisolation"
        |> StringBuilder.forEach parameters.Tests StringBuilder.appendIfNotNullOrEmpty "/test:"
        |> StringBuilder.forEach parameters.Details StringBuilder.appendIfNotNullOrEmpty "/detail:"
        |> StringBuilder.toText

    /// <summary>
    /// Runs MSTest command line tool on a group of assemblies.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default MSTestParams value.</param>
    /// <param name="assemblies">Sequence of one or more assemblies containing Microsoft Visual Studio Unit
    /// Test Framework unit tests.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "Test" (fun _ ->
    ///         !! (testDir + @"\*.Tests.dll")
    ///           |> MSTest (fun p -> { p with Category = "group1" })
    ///     )
    /// </code>
    /// </example>
    let exec (setParams: MSTestParams -> MSTestParams) (assemblies: string seq) =
        let details = assemblies |> String.separated ", "
        use __ = Trace.traceTask "MSTest" details
        let parameters = MSTestDefaults |> setParams
        let assemblies = assemblies |> Seq.toArray

        if Array.isEmpty assemblies then
            failwith "MSTest: cannot run tests (the assembly list is empty)."

        let failIfError assembly exitCode =
            if exitCode > 0 && parameters.ErrorLevel <> ErrorLevel.DontFailBuild then
                let message = sprintf "%sMSTest test run failed for %s" Environment.NewLine assembly
                Trace.traceError message
                failwith message

        for assembly in assemblies do
            let args = buildArgs parameters assembly

            let processResult =
                CreateProcess.fromRawCommandLine parameters.ToolPath args
                |> CreateProcess.withWorkingDirectory parameters.WorkingDir
                |> CreateProcess.withTimeout parameters.TimeOut
                |> CreateProcess.withFramework
                |> Proc.run

            failIfError assembly processResult.ExitCode

        __.MarkSuccess()
