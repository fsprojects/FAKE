/// Contains support for various build servers
namespace Fake.BuildServer

open System
open Fake.Core

[<AutoOpen>]
module TeamCityImportExtensions =
    type DotNetCoverageTool with
        member x.TeamCityName =
            match x with
            | DotNetCoverageTool.DotCover -> "dotcover"
            | DotNetCoverageTool.PartCover -> "partcover"
            | DotNetCoverageTool.NCover -> "ncover"
            | DotNetCoverageTool.NCover3 -> "ncover3"

    type ImportData with
        member x.TeamCityName =
            match x with
            | ImportData.BuildArtifactWithName _
            | ImportData.BuildArtifact -> "buildArtifact"
            | ImportData.DotNetCoverage _ -> "dotNetCoverage"
            | ImportData.DotNetDupFinder -> "DotNetDupFinder"
            | ImportData.PmdCpd -> "pmdCpd"
            | ImportData.Pmd -> "pmd"
            | ImportData.ReSharperInspectCode -> "ReSharperInspectCode"
            | ImportData.Jslint -> "jslint"
            | ImportData.FindBugs -> "findBugs"
            | ImportData.Checkstyle -> "checkstyle"
            | ImportData.Gtest -> "gtest"
            | ImportData.Mstest -> "mstest"
            | ImportData.Surefire -> "surefire"
            | ImportData.Junit -> "junit"
            | ImportData.FxCop -> "FxCop"
            | ImportData.Nunit _ -> "nunit"
            | ImportData.Xunit _ -> "nunit"

