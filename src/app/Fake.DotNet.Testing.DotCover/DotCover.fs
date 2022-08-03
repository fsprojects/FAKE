namespace Fake.DotNet.Testing

/// Contains a task which can be used to run [DotCover](http://www.jetbrains.com/dotcover/) on .NET assemblies.
[<RequireQualifiedAccess>]
module DotCover =

    open System
    open System.IO
    open System.Text
    open Fake.Core
    open Fake.IO.FileSystemOperators
    open Fake.Testing.Common
    open Fake.DotNet.Testing.MSTest
    open Fake.DotNet.Testing.MSpec
    open Fake.DotNet.Testing.NUnit3
    open Fake.DotNet.Testing.XUnit2

    /// The coverage report type
    type ReportType =
        | Html = 0
        | Json = 1
        | Xml = 2
        | NDependXml = 3
        | DetailedXml = 4
        | SummaryXml = 5

    /// The dotCover parameter type for running coverage
    type Params =
        { ToolPath: string
          WorkingDir: string
          TargetExecutable: string
          TargetArguments: string
          TargetWorkingDir: string
          Output: string
          Filters: string
          ErrorLevel: TestRunnerErrorLevel
          AttributeFilters: string
          CustomParameters: string }

    let internal toolPath toolName =
        let toolPath =
            ProcessUtils.tryFindLocalTool "TOOL" toolName [ (Path.GetFullPath ".") @@ "tools" @@ "DotCover" ]

        match toolPath with
        | Some path -> path
        | None -> toolName

    /// The dotCover default parameters
    let internal Defaults =
        { ToolPath = toolPath "dotCover.exe"
          WorkingDir = ""
          TargetExecutable = ""
          TargetArguments = ""
          TargetWorkingDir = ""
          Filters = ""
          AttributeFilters = ""
          Output = "dotCoverSnapshot.dcvr"
          CustomParameters = ""
          ErrorLevel = ErrorLevel.Error }

    /// The DotCover merge command parameters
    type MergeParams =
        { ToolPath: string
          WorkingDir: string
          Source: string list
          Output: string
          TempDir: string
          CustomParameters: string }

    let internal MergeDefaults =
        { ToolPath = toolPath "dotCover.exe"
          WorkingDir = ""
          Source = []
          Output = "dotCoverSnapshot.dcvr"
          TempDir = ""
          CustomParameters = "" }

    /// The DotCover report command parameters
    type ReportParams =
        { ToolPath: string
          WorkingDir: string
          Source: string
          Output: string
          ReportType: ReportType
          CustomParameters: string }

    let internal ReportDefaults: ReportParams =
        { ToolPath = toolPath "dotCover.exe"
          WorkingDir = ""
          Source = ""
          Output = "dotCoverReport.xml"
          ReportType = ReportType.Xml
          CustomParameters = "" }

    let internal buildArgs parameters =
        StringBuilder()
        |> StringBuilder.append "cover"
        |> StringBuilder.appendIfNotNullOrEmpty parameters.TargetExecutable "/TargetExecutable="
        |> StringBuilder.appendIfNotNullOrEmpty (parameters.TargetArguments.Trim()) "/TargetArguments="
        |> StringBuilder.appendIfNotNullOrEmpty parameters.TargetWorkingDir "/TargetWorkingDir="
        |> StringBuilder.appendIfNotNullOrEmpty parameters.Filters "/Filters="
        |> StringBuilder.appendIfNotNullOrEmpty parameters.AttributeFilters "/AttributeFilters="
        |> StringBuilder.appendIfNotNullOrEmpty parameters.Output "/Output="
        |> StringBuilder.appendWithoutQuotes parameters.CustomParameters
        |> StringBuilder.toText

    let internal buildMergeArgs (parameters: MergeParams) =
        StringBuilder()
        |> StringBuilder.append "merge"
        |> StringBuilder.appendIfNotNullOrEmpty (parameters.Source |> String.concat ";") "/Source="
        |> StringBuilder.appendIfNotNullOrEmpty parameters.Output "/Output="
        |> StringBuilder.appendIfNotNullOrEmpty parameters.TempDir "/TempDir="
        |> StringBuilder.appendWithoutQuotes parameters.CustomParameters
        |> StringBuilder.toText

    let internal buildReportArgs parameters =
        StringBuilder()
        |> StringBuilder.append "report"
        |> StringBuilder.appendIfNotNullOrEmpty parameters.Source "/Source="
        |> StringBuilder.appendIfNotNullOrEmpty parameters.Output "/Output="
        |> StringBuilder.appendIfNotNullOrEmpty (parameters.ReportType.ToString()) "/ReportType="
        |> StringBuilder.appendWithoutQuotes parameters.CustomParameters
        |> StringBuilder.toText

    let internal getWorkingDir workingDir =
        Seq.find
            String.isNotNullOrEmpty
            [ workingDir
              Fake.Core.Environment.environVar "teamcity.build.workingDir"
              "." ]
        |> Path.GetFullPath

    let internal buildParamsAndExecute parameters buildArguments toolPath workingDir failBuild =
        let args = buildArguments parameters
        Trace.trace (toolPath + " " + args)

        let processResult =
            CreateProcess.fromRawCommandLine toolPath args
            |> CreateProcess.withWorkingDirectory (getWorkingDir workingDir)
            |> CreateProcess.withTimeout TimeSpan.MaxValue
            |> Proc.run

        let ExitCodeForFailedTests = -3

        if (processResult.ExitCode = ExitCodeForFailedTests && not failBuild) then
            Trace.trace (sprintf "DotCover %s exited with error code %d" toolPath processResult.ExitCode)
        else if (processResult.ExitCode = ExitCodeForFailedTests && failBuild) then
            failwithf
                "Failing tests, use ErrorLevel.DontFailBuild to ignore failing tests. Exited %s with error code %d"
                toolPath
                processResult.ExitCode
        else if (processResult.ExitCode <> 0) then
            failwithf "Error running %s with exit code %d" toolPath processResult.ExitCode
        else
            Trace.trace (sprintf "DotCover exited successfully")

    /// Runs the dotCover "cover" command, using a target executable (such as NUnit or MSpec) and generates a snapshot file.
    ///
    /// ## Parameters
    ///  - `setParams` - Function used to overwrite the dotCover default parameters.
    ///
    /// ## Sample
    ///     DotCover.run (fun p -> { p with
    ///         TargetExecutable = "path to NUnit or MSpec"
    ///         WorkingDir ".
    ///         Output = artifactsDir @@ "dotCoverSnapshot.dcvr" })
    let run (setParams: Params -> Params) =
        let parameters = (Defaults |> setParams)

        buildParamsAndExecute
            parameters
            buildArgs
            parameters.ToolPath
            parameters.WorkingDir
            (parameters.ErrorLevel <> ErrorLevel.DontFailBuild)

    /// Runs the dotCover "merge" command. This combines dotCover snaphots into a single
    /// snapshot, enabling you to merge test coverage from multiple test running frameworks
    ///
    /// ## Parameters
    ///  - `setParams` - Function used to overwrite the dotCover merge default parameters.
    ///
    /// ## Sample
    ///     DotCover.merge (fun p -> { p with
    ///         Source = [artifactsDir @@ "NUnitDotCoverSnapshot.dcvr"
    ///         artifactsDir @@ "MSpecDotCoverSnapshot.dcvr"]
    ///         Output = artifactsDir @@ "dotCoverSnapshot.dcvr" })
    let merge (setParams: MergeParams -> MergeParams) =
        let parameters = (MergeDefaults |> setParams)
        buildParamsAndExecute parameters buildMergeArgs parameters.ToolPath parameters.WorkingDir true

    /// Runs the dotCover "report" command. This generates a report from a dotCover snapshot
    ///
    /// ## Parameters
    ///  - `setParams` - Function used to overwrite the dotCover report default parameters.
    ///
    /// ## Sample
    ///     DotCover.report (fun p -> { p with
    ///         Source = artifactsDir @@ "dotCoverSnapshot.dcvr"
    ///         Output = artifactsDir @@ "dotCoverReport.xml"
    ///         ReportType = ReportType.Xml })
    let report (setParams: ReportParams -> ReportParams) =
        let parameters = (ReportDefaults |> setParams)
        buildParamsAndExecute parameters buildReportArgs parameters.ToolPath parameters.WorkingDir true

    /// Runs the dotCover "cover" command against the NUnit test runner.
    ///
    /// ## Parameters
    ///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
    ///  - `setNUnitParams` - Function used to overwrite the NUnit default parameters.
    ///  - `assemblies` - The set of assemblies to run command on
    ///
    /// ## Sample
    ///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll")
    ///         |> DotCover.runNUnit
    ///             (fun dotCoverOptions -> { dotCoverOptions with
    ///                     Output = artifactsDir @@ "NUnitDotCoverSnapshot.dcvr" })
    ///             (fun nUnitOptions -> { nUnitOptions with
    ///                     DisableShadowCopy = true })
    let runNUnit
        (setDotCoverParams: Params -> Params)
        (setNUnitParams: NUnit.Common.NUnitParams -> NUnit.Common.NUnitParams)
        (assemblies: string seq)
        =
        let assemblies = assemblies |> Seq.toArray
        let details = assemblies |> String.separated ", "
        use __ = Trace.traceTask "DotCoverNUnit" details

        let parameters = NUnit.Common.NUnitDefaults |> setNUnitParams
        let args = NUnit.Common.buildArgs parameters assemblies

        run (fun p ->
            { p with
                TargetExecutable = parameters.ToolPath @@ parameters.ToolName
                TargetArguments = args }
            |> setDotCoverParams)

    /// Runs the dotCover "cover" command against the NUnit test runner.
    ///
    /// ## Parameters
    ///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
    ///  - `setNUnitParams` - Function used to overwrite the NUnit default parameters.
    ///  - `assemblies` - The set of assemblies to run command on
    ///
    /// ## Sample
    ///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll")
    ///         |> DotCover.runNUnit3
    ///             (fun dotCoverOptions -> { dotCoverOptions with
    ///                     Output = artifactsDir @@ "NUnit3DotCoverSnapshot.dcvr" })
    ///             (fun nUnit3Options -> { nUnit3Options with
    ///                     DisableShadowCopy = true })
    let runNUnit3
        (setDotCoverParams: Params -> Params)
        (setNUnitParams: NUnit3Params -> NUnit3Params)
        (assemblies: string seq)
        =
        let assemblies = assemblies |> Seq.toArray
        let details = assemblies |> String.separated ", "
        use __ = Trace.traceTask "DotCoverNUnit3" details

        let parameters = NUnit3Defaults |> setNUnitParams
        let args = NUnit3.buildArgs parameters assemblies

        run (fun p ->
            { p with
                TargetExecutable = parameters.ToolPath
                TargetArguments = args }
            |> setDotCoverParams)

    /// Runs the dotCover "cover" command against the XUnit2 test runner.
    ///
    /// ## Parameters
    ///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
    ///  - `setXUnit2Params` - Function used to overwrite the XUnit2 default parameters.
    ///  - `assemblies` - The set of assemblies to run command on
    ///
    /// ## Sample
    ///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll")
    ///         |> DotCover.runXUnit2
    ///             (fun  -> dotCoverOptions )
    ///             (fun nUnitOptions -> nUnitOptions)
    let runXUnit2
        (setDotCoverParams: Params -> Params)
        (setXUnit2Params: XUnit2Params -> XUnit2Params)
        (assemblies: string seq)
        =
        let assemblies = assemblies |> Seq.toArray
        let details = assemblies |> String.separated ", "
        use __ = Trace.traceTask "DotCoverXUnit2" details

        let parameters = XUnit2Defaults |> setXUnit2Params
        let args = XUnit2.buildArgs parameters assemblies

        run (fun p ->
            { p with
                TargetExecutable = parameters.ToolPath
                TargetArguments = args }
            |> setDotCoverParams)

    /// Builds the command line arguments from the given parameter record and the given assemblies.
    /// Runs all test assemblies in the same run for easier coverage management.
    /// [omit]
    let internal buildMSTestArgsForDotCover parameters assemblies =
        let testContainers =
            assemblies
            |> Array.map (fun a -> "/testcontainer:" + a)
            |> String.concat " "

        let testResultsFile =
            if parameters.ResultsDir <> null then
                sprintf @"%s\%s.trx" parameters.ResultsDir (DateTime.Now.ToString("yyyyMMdd-HHmmss.ff"))
            else
                null

        StringBuilder()
        |> StringBuilder.appendWithoutQuotesIfNotNull testContainers ""
        |> StringBuilder.appendWithoutQuotesIfNotNull parameters.Category "/category:"
        |> StringBuilder.appendWithoutQuotesIfNotNull parameters.TestMetadataPath "/testmetadata:"
        |> StringBuilder.appendWithoutQuotesIfNotNull parameters.TestSettingsPath "/testsettings:"
        |> StringBuilder.appendWithoutQuotesIfNotNull testResultsFile "/resultsfile:"
        |> StringBuilder.appendIfTrueWithoutQuotes parameters.NoIsolation "/noisolation"
        |> StringBuilder.toText

    /// Runs the dotCover "cover" command against the MSTest test runner.
    ///
    /// ## Parameters
    ///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
    ///  - `setMSTestParams` - Function used to overwrite the MSTest default parameters.
    ///  - `assemblies` - The set of assemblies to run command on
    ///
    /// ## Sample
    ///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll")
    ///         |> DotCover.runMSTest
    ///             (fun  -> dotCoverOptions )
    ///             (fun MSTestOptions -> MSTestOptions)
    let runMSTest
        (setDotCoverParams: Params -> Params)
        (setMSTestParams: MSTestParams -> MSTestParams)
        (assemblies: string seq)
        =
        let assemblies = assemblies |> Seq.toArray
        let details = assemblies |> String.separated ", "
        use __ = Trace.traceTask "DotCoverMSTest " details

        let parameters = MSTestDefaults |> setMSTestParams
        let args = buildMSTestArgsForDotCover parameters assemblies

        run (fun p ->
            { p with
                TargetExecutable = parameters.ToolPath
                TargetArguments = args }
            |> setDotCoverParams)

    /// Runs the dotCover "cover" command against the MSpec test runner.
    ///
    /// ## Parameters
    ///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
    ///  - `setMSpecParams` - Function used to overwrite the MSpec default parameters.
    ///  - `assemblies` - The set of assemblies to run command on
    ///
    /// ## Sample
    ///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll")
    ///         |> DotCover.runMSpec
    ///             (fun dotCoverOptions -> { dotCoverOptions with
    ///                     Output = artifactsDir @@ "MSpecDotCoverSnapshot.dcvr" })
    ///             (fun mSpecOptions -> { mSpecOptions with
    ///                     Silent = true })
    let runMSpec
        (setDotCoverParams: Params -> Params)
        (setMSpecParams: MSpecParams -> MSpecParams)
        (assemblies: string seq)
        =
        let assemblies = assemblies |> Seq.toArray
        let details = assemblies |> String.separated ", "
        use __ = Trace.traceTask "DotCoverMSpec" details

        let parameters = MSpecDefaults |> setMSpecParams

        let args = MSpec.buildArgs parameters assemblies

        run (fun p ->
            { p with
                TargetExecutable = parameters.ToolPath
                TargetArguments = args }
            |> setDotCoverParams)
