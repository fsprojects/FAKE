[<AutoOpen>]
/// Contains helper functions which allow FAKE to communicate with a TeamCity agent
module Fake.TeamCityHelper

/// Encapsulates special chars
let inline EncapsulateSpecialChars text = 
    text
    |> replace "|" "||"
    |> replace "'" "|'"
    |> replace "\n" "|n"
    |> replace "\r" "|r"
    |> replace "[" "|["
    |> replace "]" "|]"

let scrub = RemoveLineBreaks >> EncapsulateSpecialChars

/// Send message to TeamCity
let sendToTeamCity format message = 
    if buildServer = TeamCity then 
        message
        |> scrub
        |> sprintf format
        |> fun m -> postMessage (LogMessage(m, true))

/// Send message to TeamCity
let sendStrToTeamCity s = 
    if buildServer = TeamCity then postMessage (LogMessage(RemoveLineBreaks s, true))

/// Open Named Block
let sendOpenBlock = sendToTeamCity "##teamcity[blockOpened name='%s']"

/// Close Named Block
let sendCloseBlock = sendToTeamCity "##teamcity[blockClosed name='%s']"

/// Sends an error to TeamCity
let sendTeamCityError error = sendToTeamCity "##teamcity[buildStatus status='FAILURE' text='%s']" error

/// Sends an NUnit results filename to TeamCity
let sendTeamCityNUnitImport path = sendToTeamCity "##teamcity[importData type='nunit' file='%s']" path

/// Sends an FXCop results filename to TeamCity    
let sendTeamCityFXCopImport path = sendToTeamCity "##teamcity[importData type='FxCop' path='%s']" path

/// Sends an JUnit Ant task results filename to TeamCity    
let sendTeamCityJUnitImport path = sendToTeamCity "##teamcity[importData type='junit' path='%s']" path

/// Sends an Maven Surefire results filename to TeamCity    
let sendTeamCitySurefireImport path = sendToTeamCity "##teamcity[importData type='surefire' path='%s']" path

/// Sends an MSTest results filename to TeamCity    
let sendTeamCityMSTestImport path = sendToTeamCity "##teamcity[importData type='mstest' path='%s']" path

/// Sends an Google Test results filename to TeamCity    
let sendTeamCityGTestImport path = sendToTeamCity "##teamcity[importData type='gtest' path='%s']" path

/// Sends an Checkstyle results filename to TeamCity    
let sendTeamCityCheckstyleImport path = sendToTeamCity "##teamcity[importData type='checkstyle' path='%s']" path

/// Sends an FindBugs results filename to TeamCity    
let sendTeamCityFindBugsImport path = sendToTeamCity "##teamcity[importData type='findBugs' path='%s']" path

/// Sends an JSLint results filename to TeamCity    
let sendTeamCityJSLintImport path = sendToTeamCity "##teamcity[importData type='jslint' path='%s']" path

/// Sends an ReSharper inspectCode.exe results filename to TeamCity    
let sendTeamCityReSharperInspectCodeImport path = sendToTeamCity "##teamcity[importData type='ReSharperInspectCode' path='%s']" path

/// Sends an FxCop inspection results filename to TeamCity    
let sendTeamCityFxCopImport path = sendToTeamCity "##teamcity[importData type='FxCop' path='%s']" path

/// Sends an PMD inspections results filename to TeamCity    
let sendTeamCityPmdImport path = sendToTeamCity "##teamcity[importData type='pmd' path='%s']" path

/// Sends an PMD Copy/Paste Detector results filename to TeamCity    
let sendTeamCityPmdCpdImport path = sendToTeamCity "##teamcity[importData type='pmdCpd' path='%s']" path

/// Sends an ReSharper dupfinder.exe results filename to TeamCity    
let sendTeamCityDotNetDupFinderImport path = sendToTeamCity "##teamcity[importData type='DotNetDupFinder' path='%s']" path

