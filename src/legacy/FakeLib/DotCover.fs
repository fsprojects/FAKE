/// Contains a task which can be used to run [DotCover](http://www.jetbrains.com/dotcover/) on .NET assemblies.
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
module Fake.DotCover

open Fake
open System
open System.IO
open System.Text
open Fake.Testing.XUnit2
open Fake.Testing.NUnit3
open Fake.MSTest
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]

type DotCoverReportType = 
  | Html = 0
  | Json = 1
  | Xml = 2
  | NDependXml = 3

/// The dotCover parameter type for running coverage
[<CLIMutable>]
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
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

[<CLIMutable>]
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
type DotCoverMergeParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string list
      Output: string
      TempDir: string
      CustomParameters: string }

[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let DotCoverMergeDefaults =
     { ToolPath = findToolInSubPath "dotCover.exe" (currentDirectory @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = []
       Output = "dotCoverSnapshot.dcvr"
       TempDir = ""
       CustomParameters = "" }

[<CLIMutable>]
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
type DotCoverReportParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string
      Output: string
      ReportType: DotCoverReportType
      CustomParameters: string }

[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let DotCoverReportDefaults : DotCoverReportParams =
     { ToolPath = findToolInSubPath "dotCover.exe" (currentDirectory @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = ""
       Output = "dotCoverReport.xml"
       ReportType = DotCoverReportType.Xml
       CustomParameters = "" }

[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
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

[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let buildDotCoverMergeArgs (parameters:DotCoverMergeParams) =
    new StringBuilder()
    |> append "merge"
    |> appendIfNotNullOrEmpty (parameters.Source |> String.concat ";") "/Source="
    |> appendIfNotNullOrEmpty parameters.Output "/Output="
    |> appendIfNotNullOrEmpty parameters.TempDir "/TempDir="
    |> appendWithoutQuotes parameters.CustomParameters
    |> toText

[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let buildDotCoverReportArgs parameters =
    new StringBuilder()
    |> append "report"
    |> appendIfNotNullOrEmpty parameters.Source "/Source="
    |> appendIfNotNullOrEmpty parameters.Output "/Output="
    |> appendIfNotNullOrEmpty (parameters.ReportType.ToString()) "/ReportType="
    |> appendWithoutQuotes parameters.CustomParameters
    |> toText


[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let getWorkingDir workingDir =
    Seq.find isNotNullOrEmpty [workingDir; environVar("teamcity.build.workingDir"); "."]
    |> Path.GetFullPath

[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let DotCoverNUnit (setDotCoverParams: DotCoverParams -> DotCoverParams) (setNUnitParams: NUnitParams -> NUnitParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> separated ", "
    use __ = traceStartTaskUsing "DotCoverNUnit" details

    let parameters = NUnitDefaults |> setNUnitParams
    let args = buildNUnitdArgs parameters assemblies
    
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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let DotCoverNUnit3 (setDotCoverParams: DotCoverParams -> DotCoverParams) (setNUnitParams: NUnit3Params -> NUnit3Params) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> separated ", "
    use __ = traceStartTaskUsing "DotCoverNUnit3" details

    let parameters = NUnit3Defaults |> setNUnitParams
    let args = buildNUnit3Args parameters assemblies
    
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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let DotCoverXUnit2 (setDotCoverParams: DotCoverParams -> DotCoverParams) (setXUnit2Params: XUnit2Params -> XUnit2Params) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> separated ", "
    use __ = traceStartTaskUsing "DotCoverXUnit2" details

    let parameters = XUnit2Defaults |> setXUnit2Params
    let args = buildXUnit2Args assemblies parameters 
    
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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let DotCoverMSTest (setDotCoverParams: DotCoverParams -> DotCoverParams) (setMSTestParams: MSTestParams -> MSTestParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> separated ", "
    use __ = traceStartTaskUsing "DotCoverMSTest " details

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
[<System.Obsolete("FAKE0001 Use the Fake.DotNet.Testing.DotCover module instead")>]
let DotCoverMSpec (setDotCoverParams: DotCoverParams -> DotCoverParams) (setMSpecParams: MSpecParams -> MSpecParams) (assemblies: string seq) =
    let assemblies = assemblies |> Seq.toArray
    let details =  assemblies |> separated ", "
    use __ = traceStartTaskUsing "DotCoverMSpec" details

    let parameters = MSpecDefaults |> setMSpecParams            
   
    let args = buildMSpecArgs parameters assemblies
    
    DotCover (fun p ->
                    {p with
                        TargetExecutable = parameters.ToolPath
                        TargetArguments = args
                    } |> setDotCoverParams)
