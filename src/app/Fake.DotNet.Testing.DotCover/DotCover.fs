/// Contains a task which can be used to run [DotCover](http://www.jetbrains.com/dotcover/) on .NET assemblies.
[<RequireQualifiedAccess>]
module Fake.DotNet.Testing.DotCover

open System
open System.IO
open System.Text
open Fake
open Fake.Core
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing
open Fake.Testing.Common
open Fake.DotNet.Testing.MSTest
open Fake.DotNet.Testing.MSpec
open Fake.DotNet.Testing.NUnit3
open Fake.DotNet.Testing.XUnit2

type ReportType = 
  | Html = 0
  | Json = 1
  | Xml = 2
  | NDependXml = 3

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

/// The dotCover default parameters
let internal Defaults = 
    { ToolPath = Fake.IO.Globbing.Tools.findToolInSubPath "dotCover.exe" ((Path.GetFullPath ".") @@ "tools" @@ "DotCover")
      WorkingDir = ""
      TargetExecutable = ""
      TargetArguments = ""
      TargetWorkingDir = ""
      Filters = ""
      AttributeFilters = ""
      Output = "dotCoverSnapshot.dcvr"
      CustomParameters = "" 
      ErrorLevel = ErrorLevel.Error} 

type MergeParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string list
      Output: string
      TempDir: string
      CustomParameters: string }

let internal MergeDefaults =
     { ToolPath = Fake.IO.Globbing.Tools.findToolInSubPath "dotCover.exe" ((Path.GetFullPath ".") @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = []
       Output = "dotCoverSnapshot.dcvr"
       TempDir = ""
       CustomParameters = "" }

type ReportParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string
      Output: string
      ReportType: ReportType
      CustomParameters: string }

