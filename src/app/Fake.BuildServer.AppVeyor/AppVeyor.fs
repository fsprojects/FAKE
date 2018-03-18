/// Contains support for various build servers
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core
open Fake.IO

[<AutoOpen>]
module TypeExtensions =
    type DotNetCoverageTool with
        member x.AppVeyorName =
            match x with | DotCover -> "dotcover" | PartCover -> "partcover" | NCover -> "ncover" | NCover3 -> "ncover3"

    type ImportData with
        member x.AppVeyorName =
            match x with
            | BuildArtifact -> "buildArtifact"
            | DotNetCoverage _ -> "dotNetCoverage"
            | DotNetDupFinder -> "DotNetDupFinder"
            | PmdCpd -> "pmdCpd"
            | Pmd -> "pmd"
            | ReSharperInspectCode -> "ReSharperInspectCode"
            | Jslint -> "jslint"
            | FindBugs -> "findBugs"
            | Checkstyle -> "checkstyle"
            | Gtest -> "gtest"
            | Mstest -> "mstest"
            | Surefire -> "surefire"
            | Junit -> "junit"
            | FxCop -> "FxCop"
            | Nunit -> "nunit"
(*
module AppVeyor =

    /// Open Named Block that will be closed when the block is disposed
    /// Usage: `use __ = teamCityBlock "My Block"`
    let teamCityBlock name description =
        TeamCityWriter.sendOpenBlock name description
        { new System.IDisposable
            with member __.Dispose() = TeamCityWriter.sendCloseBlock name }

    /// Sends an error to TeamCity
    let sendTeamCityError error = TeamCityWriter.sendToTeamCity "##teamcity[buildStatus status='FAILURE' text='%s']" error

    let sendTeamCityImportData typ file = TeamCityWriter.sendToTeamCity2 "##teamcity[importData type='%s' file='%s']" typ file




    module Import =
        /// Sends an NUnit results filename to TeamCity
        let sendNUnit path = sendTeamCityImportData "nunit" path

        /// Sends an FXCop results filename to TeamCity
        let sendFXCop path = sendTeamCityImportData "FxCop" path

        /// Sends an JUnit Ant task results filename to TeamCity
        let sendJUnit path = sendTeamCityImportData "junit" path

        /// Sends an Maven Surefire results filename to TeamCity
        let sendSurefire path = sendTeamCityImportData "surefire" path

        /// Sends an MSTest results filename to TeamCity
        let sendMSTest path = sendTeamCityImportData "mstest" path

        /// Sends an Google Test results filename to TeamCity
        let sendGTest path = sendTeamCityImportData "gtest" path

        /// Sends an Checkstyle results filename to TeamCity
        let sendCheckstyle path = sendTeamCityImportData "checkstyle" path

        /// Sends an FindBugs results filename to TeamCity
        let sendFindBugs path = sendTeamCityImportData "findBugs" path

        /// Sends an JSLint results filename to TeamCity
        let sendJSLint path = sendTeamCityImportData "jslint" path

        /// Sends an ReSharper inspectCode.exe results filename to TeamCity
        let sendReSharperInspectCode path = sendTeamCityImportData "ReSharperInspectCode" path

        /// Sends an PMD inspections results filename to TeamCity
        let sendPmd path = sendTeamCityImportData "pmd" path

        /// Sends an PMD Copy/Paste Detector results filename to TeamCity
        let sendPmdCpd path = sendTeamCityImportData "pmdCpd" path

        /// Sends an ReSharper dupfinder.exe results filename to TeamCity
        let sendDotNetDupFinder path = sendTeamCityImportData "DotNetDupFinder" path

        /// Sends an dotcover, partcover, ncover or ncover3 results filename to TeamCity
        let sendDotNetCoverageForTool path (tool : DotNetCoverageTool) =
            sprintf "##teamcity[importData type='dotNetCoverage' tool='%s' path='%s']" (tool.TeamCityName |> TeamCityWriter.scrub) (path |> TeamCityWriter.scrub)
            |> TeamCityWriter.sendStrToTeamCity

    /// Sends the full path to the dotCover home folder to override the bundled dotCover to TeamCity
    let sendTeamCityDotCoverHome = TeamCityWriter.sendToTeamCity "##teamcity[dotNetCoverage dotcover_home='%s']"

    /// Sends the full path to NCover installation folder to TeamCity
    let sendTeamCityNCover3Home = TeamCityWriter.sendToTeamCity "##teamcity[dotNetCoverage ncover3_home='%s']"

    /// Sends arguments for the NCover report generator to TeamCity
    let sendTeamCityNCover3ReporterArgs = TeamCityWriter.sendToTeamCity "##teamcity[dotNetCoverage ncover3_reporter_args='%s']"

    /// Sends the path to NCoverExplorer to TeamCity
    let sendTeamCityNCoverExplorerTool = TeamCityWriter.sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_tool='%s']"

    /// Sends additional arguments for NCover 1.x to TeamCity
    let sendTeamCityNCoverExplorerToolArgs = TeamCityWriter.sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_tool_args='%s']"

    /// Sends the value for NCover /report: argument to TeamCity
    let sendTeamCityNCoverReportType : int -> unit = string >> TeamCityWriter.sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_report_type='%s']"

    /// Sends the value for NCover  /sort: argument to TeamCity
    let sendTeamCityNCoverReportOrder : int -> unit = string >> TeamCityWriter.sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_report_order='%s']"

    /// Send the PartCover xslt transformation rules (Input xlst and output files) to TeamCity
    let sendTeamCityPartCoverReportXslts : seq<string * string> -> unit =
        Seq.map (fun (xslt, output) -> sprintf "%s=>%s" xslt output)
        >> Seq.map TeamCityWriter.EncapsulateSpecialChars
        >> String.concat "|n"
        >> sprintf "##teamcity[dotNetCoverage partcover_report_xslts='%s']"
        >> TeamCityWriter.sendStrToTeamCity

    /// Starts the test case.
    let StartTestCase testCaseName =
        TeamCityWriter.sendToTeamCity "##teamcity[testStarted name='%s' captureStandardOutput='true']" testCaseName

    /// Finishes the test case.
    let FinishTestCase testCaseName (duration : System.TimeSpan) =
        let duration =
            duration.TotalMilliseconds
            |> round
            |> string
        sprintf "##teamcity[testFinished name='%s' duration='%s']" (TeamCityWriter.EncapsulateSpecialChars testCaseName) duration
        |> TeamCityWriter.sendStrToTeamCity

    /// Ignores the test case.
    let IgnoreTestCase name message =
        StartTestCase name
        sprintf "##teamcity[testIgnored name='%s' message='%s']" (TeamCityWriter.EncapsulateSpecialChars name)
            (TeamCityWriter.EncapsulateSpecialChars message) |> TeamCityWriter.sendStrToTeamCity


    /// Report Standard-Output for a given test-case
    let ReportTestOutput name output =
        sprintf "##teamcity[testStdOut name='%s' out='%s']" 
            (TeamCityWriter.EncapsulateSpecialChars name)
            (TeamCityWriter.EncapsulateSpecialChars output)
        |> TeamCityWriter.sendStrToTeamCity

    /// Report Standard-Error for a given test-case
    let ReportTestError name output =
        sprintf "##teamcity[testStdErr name='%s' out='%s']" 
            (TeamCityWriter.EncapsulateSpecialChars name)
            (TeamCityWriter.EncapsulateSpecialChars output)
        |> TeamCityWriter.sendStrToTeamCity

    /// Ignores the test case.
    let IgnoreTestCaseWithDetails name message details =
        IgnoreTestCase name (message + " " + details)

    /// Finishes the test suite.
    let FinishTestSuite testSuiteName =
        TeamCityWriter.EncapsulateSpecialChars testSuiteName |> TeamCityWriter.sendToTeamCity "##teamcity[testSuiteFinished name='%s']"

    /// Starts the test suite.
    let StartTestSuite testSuiteName =
        TeamCityWriter.EncapsulateSpecialChars testSuiteName |> TeamCityWriter.sendToTeamCity "##teamcity[testSuiteStarted name='%s']"

    /// Reports the progress.
    let ReportProgress message = TeamCityWriter.EncapsulateSpecialChars message |> TeamCityWriter.sendToTeamCity "##teamcity[progressMessage '%s']"

    /// Reports the progress start.
    let ReportProgressStart message = TeamCityWriter.EncapsulateSpecialChars message |> TeamCityWriter.sendToTeamCity "##teamcity[progressStart '%s']"

    /// Reports the progress end.
    let ReportProgressFinish message = TeamCityWriter.EncapsulateSpecialChars message |> TeamCityWriter.sendToTeamCity "##teamcity[progressFinish '%s']"

    /// Create  the build status.
    /// [omit]
    let buildStatus status message =
        sprintf "##teamcity[buildStatus status='%s' text='%s']" (TeamCityWriter.EncapsulateSpecialChars status) (TeamCityWriter.EncapsulateSpecialChars message)

    /// Reports the build status.
    let ReportBuildStatus status message = buildStatus status message |> TeamCityWriter.sendStrToTeamCity

    /// Publishes an artifact on the TeamcCity build server.
    let PublishArtifact path = TeamCityWriter.EncapsulateSpecialChars path |> TeamCityWriter.sendToTeamCity "##teamcity[publishArtifacts '%s']"

    /// Sets the TeamCity build number.
    let SetBuildNumber buildNumber = TeamCityWriter.EncapsulateSpecialChars buildNumber |> TeamCityWriter.sendToTeamCity "##teamcity[buildNumber '%s']"

    /// Reports a build statistic.
    let SetBuildStatistic key value =
        sprintf "##teamcity[buildStatisticValue key='%s' value='%s']" (TeamCityWriter.EncapsulateSpecialChars key)
            (TeamCityWriter.EncapsulateSpecialChars value) |> TeamCityWriter.sendStrToTeamCity

    /// Reports a parameter value
    let SetTeamCityParameter name value =
        sprintf "##teamcity[setParameter name='%s' value='%s']" (TeamCityWriter.EncapsulateSpecialChars name)
            (TeamCityWriter.EncapsulateSpecialChars value) |> TeamCityWriter.sendStrToTeamCity

    /// Reports a failed test.
    let TestFailed name message details =
        sprintf "##teamcity[testFailed name='%s' message='%s' details='%s']" (TeamCityWriter.EncapsulateSpecialChars name)
            (TeamCityWriter.EncapsulateSpecialChars message) (TeamCityWriter.EncapsulateSpecialChars details) |> TeamCityWriter.sendStrToTeamCity

    /// Reports a failed comparison.
    let ComparisonFailure name message details expected actual =
        sprintf
            "##teamcity[testFailed type='comparisonFailure' name='%s' message='%s' details='%s' expected='%s' actual='%s']"
            (TeamCityWriter.EncapsulateSpecialChars name) (TeamCityWriter.EncapsulateSpecialChars message) (TeamCityWriter.EncapsulateSpecialChars details)
            (TeamCityWriter.EncapsulateSpecialChars expected) (TeamCityWriter.EncapsulateSpecialChars actual) |> TeamCityWriter.sendStrToTeamCity

    /// The Version of the TeamCity server. This property can be used to determine the build is run within TeamCity.
    let TeamCityVersion = Environment.environVarOrNone "TEAMCITY_VERSION"

    /// The Name of the project the current build belongs to or None if it's not on TeamCity.
    let TeamCityProjectName = Environment.environVarOrNone "TEAMCITY_PROJECT_NAME"

    /// The Name of the Build Configuration the current build belongs to or None if it's not on TeamCity.
    let TeamCityBuildConfigurationName = Environment.environVarOrNone "TEAMCITY_BUILDCONF_NAME"

    /// Is set to true if the build is a personal one.
    let TeamCityBuildIsPersonal =
        match Environment.environVarOrNone "BUILD_IS_PERSONAL" with
        | Some _ -> true
        | None -> false

    /// The Build number assigned to the build by TeamCity using the build number format or None if it's not on TeamCity.
    let TeamCityBuildNumber = Environment.environVarOrNone "BUILD_NUMBER"


    /// Implements a TraceListener for TeamCity build servers.
    /// ## Parameters
    ///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
    ///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
    type internal AppVeyorTraceListener(importantMessagesToStdErr, colorMap) =

        interface ITraceListener with
            /// Writes the given message to the Console.
            member __.Write msg = 
                let color = colorMap msg
                match msg with
                | OpenTag (KnownTags.Test name, _) ->
                    StartTestCase name
                | TestOutput (testName,out,false) ->
                    ReportTestOutput testName out
                | TestOutput (testName,out,true) ->
                    ReportTestError testName out
                | TestStatus (testName,Ignored message) ->
                    IgnoreTestCase testName message
                | TestStatus (testName,Failed(message, detail, None)) ->
                    TestFailed testName message detail
                | TestStatus (testName,Failed(message, detail, Some (expected, actual))) ->
                    ComparisonFailure testName message detail expected actual
                | CloseTag (KnownTags.Test name, time) ->
                    FinishTestCase name time
                | OpenTag (KnownTags.TestSuite name, _) ->
                    StartTestSuite name
                | CloseTag (KnownTags.TestSuite name, _) ->
                    FinishTestSuite name
                | OpenTag (tag, description) ->
                    TeamCityWriter.sendOpenBlock tag.Name (sprintf "%s: %s" tag.Type description)
                | CloseTag (tag, _) ->
                    TeamCityWriter.sendCloseBlock tag.Name
                | ImportantMessage text | ErrorMessage text ->
                    ConsoleWriter.write importantMessagesToStdErr color true text
                | LogMessage(text, newLine) | TraceMessage(text, newLine) ->
                    ConsoleWriter.write false color newLine text
                | ImportData (BuildArtifact, path) ->
                    PublishArtifact path
                | ImportData (DotNetCoverage tool, path) ->
                    Import.sendDotNetCoverageForTool path tool
                | ImportData (typ, path) ->
                    sendTeamCityImportData typ.TeamCityName path
                | BuildNumber number -> SetBuildNumber number

    let defaultTraceListener =
      AppVeyorTraceListener(false, ConsoleWriter.colorMap) :> ITraceListener
    let detect () =
        BuildServer.buildServer = BuildServer.AppVeyor
    let install(force:bool) =
        if not (detect()) then failwithf "Cannot run 'install()' on a non-AppVeyor environment"
        if force || not (CoreTracing.areListenersSet()) then
            CoreTracing.setTraceListeners [defaultTraceListener]
        () 
    let Installer =
        { new BuildServerInstaller() with
            member __.Install () = install (false)
            member __.Detect () = detect() }*)