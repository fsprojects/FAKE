[<AutoOpen>]
/// Contains a task which can be used to run [DotCover](http://www.jetbrains.com/dotcover/) on .NET assemblies.
module Fake.DotCover

open Fake
open System
open System.IO
open System.Text

type DotCoverReportType = 
  | Html = 0
  | Json = 1
  | Xml = 2
  | NDependXml = 3

/// The DotCover parameter type for running coverage
type DotCoverParams = 
    { ToolPath: string
      WorkingDir: string
      TargetExecutable: string
      TargetArguments: string
      TargetWorkingDir: string
      Output: string
      Filters: string }

/// The DotCover defaeult parameters
let DotCoverDefaults = 
    { ToolPath = findToolInSubPath "dotcover.exe" (currentDirectory @@ "tools" @@ "DotCover")
      WorkingDir = ""
      TargetExecutable = ""
      TargetArguments = ""
      TargetWorkingDir = ""
      Output = "DotCover.snapshot"
      Filters = "" }

type DotCoverMergeParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string list
      Output: string
      TempDir: string }

let DotCoverMergeDefaults =
     { ToolPath = findToolInSubPath "dotcover.exe" (currentDirectory @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = []
       Output = "DotCover.snapshot"
       TempDir = "" }

type DotCoverReportParams = 
    { ToolPath: string
      WorkingDir: string
      Source: string
      Output: string
      ReportType: DotCoverReportType }
      
let DotCoverReportDefaults : DotCoverReportParams =
     { ToolPath = findToolInSubPath "dotcover.exe" (currentDirectory @@ "tools" @@ "DotCover")
       WorkingDir = ""
       Source = ""
       Output = "DotCover.xml"
       ReportType = DotCoverReportType.Xml }

let buildDotCoverArgs parameters =
    new StringBuilder()
    |> append "cover"
    |> appendIfNotNullOrEmpty parameters.TargetExecutable "/TargetExecutable="
    |> appendIfNotNullOrEmpty (parameters.TargetArguments.Trim()) "/TargetArguments="
    |> appendIfNotNullOrEmpty parameters.TargetWorkingDir "/TargetWorkingDir="
    |> appendIfNotNullOrEmpty parameters.Output "/Output="
    |> toText

let buildDotCoverMergeArgs (parameters:DotCoverMergeParams) =
    new StringBuilder()
    |> append "merge"
    |> appendIfNotNullOrEmpty (parameters.Source |> String.concat ";") "/Source="
    |> appendIfNotNullOrEmpty parameters.Output "/Output="
    |> appendIfNotNullOrEmpty parameters.TempDir "/TempDir="
    |> toText
    
let buildDotCoverReportArgs parameters =
    new StringBuilder()
    |> append "report"
    |> appendIfNotNullOrEmpty parameters.Source "/Source="
    |> appendIfNotNullOrEmpty parameters.Output "/Output="
    |> appendIfNotNullOrEmpty (parameters.ReportType.ToString()) "/ReportType="
    |> toText


let getWorkingDir workingDir =
    Seq.find isNotNullOrEmpty [workingDir; environVar("teamcity.build.workingDir"); "."]
    |> Path.GetFullPath

let buildParamsAndExecute parameters buildArguments toolPath workingDir =
    let args = buildArguments parameters
    trace (toolPath + " " + args)
    let result = ExecProcess (fun info ->  
              info.FileName <- toolPath
              info.WorkingDirectory <- getWorkingDir workingDir
              info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "Error running %s" toolPath

/// Runs the DotCover "cover" command, using a target executable (such as NUnit or MSpec)
/// and generates a snapshot file
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the DotCover default parameters.
let DotCover (setParams: DotCoverParams -> DotCoverParams) =
    let parameters = (DotCoverDefaults |> setParams)
    buildParamsAndExecute parameters buildDotCoverArgs parameters.ToolPath parameters.WorkingDir

/// Runs the DotCover "merge" command. This combines dotCover snaphots into a single
/// snapshot, enabling you to merge test coverage from multiple test running frameworks
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the DotCover merge default parameters.
/// ## Sample
///
///    DotCoverMerge (fun p -> { p with 
///                         Source = [artifactsDir @@ "NUnitDotCover.snapshot";artifactsDir @@ "MSpecDotCover.snapshot"]
///                         Output = artifactsDir @@ "DotCover.snapshot" }) 
let DotCoverMerge (setParams: DotCoverMergeParams -> DotCoverMergeParams) =
    let parameters = (DotCoverMergeDefaults |> setParams)
    buildParamsAndExecute parameters buildDotCoverMergeArgs parameters.ToolPath parameters.WorkingDir
   
/// Runs the DotCover "report" command. This generates a report from a DotCover snapshot
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the DotCover report default parameters.
/// ## Sample
///
///     DotCoverReport (fun p -> { p with 
///                         Source = artifactsDir @@ "DotCover.snapshot"
///                         Output = artifactsDir @@ "DotCover.xml"
///                         ReportType = DotCoverReportType.Xml })
let DotCoverReport (setParams: DotCoverReportParams -> DotCoverReportParams) =
    let parameters = (DotCoverReportDefaults |> setParams)
    buildParamsAndExecute parameters buildDotCoverReportArgs parameters.ToolPath parameters.WorkingDir

/// Runs the DotCover "cover" command against the NUnit test runner.
/// ## Parameters
///
///  - `setDotCoverParams` - Function used to overwrite the DotCover report default parameters.
///  - `setNUnitParams` - Function used to overwrite the NUnit default parameters.
/// ## Sample
///
///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll") 
///         |> DotCoverNUnit 
///             (fun dotCoverOptions -> { dotCoverOptions with 
///                     Output = artifactsDir @@ "NUnitDotCover.snapshot" }) 
///             (fun nUnitOptions -> { nUnitOptions with
///                     DisableShadowCopy = true })
let DotCoverNUnit (setDotCoverParams: DotCoverParams -> DotCoverParams) (setNUnitParams: NUnitParams -> NUnitParams) (assemblies: string seq) =
    let parameters = NUnitDefaults |> setNUnitParams            
    let assemblies = assemblies |> Seq.toArray
    let args = buildNUnitdArgs parameters assemblies
    
    DotCover (fun p ->
                  {p with
                     TargetExecutable = parameters.ToolPath @@ parameters.ToolName
                     TargetArguments = args
                  } |> setDotCoverParams)

/// Runs the DotCover "cover" command against the MSpec test runner.
/// ## Parameters
///
///  - `setDotCoverParams` - Function used to overwrite the DotCover report default parameters.
///  - `setMSpecParams` - Function used to overwrite the MSpec default parameters.
/// ## Sample
///
///     !! (buildDir @@ buildMode @@ "/*.Unit.Tests.dll") 
///         |> DotCoverMSpec 
///             (fun dotCoverOptions -> { dotCoverOptions with 
///                     Output = artifactsDir @@ "MSpecDotCover.snapshot" }) 
///             (fun mSpecOptions -> { mSpecOptions with
///                     Silent = true })
let DotCoverMSpec (setDotCoverParams: DotCoverParams -> DotCoverParams) (setMSpecParams: MSpecParams -> MSpecParams) (assemblies: string seq) =
    let parameters = MSpecDefaults |> setMSpecParams            
    let assemblies = assemblies |> Seq.toArray
    let args = buildMSpecArgs parameters assemblies
    
    DotCover (fun p ->
                  {p with
                     TargetExecutable = parameters.ToolPath
                     TargetArguments = args
                  } |> setDotCoverParams)