let internal ReportDefaults : ReportParams =
     { ToolPath = Fake.IO.Globbing.Tools.findToolInSubPath "dotCover.exe" ((Path.GetFullPath ".") @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = ""
       Output = "dotCoverReport.xml"
       ReportType = ReportType.Xml
       CustomParameters = "" }

let internal buildArgs parameters =
    new StringBuilder()
    |> StringBuilder.append "cover"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.TargetExecutable "/TargetExecutable="
    |> StringBuilder.appendIfNotNullOrEmpty (parameters.TargetArguments.Trim()) "/TargetArguments="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.TargetWorkingDir "/TargetWorkingDir="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Filters "/Filters="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.AttributeFilters "/AttributeFilters="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Output "/Output="
    |> StringBuilder.appendWithoutQuotes parameters.CustomParameters
    |> StringBuilder.toText

let internal buildMergeArgs (parameters:MergeParams) =
    new StringBuilder()
    |> StringBuilder.append "merge"
    |> StringBuilder.appendIfNotNullOrEmpty (parameters.Source |> String.concat ";") "/Source="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Output "/Output="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.TempDir "/TempDir="
    |> StringBuilder.appendWithoutQuotes parameters.CustomParameters
    |> StringBuilder.toText

let internal buildReportArgs parameters =
    new StringBuilder()
    |> StringBuilder.append "report"
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Source "/Source="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Output "/Output="
    |> StringBuilder.appendIfNotNullOrEmpty (parameters.ReportType.ToString()) "/ReportType="
    |> StringBuilder.appendWithoutQuotes parameters.CustomParameters
    |> StringBuilder.toText


let internal getWorkingDir workingDir =
    Seq.find String.isNotNullOrEmpty [workingDir; Fake.Core.Environment.environVar("teamcity.build.workingDir"); "."]
    |> Path.GetFullPath

let internal buildParamsAndExecute parameters buildArguments toolPath workingDir failBuild =
    let args = buildArguments parameters
    Trace.trace (toolPath + " " + args)
    let result = Fake.Core.Process.execSimple (fun info ->  
                                        {info with
                                            FileName = toolPath
                                            WorkingDirectory = getWorkingDir workingDir
                                            Arguments = args
                                        })
                                        TimeSpan.MaxValue
    let ExitCodeForFailedTests = -3
    if (result = ExitCodeForFailedTests && not failBuild) then 
        Trace.trace (sprintf "DotCover %s exited with errorcode %d" toolPath result)
    else if (result = ExitCodeForFailedTests && failBuild) then 
        failwithf "Failing tests, use ErrorLevel.DontFailBuild to ignore failing tests. Exited %s with errorcode %d" toolPath result
    else if (result <> 0) then 
        failwithf "Error running %s with exitcode %d" toolPath result
    else 
        Trace.trace (sprintf "DotCover exited successfully")

/// Runs the dotCover "cover" command, using a target executable (such as NUnit or MSpec) and generates a snapshot file.
///
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the dotCover default parameters.
let run (setParams: Params -> Params) =
    let parameters = (Defaults |> setParams)
    buildParamsAndExecute parameters buildArgs parameters.ToolPath parameters.WorkingDir (parameters.ErrorLevel <> ErrorLevel.DontFailBuild)

/// Runs the dotCover "merge" command. This combines dotCover snaphots into a single
/// snapshot, enabling you to merge test coverage from multiple test running frameworks
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the dotCover merge default parameters.
///
/// ## Sample
///
///     merge (fun p -> { p with 
///                         Source = [artifactsDir @@ "NUnitDotCoverSnapshot.dcvr"
///                                   artifactsDir @@ "MSpecDotCoverSnapshot.dcvr"]
///                         Output = artifactsDir @@ "dotCoverSnapshot.dcvr" }) 
let merge (setParams: MergeParams -> MergeParams) =
    let parameters = (MergeDefaults |> setParams)
    buildParamsAndExecute parameters buildMergeArgs parameters.ToolPath parameters.WorkingDir true 
   
/// Runs the dotCover "report" command. This generates a report from a dotCover snapshot
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the dotCover report default parameters.
///
/// ## Sample
///
///     report (fun p -> { p with 
///                         Source = artifactsDir @@ "dotCoverSnapshot.dcvr"
///                         Output = artifactsDir @@ "dotCoverReport.xml"
///                         ReportType = ReportType.Xml })
let report (setParams: ReportParams -> ReportParams) =
    let parameters = (ReportDefaults |> setParams)
    buildParamsAndExecute parameters buildReportArgs parameters.ToolPath parameters.WorkingDir true

/// Runs the dotCover "cover" command against the NUnit test runner.
/// ## Parameters
///
///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
///  - `setNUnitParams` - Function used to overwrite the NUnit default parameters.
///
/// ## Sample
///
///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll") 
///         |> runNUnit 
///             (fun dotCoverOptions -> { dotCoverOptions with 
///                     Output = artifactsDir @@ "NUnitDotCoverSnapshot.dcvr" }) 
///             (fun nUnitOptions -> { nUnitOptions with
///                     DisableShadowCopy = true })
let runNUnit (setDotCoverParams: Params -> Params) (setNUnitParams: NUnit.Common.NUnitParams -> NUnit.Common.NUnitParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverNUnit" details

    let parameters = NUnit.Common.NUnitDefaults |> setNUnitParams
    let args = NUnit.Common.buildArgs parameters assemblies
    
    run (fun p ->
                    {p with
                        TargetExecutable = parameters.ToolPath @@ parameters.ToolName
                        TargetArguments = args
                    } |> setDotCoverParams)

/// Runs the dotCover "cover" command against the NUnit test runner.
/// ## Parameters
///
///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
///  - `setNUnitParams` - Function used to overwrite the NUnit default parameters.
///
/// ## Sample
///
///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll") 
///         |> runNUnit3 
///             (fun dotCoverOptions -> { dotCoverOptions with 
///                     Output = artifactsDir @@ "NUnit3DotCoverSnapshot.dcvr" }) 
///             (fun nUnit3Options -> { nUnit3Options with
///                     DisableShadowCopy = true })
let runNUnit3 (setDotCoverParams: Params -> Params) (setNUnitParams: NUnit3Params -> NUnit3Params) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverNUnit3" details

    let parameters = NUnit3Defaults |> setNUnitParams
    let args = NUnit3.buildArgs parameters assemblies
    
    run (fun p ->
                    {p with
                        TargetExecutable = parameters.ToolPath
                        TargetArguments = args
                    } |> setDotCoverParams)

/// Runs the dotCover "cover" command against the XUnit2 test runner.
/// ## Parameters
///
///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
///  - `setXUnit2Params` - Function used to overwrite the XUnit2 default parameters.
///
/// ## Sample
///
///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll") 
///         |> runXUnit2 
///             (fun  -> dotCoverOptions )
///             (fun nUnitOptions -> nUnitOptions) 
let runXUnit2 (setDotCoverParams: Params -> Params) (setXUnit2Params: XUnit2.XUnit2Params -> XUnit2.XUnit2Params) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverXUnit2" details

    let parameters = XUnit2.XUnit2Defaults |> setXUnit2Params
    let args = XUnit2.buildArgs parameters assemblies
    
    run (fun p ->
                    {p with
                        TargetExecutable = parameters.ToolPath 
                        TargetArguments = args
                    } |> setDotCoverParams)

/// Builds the command line arguments from the given parameter record and the given assemblies.
/// Runs all test assemblies in the same run for easier coverage management. 
/// [omit]
let internal buildMSTestArgsForDotCover parameters assemblies = 
    let testcontainers = assemblies |>  Array.map (fun a -> "/testcontainer:" + a) |> String.concat " "

    let testResultsFile = 
        if parameters.ResultsDir <> null then 
            sprintf @"%s\%s.trx" parameters.ResultsDir (DateTime.Now.ToString("yyyyMMdd-HHmmss.ff"))
        else null
    new StringBuilder()
    |> StringBuilder.appendWithoutQuotesIfNotNull testcontainers ""
    |> StringBuilder.appendWithoutQuotesIfNotNull parameters.Category "/category:"
    |> StringBuilder.appendWithoutQuotesIfNotNull parameters.TestMetadataPath "/testmetadata:"
    |> StringBuilder.appendWithoutQuotesIfNotNull parameters.TestSettingsPath "/testsettings:"
    |> StringBuilder.appendWithoutQuotesIfNotNull testResultsFile "/resultsfile:"
    |> StringBuilder.appendIfTrueWithoutQuotes parameters.NoIsolation "/noisolation"
    |> StringBuilder.toText

/// Runs the dotCover "cover" command against the MSTest test runner.
/// ## Parameters
///
///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
///  - `setMSTestParams` - Function used to overwrite the MSTest default parameters.
///
/// ## Sample
///
///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll") 
///         |> runMSTest 
///             (fun  -> dotCoverOptions )
///             (fun MSTestOptions -> MSTestOptions) 
let runMSTest (setDotCoverParams: Params -> Params) (setMSTestParams: MSTestParams -> MSTestParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverMSTest " details

    let parameters = MSTestDefaults |> setMSTestParams
    let args = buildMSTestArgsForDotCover parameters assemblies
    
    run (fun p ->
                    {p with
                        TargetExecutable = parameters.ToolPath 
                        TargetArguments = args
                    } |> setDotCoverParams)

/// Runs the dotCover "cover" command against the MSpec test runner.
/// ## Parameters
///
///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
///  - `setMSpecParams` - Function used to overwrite the MSpec default parameters.
///
/// ## Sample
///
///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll") 
///         |> runMSpec 
///             (fun dotCoverOptions -> { dotCoverOptions with 
///                     Output = artifactsDir @@ "MSpecDotCoverSnapshot.dcvr" }) 
///             (fun mSpecOptions -> { mSpecOptions with
///                     Silent = true })
let runMSpec (setDotCoverParams: Params -> Params) (setMSpecParams: MSpecParams -> MSpecParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverMSpec" details

    let parameters = MSpecDefaults |> setMSpecParams            
   
    let args = MSpec.buildArgs parameters assemblies
    
    run (fun p ->
                    {p with
                        TargetExecutable = parameters.ToolPath
                        TargetArguments = args
                    } |> setDotCoverParams)