/// Sends an dotcover, partcover, ncover or ncover3 results filename to TeamCity    
[<System.Obsolete("This function does not specify the type of coverage tool used to generate the report.  Use 'sendTeamCityDotNetCoverageImportForTool' instead")>]
let sendTeamCityDotNetCoverageImport path = sendToTeamCity "##teamcity[importData type='dotNetCoverage' path='%s']" path

type TeamCityDotNetCoverageTool = | DotCover | PartCover | NCover | NCover3 with override x.ToString() = match x with | DotCover -> "dotcover" | PartCover -> "partcover" | NCover -> "ncover" | NCover3 -> "ncover3"
/// Sends an dotcover, partcover, ncover or ncover3 results filename to TeamCity    
let sendTeamCityDotNetCoverageImportForTool path (tool : TeamCityDotNetCoverageTool) = 
    sprintf "##teamcity[importData type='dotNetCoverage' tool='%s' path='%s']" (string tool |> scrub) (path |> scrub)
    |> sendStrToTeamCity

/// Sends the full path to the dotCover home folder to override the bundled dotCover to TeamCity
let sendTeamCityDotCoverHome = sendToTeamCity "##teamcity[dotNetCoverage dotcover_home='%s']"
    
/// Sends the full path to NCover installation folder to TeamCity
let sendTeamCityNCover3Home = sendToTeamCity "##teamcity[dotNetCoverage ncover3_home='%s']"

/// Sends arguments for the NCover report generator to TeamCity
let sendTeamCityNCover3ReporterArgs = sendToTeamCity "##teamcity[dotNetCoverage ncover3_reporter_args='%s']"
    
/// Sends the path to NCoverExplorer to TeamCity
let sendTeamCityNCoverExplorerTool = sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_tool='%s']"
    
/// Sends additional arguments for NCover 1.x to TeamCity
let sendTeamCityNCoverExplorerToolArgs = sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_tool_args='%s']"
    
/// Sends the value for NCover /report: argument to TeamCity
let sendTeamCityNCoverReportType : int -> unit = string >> sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_report_type='%s']"
    
/// Sends the value for NCover  /sort: argument to TeamCity
let sendTeamCityNCoverReportOrder : int -> unit = string >> sendToTeamCity "##teamcity[dotNetCoverage ncover_explorer_report_order='%s']"
    
/// Send the PartCover xslt transformation rules (Input xlst and output files) to TeamCity
let sendTeamCityPartCoverReportXslts : seq<string * string> -> unit =
    Seq.map (fun (xslt, output) -> sprintf "%s=>%s" xslt output)
    >> Seq.map EncapsulateSpecialChars
    >> String.concat "|n"
    >> sprintf "##teamcity[dotNetCoverage partcover_report_xslts='%s']"
    >> sendStrToTeamCity      

/// Starts the test case.
let StartTestCase testCaseName = 
    sendToTeamCity "##teamcity[testStarted name='%s' captureStandardOutput='true']" testCaseName

/// Finishes the test case.
let FinishTestCase testCaseName (duration : System.TimeSpan) = 
    let duration = 
        duration.TotalMilliseconds
        |> round
        |> string
    sprintf "##teamcity[testFinished name='%s' duration='%s']" (EncapsulateSpecialChars testCaseName) duration 
    |> sendStrToTeamCity

/// Ignores the test case.      
let IgnoreTestCase name message = 
    StartTestCase name
    sprintf "##teamcity[testIgnored name='%s' message='%s']" (EncapsulateSpecialChars name) 
        (EncapsulateSpecialChars message) |> sendStrToTeamCity


/// Ignores the test case.      
let IgnoreTestCaseWithDetails name message details = 
    IgnoreTestCase name (message + " " + details)

/// Finishes the test suite.
let FinishTestSuite testSuiteName = 
    EncapsulateSpecialChars testSuiteName |> sendToTeamCity "##teamcity[testSuiteFinished name='%s']"