/// native support for TeamCity specific APIs.
/// The general documentation on how to use CI server integration can be found [here](/buildserver.html).
/// This module does not provide any special APIs please use FAKE APIs and they should integrate into this CI server.
/// If some integration is not working as expected or you have features you would like to use directly please open an issue. 
/// 
/// For more information on TeamCity interaction from build scripts [see here](https://confluence.jetbrains.com/display/TCD18/Build+Script+Interaction+with+TeamCity)
[<RequireQualifiedAccess>]
module TeamCity =
    open Fake.IO

    

    /// Open Named Block that will be closed when the block is disposed
    /// Usage: `use __ = TeamCity.block "My Block"`
    let block name description =
        TeamCityWriter.sendOpenBlock name description
        { new System.IDisposable
            with member __.Dispose() = TeamCityWriter.sendCloseBlock name }

    /// Sends an error to TeamCity
    let sendTeamCityError error = TeamCityWriter.sendBuildStatus "FAILURE" error

    /// Reports build problem
    let reportBuildProblem message = TeamCityWriter.sendBuildProblem message

    module internal Import =
        /// Sends an NUnit results filename to TeamCity
        let sendNUnit path = TeamCityWriter.sendImportData "nunit" path

        /// Sends an FXCop results filename to TeamCity
        let sendFXCop path = TeamCityWriter.sendImportData "FxCop" path

        /// Sends an JUnit Ant task results filename to TeamCity
        let sendJUnit path = TeamCityWriter.sendImportData "junit" path

        /// Sends an Maven Surefire results filename to TeamCity
        let sendSurefire path = TeamCityWriter.sendImportData "surefire" path

        /// Sends an MSTest results filename to TeamCity
        let sendMSTest path = TeamCityWriter.sendImportData "mstest" path

        /// Sends an Google Test results filename to TeamCity
        let sendGTest path = TeamCityWriter.sendImportData "gtest" path

        /// Sends an Checkstyle results filename to TeamCity
        let sendCheckstyle path = TeamCityWriter.sendImportData "checkstyle" path

        /// Sends an FindBugs results filename to TeamCity
        let sendFindBugs path = TeamCityWriter.sendImportData "findBugs" path

        /// Sends an JSLint results filename to TeamCity
        let sendJSLint path = TeamCityWriter.sendImportData "jslint" path

        /// Sends an ReSharper inspectCode.exe results filename to TeamCity
        let sendReSharperInspectCode path = TeamCityWriter.sendImportData "ReSharperInspectCode" path

        /// Sends an PMD inspections results filename to TeamCity
        let sendPmd path = TeamCityWriter.sendImportData "pmd" path

        /// Sends an PMD Copy/Paste Detector results filename to TeamCity
        let sendPmdCpd path = TeamCityWriter.sendImportData "pmdCpd" path

        /// Sends an ReSharper dupfinder.exe results filename to TeamCity
        let sendDotNetDupFinder path = TeamCityWriter.sendImportData "DotNetDupFinder" path

        /// Sends an dotcover, partcover, ncover or ncover3 results filename to TeamCity
        let sendDotNetCoverageForTool path (tool : DotNetCoverageTool) = TeamCityWriter.sendImportDataWithTool "dotNetCoverage" tool.TeamCityName path

    /// Sends the full path to the dotCover home folder to override the bundled dotCover to TeamCity
    let internal sendTeamCityDotCoverHome = TeamCityWriter.sendDotNetCoverage "dotcover_home"

    /// Sends the full path to NCover installation folder to TeamCity
    let internal sendTeamCityNCover3Home = TeamCityWriter.sendDotNetCoverage "ncover3_home"

    /// Sends arguments for the NCover report generator to TeamCity
    let internal sendTeamCityNCover3ReporterArgs = TeamCityWriter.sendDotNetCoverage "ncover3_reporter_args"

    /// Sends the path to NCoverExplorer to TeamCity
    let internal sendTeamCityNCoverExplorerTool = TeamCityWriter.sendDotNetCoverage "ncover_explorer_tool"

    /// Sends additional arguments for NCover 1.x to TeamCity
    let internal sendTeamCityNCoverExplorerToolArgs = TeamCityWriter.sendDotNetCoverage "ncover_explorer_tool_args"

    /// Sends the value for NCover /report: argument to TeamCity
    let internal sendTeamCityNCoverReportType : int -> unit = string >> TeamCityWriter.sendDotNetCoverage "ncover_explorer_report_type"

    /// Sends the value for NCover  /sort: argument to TeamCity
    let internal sendTeamCityNCoverReportOrder : int -> unit = string >> TeamCityWriter.sendDotNetCoverage "ncover_explorer_report_order"

    /// Send the PartCover xslt transformation rules (Input xlst and output files) to TeamCity
    let internal sendTeamCityPartCoverReportXslts : seq<string * string> -> unit =
        Seq.map (fun (xslt, output) -> sprintf "%s=>%s" xslt output)
        >> String.concat "\n"
        >> TeamCityWriter.sendDotNetCoverage "partcover_report_xslts"

    /// Starts the test case.
    let internal startTestCase testCaseName = TeamCityWriter.sendTestStarted testCaseName

    /// Finishes the test case.
    let internal finishTestCase testCaseName (duration : System.TimeSpan) = 
        TeamCityWriter.sendTestFinished testCaseName (duration.TotalMilliseconds |> round |> string)

    /// Ignores the test case.
    let internal ignoreTestCase name message =
        TeamCityWriter.sendTestStarted name
        TeamCityWriter.sendTestIgnored name message

    /// Report Standard-Output for a given test-case
    let internal reportTestOutput name output = TeamCityWriter.sendTestStdOut name output

    /// Report Standard-Error for a given test-case
    let internal reportTestError name output = TeamCityWriter.sendTestStdError name output

    /// Ignores the test case.
    let internal ignoreTestCaseWithDetails name message details =
        TeamCityWriter.sendTestStarted name
        TeamCityWriter.sendTestIgnored name (message + " " + details)

    /// Finishes the test suite.
    let internal finishTestSuite testSuiteName = TeamCityWriter.sendTestSuiteFinished testSuiteName

    /// Starts the test suite.
    let internal startTestSuite testSuiteName = TeamCityWriter.sendTestSuiteStarted testSuiteName

    /// Reports the progress.
    let reportProgress message = TeamCityWriter.sendProgressMessage message

    /// Reports the progress start.
    let reportProgressStart message = TeamCityWriter.sendProgressStart message

    /// Reports the progress end.
    let reportProgressFinish message = TeamCityWriter.sendProgressFinish message

    /// Reports the build status.
    let reportBuildStatus status message = TeamCityWriter.sendBuildStatus status message

    /// Publishes an artifact on the TeamcCity build server.
    let internal publishArtifact path = TeamCityWriter.sendPublishArtifact path

    /// Publishes an artifact on the TeamcCity build server with a name.
    let internal publishNamedArtifact name path = TeamCityWriter.sendPublishNamedArtifact name path

    /// Sets the TeamCity build number.
    let internal setBuildNumber buildNumber = TeamCityWriter.sendBuildNumber buildNumber

    /// Reports a build statistic.
    let setBuildStatistic key value = TeamCityWriter.sendBuildStatistic key value

    /// Reports a parameter value
    let setParameter name value = TeamCityWriter.sendSetParameter name value
       
    /// Reports a failed test
    let internal testFailed name message details = TeamCityWriter.sendTestFailed name message details

    /// Reports a failed comparison
    let internal comparisonFailure name message details expected actual = TeamCityWriter.sendComparisonFailed name message details expected actual

    /// Sends a warning message.
    let internal warning message = TeamCityWriter.sendMessage "WARNING" message

    /// Sends an error message.
    let internal error message = TeamCityWriter.sendMessage "ERROR" message

    /// TeamCity build parameters
    ///
    /// See [Predefined Build Parameters documentation](https://confluence.jetbrains.com/display/TCD18/Predefined+Build+Parameters) for more information
    type BuildParameters =
        /// Get all system build parameters (Without the 'system.' prefix)
        static member System
            with get() = TeamCityBuildParameters.getAllSystem()

        /// Get all configuration build parameters
        static member Configuration
            with get() = TeamCityBuildParameters.getAllConfiguration()

        /// Get all runner build parameters
        static member Runner
            with get() = TeamCityBuildParameters.getAllRunner()

        /// Get all build parameters
        ///
        /// System ones are prefixed with 'system.', runner ones with 'runner.' and environment variables with 'env.'
        static member All
            with get() = TeamCityBuildParameters.getAll()

    /// The type of change that occured
    type FileChangeType =
        | FileChanged
        | FileAdded
        | FileRemoved
        | FileNotChanged
        | DirectoryChanged
        | DirectoryAdded
        | DirectoryRemoved

    /// Describe a change between builds
    type FileChange = {
        /// Path of the file that changed, relative to the current checkout directory (TemaCity.Environment.CheckoutDirectory)
        FilePath: string
        /// Type of modification for the file
        ModificationType: FileChangeType
        /// File revision in the repository. If the file is a part of change list started via the remote run, then the value will be None
        Revision: string option }

    module private ChangedFiles =
        let get () =
            match BuildParameters.System |> Map.tryFind "teamcity.build.changedFiles.file" with
            | Some file when File.exists file ->
                Some [
                    for line in File.read file do
                        let split = line.Split(':')
                        if split.Length = 3 then
                            let filePath = split.[0]
                            let modificationType =
                                match split.[1].ToUpperInvariant() with
                                | "CHANGED" -> FileChanged
                                | "ADDED" -> FileAdded
                                | "REMOVED" -> FileRemoved
                                | "NOT_CHANGED" -> FileNotChanged
                                | "DIRECTORY_CHANGED" -> DirectoryChanged
                                | "DIRECTORY_ADDED" -> DirectoryAdded
                                | "DIRECTORY_REMOVED" -> DirectoryRemoved
                                | _ -> failwithf "Unknown change type: %s" (split.[1])
                            let revision =
                                match split.[2] with
                                | "<personal>" -> None
                                | revision -> Some revision

                            yield { FilePath = filePath; ModificationType = modificationType; Revision = revision }
                        else
                            failwithf "Unable to split change line: %s" line
                ]
            | _ -> None

        let cache = lazy (get ())

    module private RecentlyFailedTests =
        let get () =
            match BuildParameters.System |> Map.tryFind "teamcity.tests.recentlyFailedTests.file" with
            | Some file when File.exists file -> File.read file |> List.ofSeq |> Some
            | _ -> None

        let cache = lazy (get ())

    type Environment =
        /// The Version of the TeamCity server. This property can be used to determine the build is run within TeamCity.
        static member Version = Environment.environVarOrNone "TEAMCITY_VERSION"

        /// The Name of the project the current build belongs to or None if it's not on TeamCity.
        static member ProjectName = Environment.environVarOrNone "TEAMCITY_PROJECT_NAME"

        /// The Name of the Build Configuration the current build belongs to or None if it's not on TeamCity.
        static member BuildConfigurationName = Environment.environVarOrNone "TEAMCITY_BUILDCONF_NAME"

        /// Is set to true if the build is a personal one.
        static member BuildIsPersonal =
            match Environment.environVarOrNone "BUILD_IS_PERSONAL" with
            | Some _ -> true
            | None -> false

        /// The Build number assigned to the build by TeamCity using the build number format or None if it's not on TeamCity.
        static member BuildNumber = Environment.environVarOrNone "BUILD_NUMBER"

        /// Get the branch of the main VCS root
        static member Branch
            with get() = BuildParameters.Configuration |> Map.tryFind "vcsroot.branch"

        /// Get the display name of the branch of the main VCS root as shown in TeamCity
        ///
        /// See [the documentation](https://confluence.jetbrains.com/display/TCD18/Working+with+Feature+Branches#WorkingwithFeatureBranches-branchSpec) for more information
        static member BranchDisplayName
            with get() = 
                match BuildParameters.Configuration |> Map.tryFind "teamcity.build.branch" with
                | Some _  as branch -> branch
                | None -> Environment.Branch

        /// Get if the current branch of the main VCS root is the one configured as default
        static member IsDefaultBranch
            with get() =
                if BuildServer.buildServer = BuildServer.TeamCity then
                    match BuildParameters.Configuration |> Map.tryFind "teamcity.build.branch.is_default" with
                    | Some "true" -> true
                    | Some _ -> false
                    | None ->
                        // When only one branch is configured, TeamCity doesn't emit this parameter
                        Environment.Branch.IsSome
                else
                    false

        /// Get the path to the build checkout directory
        static member CheckoutDirectory
            with get() = BuildParameters.System |> Map.tryFind "teamcity.build.checkoutDir"

        /// Changed files (since previous build) that are included in this build
        ///
        /// See [the documentation](https://confluence.jetbrains.com/display/TCD18/Risk+Tests+Reordering+in+Custom+Test+Runner) for more information
        static member ChangedFiles
            with get() = ChangedFiles.cache.Value

        /// Name of recently failing tests
        ///
        /// See [the documentation](https://confluence.jetbrains.com/display/TCD18/Risk+Tests+Reordering+in+Custom+Test+Runner) for more information
        static member RecentlyFailedTests
            with get() = RecentlyFailedTests.cache.Value

    /// Implements a TraceListener for TeamCity build servers.
    /// 
    /// See [the documentation](https://confluence.jetbrains.com/display/TCD18/Build+Script+Interaction+with+TeamCity) for more information
    type internal TeamCityTraceListener() =

        interface ITraceListener with
            /// Writes the given message to the Console.
            member __.Write msg =
                let color = ConsoleWriter.colorMap msg
                match msg with
                | TraceData.OpenTag (KnownTags.Test name, _) ->
                    startTestCase name
                | TraceData.TestOutput (testName,out,err) ->
                    if not (String.IsNullOrEmpty out) then reportTestOutput testName out
                    if not (String.IsNullOrEmpty err) then reportTestError testName err
                | TraceData.TestStatus (testName,TestStatus.Ignored message) ->
                    ignoreTestCase testName message
                | TraceData.TestStatus (testName,TestStatus.Failed(message, detail, None)) ->
                    testFailed testName message detail
                | TraceData.TestStatus (testName,TestStatus.Failed(message, detail, Some (expected, actual))) ->
                    comparisonFailure testName message detail expected actual
                | TraceData.BuildState (TagStatus.Success, _) ->
                    reportBuildStatus "SUCCESS" "{build.status.text}"
                | TraceData.BuildState (TagStatus.Warning, None) ->
                    warning "Setting build state to warning."
                | TraceData.BuildState (TagStatus.Warning, Some message) ->
                    warning message
                | TraceData.BuildState (TagStatus.Failed, None) ->
                    reportBuildStatus "FAILURE" "Failure - {build.status.text}"
                | TraceData.BuildState (TagStatus.Failed, Some message) ->
                    reportBuildStatus "FAILURE" (sprintf "%s - {build.status.text}" message)
                | TraceData.CloseTag (KnownTags.Test name, time, _) ->
                    finishTestCase name time
                | TraceData.OpenTag (KnownTags.TestSuite name, _) ->
                    startTestSuite name
                | TraceData.CloseTag (KnownTags.TestSuite name, _, _) ->
                    finishTestSuite name
                | TraceData.OpenTag (tag, description) ->
                    match description with
                    | Some d -> TeamCityWriter.sendOpenBlock tag.Name (sprintf "%s: %s" tag.Type d)
                    | _ -> TeamCityWriter.sendOpenBlock tag.Name tag.Type
                | TraceData.CloseTag (tag, _, _) ->
                    TeamCityWriter.sendCloseBlock tag.Name
                | TraceData.ImportantMessage text ->
                    warning text
                | TraceData.ErrorMessage text ->
                    error text
                | TraceData.LogMessage(text, newLine) | TraceData.TraceMessage(text, newLine) ->
                    ConsoleWriter.write false color newLine text
                | TraceData.ImportData (ImportData.BuildArtifactWithName name, path) ->
                    publishNamedArtifact name path
                | TraceData.ImportData (ImportData.BuildArtifact, path) ->
                    publishArtifact path
                | TraceData.ImportData (ImportData.DotNetCoverage tool, path) ->
                    Import.sendDotNetCoverageForTool path tool
                | TraceData.ImportData (typ, path) ->
                    TeamCityWriter.sendImportData typ.TeamCityName path
                | TraceData.BuildNumber number -> setBuildNumber number

    let defaultTraceListener =
        TeamCityTraceListener() :> ITraceListener
    let detect () =
        BuildServer.buildServer = BuildServer.TeamCity
    let install(force:bool) =
        if not (detect()) then failwithf "Cannot run 'install()' on a non-TeamCity environment"
        if force || not (CoreTracing.areListenersSet()) then
            CoreTracing.setTraceListeners [defaultTraceListener]
        ()
    let Installer =
        { new BuildServerInstaller() with
            member __.Install () = install (false)
            member __.Detect () = detect() }
