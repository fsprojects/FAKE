/// Contains a task which can be used to run [DotCover](http://www.jetbrains.com/dotcover/) on .NET assemblies.
module Fake.DotCover

open Fake
open System
open System.IO
open System.Text
open Fake.Testing.XUnit2
open Fake.Testing.NUnit3
open Fake.MSTest

type DotCoverReportType = 
  | Html = 0
  | Json = 1
  | Xml = 2
  | NDependXml = 3

/// The dotCover parameter type for running coverage
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
let DotCoverDefaults = 
    { ToolPath = findToolInSubPath "dotCover.exe" (currentDirectory @@ "tools" @@ "DotCover")
      WorkingDir = ""
      TargetExecutable = ""
      TargetArguments = ""
      TargetWorkingDir = ""
      Filters = ""
      AttributeFilters = ""
      Output = "dotCoverSnapshot.dcvr"
      CustomParameters = "" 
      ErrorLevel = ErrorLevel.Error} 

type DotCoverMergeParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string list
      Output: string
      TempDir: string
      CustomParameters: string }

let DotCoverMergeDefaults =
     { ToolPath = findToolInSubPath "dotCover.exe" (currentDirectory @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = []
       Output = "dotCoverSnapshot.dcvr"
       TempDir = ""
       CustomParameters = "" }

type DotCoverReportParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string
      Output: string
      ReportType: DotCoverReportType
      CustomParameters: string }
      
let DotCoverReportDefaults : DotCoverReportParams =
     { ToolPath = findToolInSubPath "dotCover.exe" (currentDirectory @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = ""
       Output = "dotCoverReport.xml"
       ReportType = DotCoverReportType.Xml
       CustomParameters = "" }

let buildDotCoverArgs parameters =
    new StringBuilder()
    |> append "cover"
    |> appendIfNotNullOrEmpty parameters.TargetExecutable "/TargetExecutable="
    |> appendIfNotNullOrEmpty (parameters.TargetArguments.Trim()) "/TargetArguments="
    |> appendIfNotNullOrEmpty parameters.TargetWorkingDir "/TargetWorkingDir="
    |> appendIfNotNullOrEmpty parameters.Filters "/Filters="
    |> appendIfNotNullOrEmpty parameters.AttributeFilters "/AttributeFilters="
    |> appendIfNotNullOrEmpty parameters.Output "/Output="
    |> appendWithoutQuotes parameters.CustomParameters
    |> toText

let buildDotCoverMergeArgs (parameters:DotCoverMergeParams) =
    new StringBuilder()
    |> append "merge"
    |> appendIfNotNullOrEmpty (parameters.Source |> String.concat ";") "/Source="
    |> appendIfNotNullOrEmpty parameters.Output "/Output="
    |> appendIfNotNullOrEmpty parameters.TempDir "/TempDir="
    |> appendWithoutQuotes parameters.CustomParameters
    |> toText
    
let buildDotCoverReportArgs parameters =
    new StringBuilder()
    |> append "report"
    |> appendIfNotNullOrEmpty parameters.Source "/Source="
    |> appendIfNotNullOrEmpty parameters.Output "/Output="
    |> appendIfNotNullOrEmpty (parameters.ReportType.ToString()) "/ReportType="
    |> appendWithoutQuotes parameters.CustomParameters
    |> toText


let getWorkingDir workingDir =
    Seq.find isNotNullOrEmpty [workingDir; environVar("teamcity.build.workingDir"); "."]
    |> Path.GetFullPath

let buildParamsAndExecute parameters buildArguments toolPath workingDir failBuild =
    let args = buildArguments parameters
    trace (toolPath + " " + args)
    let result = ExecProcess (fun info ->  
              info.FileName <- toolPath
              info.WorkingDirectory <- getWorkingDir workingDir
              info.Arguments <- args) TimeSpan.MaxValue
    let ExitCodeForFailedTests = -3
    if (result = ExitCodeForFailedTests && not failBuild) then 
        trace (sprintf "DotCover %s exited with errorcode %d" toolPath result)
    else if (result = ExitCodeForFailedTests && failBuild) then 
        failwithf "Failing tests, use ErrorLevel.DontFailBuild to ignore failing tests. Exited %s with errorcode %d" toolPath result
    else if (result <> 0) then 
        failwithf "Error running %s with exitcode %d" toolPath result
    else 
        trace (sprintf "DotCover exited successfully")

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
let DotCoverNUnit (setDotCoverParams: DotCoverParams -> DotCoverParams) (setNUnitParams: NUnitParams -> NUnitParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> separated ", "
    traceStartTask "DotCoverNUnit" details

    try
        let parameters = NUnitDefaults |> setNUnitParams
        let args = buildNUnitdArgs parameters assemblies
    
        DotCover (fun p ->
                      {p with
                         TargetExecutable = parameters.ToolPath @@ parameters.ToolName
                         TargetArguments = args
                      } |> setDotCoverParams)
    finally
        traceEndTask "DotCoverNUnit" details

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
    let details =  assemblies |> separated ", "
    traceStartTask "DotCoverNUnit3" details

    try
        let parameters = NUnit3Defaults |> setNUnitParams
        let args = buildNUnit3Args parameters assemblies
    
        DotCover (fun p ->
                      {p with
                         TargetExecutable = parameters.ToolPath
                         TargetArguments = args
                      } |> setDotCoverParams)
    finally
        traceEndTask "DotCoverNUnit3" details

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
let DotCoverXUnit2 (setDotCoverParams: DotCoverParams -> DotCoverParams) (setXUnit2Params: XUnit2Params -> XUnit2Params) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> separated ", "
    traceStartTask "DotCoverXUnit2" details

    try
        let parameters = XUnit2Defaults |> setXUnit2Params
        let args = buildXUnit2Args assemblies parameters 
    
        DotCover (fun p ->
                      {p with
                         TargetExecutable = parameters.ToolPath 
                         TargetArguments = args
                      } |> setDotCoverParams)
    finally
        traceEndTask "DotCoverXUnit2" details

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
    |> appendWithoutQuotesIfNotNull testcontainers ""
    |> appendWithoutQuotesIfNotNull parameters.Category "/category:"
    |> appendWithoutQuotesIfNotNull parameters.TestMetadataPath "/testmetadata:"
    |> appendWithoutQuotesIfNotNull parameters.TestSettingsPath "/testsettings:"
    |> appendWithoutQuotesIfNotNull testResultsFile "/resultsfile:"
    |> appendIfTrueWithoutQuotes parameters.NoIsolation "/noisolation"
    |> toText

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
    let details =  assemblies |> separated ", "
    traceStartTask "DotCoverMSTest " details

    try
        let parameters = MSTestDefaults |> setMSTestParams
        let args = buildMSTestArgsForDotCover parameters assemblies
    
        DotCover (fun p ->
                      {p with
                         TargetExecutable = parameters.ToolPath 
                         TargetArguments = args
                      } |> setDotCoverParams)
    finally
        traceEndTask "DotCoverMSTest" details

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
    let details =  assemblies |> separated ", "
    traceStartTask "DotCoverMSpec" details

    try
        let parameters = MSpecDefaults |> setMSpecParams            
   
        let args = buildMSpecArgs parameters assemblies
    
        DotCover (fun p ->
                      {p with
                         TargetExecutable = parameters.ToolPath
                         TargetArguments = args
                      } |> setDotCoverParams)
    finally
        traceEndTask "DotCoverMSpec" details