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

/// Send message to TeamCity
let sendToTeamCity format message = 
    if buildServer = TeamCity then 
        message
        |> RemoveLineBreaks
        |> EncapsulateSpecialChars
        |> sprintf format
        |> fun m -> postMessage (LogMessage(m, true))

/// Send message to TeamCity
let sendStrToTeamCity s = 
    if buildServer = TeamCity then postMessage (LogMessage(RemoveLineBreaks s, true))

/// Sends an error to TeamCity
let sendTeamCityError error = sendToTeamCity "##teamcity[buildStatus status='FAILURE' text='%s']" error

/// Sends an NUnit results filename to TeamCity
let sendTeamCityNUnitImport path = sendToTeamCity "##teamcity[importData type='nunit' file='%s']" path

/// Sends an FXCop results filename to TeamCity    
let sendTeamCityFXCopImport path = sendToTeamCity "##teamcity[importData type='FxCop' path='%s']" path

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