/// Starts the test suite.
let StartTestSuite testSuiteName = 
    EncapsulateSpecialChars testSuiteName |> sendToTeamCity "##teamcity[testSuiteStarted name='%s']"

/// Reports the progress.
let ReportProgress message = EncapsulateSpecialChars message |> sendToTeamCity "##teamcity[progressMessage '%s']"

/// Reports the progress start.
let ReportProgressStart message = EncapsulateSpecialChars message |> sendToTeamCity "##teamcity[progressStart '%s']"

/// Reports the progress end.
let ReportProgressFinish message = EncapsulateSpecialChars message |> sendToTeamCity "##teamcity[progressFinish '%s']"

/// Create  the build status.
/// [omit]
let buildStatus status message = 
    sprintf "##teamcity[buildStatus '%s' text='%s']" (EncapsulateSpecialChars status) (EncapsulateSpecialChars message)

/// Reports the build status.
let ReportBuildStatus status message = buildStatus status message |> sendStrToTeamCity

/// Publishes an artifact on the TeamcCity build server.
let PublishArtifact path = EncapsulateSpecialChars path |> sendToTeamCity "##teamcity[publishArtifacts '%s']"

[<System.Obsolete("There was a typo - please use PublishArtifact")>]
let PublishArticfact path = PublishArtifact path

/// Sets the TeamCity build number.
let SetBuildNumber buildNumber = EncapsulateSpecialChars buildNumber |> sendToTeamCity "##teamcity[buildNumber '%s']"

/// Reports a build statistic.
let SetBuildStatistic key value = 
    sprintf "##teamcity[buildStatisticValue key='%s' value='%s']" (EncapsulateSpecialChars key) 
        (EncapsulateSpecialChars value) |> sendStrToTeamCity

/// Reports a parameter value
let SetTeamCityParameter name value = 
    sprintf "##teamcity[setParameter name='%s' value='%s']" (EncapsulateSpecialChars name) 
        (EncapsulateSpecialChars value) |> sendStrToTeamCity

/// Reports a failed test.
let TestFailed name message details = 
    sprintf "##teamcity[testFailed name='%s' message='%s' details='%s']" (EncapsulateSpecialChars name) 
        (EncapsulateSpecialChars message) (EncapsulateSpecialChars details) |> sendStrToTeamCity

/// Reports a failed comparison.
let ComparisonFailure name message details expected actual = 
    sprintf 
        "##teamcity[testFailed type='comparisonFailure' name='%s' message='%s' details='%s' expected='%s' actual='%s']" 
        (EncapsulateSpecialChars name) (EncapsulateSpecialChars message) (EncapsulateSpecialChars details) 
        (EncapsulateSpecialChars expected) (EncapsulateSpecialChars actual) |> sendStrToTeamCity

/// Gets the recently failed tests
let getRecentlyFailedTests() = appSetting "teamcity.tests.recentlyFailedTests.file" |> ReadFile

/// Gets the changed files
let getChangedFilesInCurrentBuild() = appSetting "teamcity.build.changedFiles.file" |> ReadFile

/// The Version of the TeamCity server. This property can be used to determine the build is run within TeamCity.
let TeamCityVersion = environVarOrNone "TEAMCITY_VERSION"

/// The Name of the project the current build belongs to or None if it's not on TeamCity.
let TeamCityProjectName = environVarOrNone "TEAMCITY_PROJECT_NAME"

/// The Name of the Build Configuration the current build belongs to or None if it's not on TeamCity.
let TeamCityBuildConfigurationName = environVarOrNone "TEAMCITY_BUILDCONF_NAME"

/// Is set to true if the build is a personal one.
let TeamCityBuildIsPersonal = 
    match environVarOrNone "BUILD_IS_PERSONAL" with
    | Some _ -> true
    | None -> false

/// The Build number assigned to the build by TeamCity using the build number format or None if it's not on TeamCity.
let TeamCityBuildNumber = environVarOrNone "BUILD_NUMBER"
