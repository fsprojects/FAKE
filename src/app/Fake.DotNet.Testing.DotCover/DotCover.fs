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

type DotCoverReportType = 
  | Html = 0
  | Json = 1
  | Xml = 2
  | NDependXml = 3

/// The dotCover parameter type for running coverage
[<CLIMutable>]
type DotCoverParams = 
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
let internal DotCoverDefaults = 
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

[<CLIMutable>]
type DotCoverMergeParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string list
      Output: string
      TempDir: string
      CustomParameters: string }

let internal DotCoverMergeDefaults =
     { ToolPath = Fake.IO.Globbing.Tools.findToolInSubPath "dotCover.exe" ((Path.GetFullPath ".") @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = []
       Output = "dotCoverSnapshot.dcvr"
       TempDir = ""
       CustomParameters = "" }

[<CLIMutable>]
type DotCoverReportParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string
      Output: string
      ReportType: DotCoverReportType
      CustomParameters: string }

let internal DotCoverReportDefaults : DotCoverReportParams =
     { ToolPath = Fake.IO.Globbing.Tools.findToolInSubPath "dotCover.exe" ((Path.GetFullPath ".") @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = ""
       Output = "dotCoverReport.xml"
       ReportType = DotCoverReportType.Xml
       CustomParameters = "" }

let internal buildDotCoverArgs parameters =
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

let internal buildDotCoverMergeArgs (parameters:DotCoverMergeParams) =
    new StringBuilder()
    |> StringBuilder.append "merge"
    |> StringBuilder.appendIfNotNullOrEmpty (parameters.Source |> String.concat ";") "/Source="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.Output "/Output="
    |> StringBuilder.appendIfNotNullOrEmpty parameters.TempDir "/TempDir="
    |> StringBuilder.appendWithoutQuotes parameters.CustomParameters
    |> StringBuilder.toText

let internal buildDotCoverReportArgs parameters =
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
let DotCover (setParams: DotCoverParams -> DotCoverParams) =
    let parameters = (DotCoverDefaults |> setParams)
    buildParamsAndExecute parameters buildDotCoverArgs parameters.ToolPath parameters.WorkingDir (parameters.ErrorLevel <> ErrorLevel.DontFailBuild)

/// Runs the dotCover "merge" command. This combines dotCover snaphots into a single
/// snapshot, enabling you to merge test coverage from multiple test running frameworks
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the dotCover merge default parameters.
///
/// ## Sample
///
///     DotCoverMerge (fun p -> { p with 
///                         Source = [artifactsDir @@ "NUnitDotCoverSnapshot.dcvr"
///                                   artifactsDir @@ "MSpecDotCoverSnapshot.dcvr"]
///                         Output = artifactsDir @@ "dotCoverSnapshot.dcvr" }) 
let DotCoverMerge (setParams: DotCoverMergeParams -> DotCoverMergeParams) =
    let parameters = (DotCoverMergeDefaults |> setParams)
    buildParamsAndExecute parameters buildDotCoverMergeArgs parameters.ToolPath parameters.WorkingDir false 
   
/// Runs the dotCover "report" command. This generates a report from a dotCover snapshot
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the dotCover report default parameters.
///
/// ## Sample
///
///     DotCoverReport (fun p -> { p with 
///                         Source = artifactsDir @@ "dotCoverSnapshot.dcvr"
///                         Output = artifactsDir @@ "dotCoverReport.xml"
///                         ReportType = DotCoverReportType.Xml })
let DotCoverReport (setParams: DotCoverReportParams -> DotCoverReportParams) =
    let parameters = (DotCoverReportDefaults |> setParams)
    buildParamsAndExecute parameters buildDotCoverReportArgs parameters.ToolPath parameters.WorkingDir

/// Runs the dotCover "cover" command against the NUnit test runner.
/// ## Parameters
///
///  - `setDotCoverParams` - Function used to overwrite the dotCover report default parameters.
///  - `setNUnitParams` - Function used to overwrite the NUnit default parameters.
///
/// ## Sample
///
///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll") 
///         |> DotCoverNUnit 
///             (fun dotCoverOptions -> { dotCoverOptions with 
///                     Output = artifactsDir @@ "NUnitDotCoverSnapshot.dcvr" }) 
///             (fun nUnitOptions -> { nUnitOptions with
///                     DisableShadowCopy = true })
let DotCoverNUnit (setDotCoverParams: DotCoverParams -> DotCoverParams) (setNUnitParams: NUnit.Common.NUnitParams -> NUnit.Common.NUnitParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverNUnit" details

    let parameters = NUnit.Common.NUnitDefaults |> setNUnitParams
    let args = NUnit.Common.buildNUnitdArgs parameters assemblies
    
    DotCover (fun p ->
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
///         |> DotCoverNUnit 
///             (fun dotCoverOptions -> { dotCoverOptions with 
///                     Output = artifactsDir @@ "NUnitDotCoverSnapshot.dcvr" }) 
///             (fun nUnitOptions -> { nUnitOptions with
///                     DisableShadowCopy = true })
let DotCoverNUnit3 (setDotCoverParams: DotCoverParams -> DotCoverParams) (setNUnitParams: NUnit3Params -> NUnit3Params) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverNUnit3" details

    let parameters = NUnit3Defaults |> setNUnitParams
    let args = NUnit3.buildNUnit3Args parameters assemblies
    
    DotCover (fun p ->
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
///         |> DotCoverXUnit2 
///             (fun  -> dotCoverOptions )
///             (fun nUnitOptions -> nUnitOptions) 
let DotCoverXUnit2 (setDotCoverParams: DotCoverParams -> DotCoverParams) (setXUnit2Params: XUnit2.XUnit2Params -> XUnit2.XUnit2Params) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverXUnit2" details

    let parameters = XUnit2.XUnit2Defaults |> setXUnit2Params
    let args = XUnit2.buildXUnit2Args assemblies parameters 
    
    DotCover (fun p ->
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
///         |> MSTest 
///             (fun  -> dotCoverOptions )
///             (fun MSTestOptions -> MSTestOptions) 
let DotCoverMSTest (setDotCoverParams: DotCoverParams -> DotCoverParams) (setMSTestParams: MSTestParams -> MSTestParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverMSTest " details

    let parameters = MSTestDefaults |> setMSTestParams
    let args = buildMSTestArgsForDotCover parameters assemblies
    
    DotCover (fun p ->
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
///         |> DotCoverMSpec 
///             (fun dotCoverOptions -> { dotCoverOptions with 
///                     Output = artifactsDir @@ "MSpecDotCoverSnapshot.dcvr" }) 
///             (fun mSpecOptions -> { mSpecOptions with
///                     Silent = true })
let DotCoverMSpec (setDotCoverParams: DotCoverParams -> DotCoverParams) (setMSpecParams: MSpecParams -> MSpecParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> String.separated ", "
    use __ = Trace.traceTask "DotCoverMSpec" details

    let parameters = MSpecDefaults |> setMSpecParams            
   
    let args = MSpec.buildMSpecArgs parameters assemblies
    
    DotCover (fun p ->
                    {p with
                        TargetExecutable = parameters.ToolPath
                        TargetArguments = args
                    } |> setDotCoverParams)
