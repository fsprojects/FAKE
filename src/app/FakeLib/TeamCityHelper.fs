[<AutoOpen>]
module Fake.TeamCityHelper

/// Send message to TeamCity
let sendToTeamCity format message =
    if buildServer = TeamCity then
        message 
          |> RemoveLineBreaks 
          |> EncapsulateApostrophe
          |> toRelativePath 
          |> sprintf format
          |> fun m -> buffer.Post {defaultMessage with Text = m }
    
/// Send message to TeamCity
let sendStrToTeamCity s =
    if buildServer = TeamCity then buffer.Post {defaultMessage with Text = RemoveLineBreaks s }
  
/// Sends an error to TeamCity
let sendTeamCityError error =
    sendToTeamCity "##teamcity[buildStatus status='FAILURE' text='{build.status.text} %s']" error

/// Sends an NUnit results filename to TeamCity
let sendTeamCityNUnitImport path =  
    sendToTeamCity "##teamcity[importData type='nunit' file='%s']" path

/// Sends an FXCop results filename to TeamCity    
let sendTeamCityFXCopImport path =      
    sendToTeamCity "##teamcity[importData type='FxCop' path='%s']" path
  
/// Starts the test case.
let StartTestCase testCaseName =
    sendToTeamCity "##teamcity[testStarted name='%s' captureStandardOutput='true']" testCaseName
  
/// Finishes the test case.
let FinishTestCase testCaseName (duration:System.TimeSpan) =
    sprintf "##teamcity[testFinished name='%s' duration='%s']" 
      (testCaseName |> EncapsulateApostrophe)
      (duration.TotalMilliseconds |> round |> string) 
      |> sendStrToTeamCity 
                
/// Ignores the test case.      
let IgnoreTestCase name message =
  StartTestCase name
  sprintf "##teamcity[testIgnored name='%s' message='%s']" (EncapsulateApostrophe name) (EncapsulateApostrophe message)
    |> sendStrToTeamCity
  FinishTestCase name System.TimeSpan.Zero
  
/// Finishes the test suite.
let FinishTestSuite testSuiteName =
    EncapsulateApostrophe testSuiteName
      |> sendToTeamCity "##teamcity[testSuiteFinished name='%s']" 

/// Starts the test suite.
let StartTestSuite testSuiteName =
    EncapsulateApostrophe testSuiteName
      |> sendToTeamCity "##teamcity[testSuiteStarted name='%s']"

/// Reports the progress.
let ReportProgress message =
    EncapsulateApostrophe message
      |> sendToTeamCity "##teamcity[progressMessage '%s']"

/// Reports the progress start.
let ReportProgressStart message =
    EncapsulateApostrophe message
      |> sendToTeamCity "##teamcity[progressStart '%s']"

/// Reports the progress end.
let ReportProgressFinish message =
    EncapsulateApostrophe message
      |> sendToTeamCity "##teamcity[progressFinish '%s']"

/// Reports the build status.
let ReportBuildStatus status message =
    sprintf "##teamcity[buildStatus '%s' text='%s']"
      (EncapsulateApostrophe status)
      (EncapsulateApostrophe message)
      |> sendStrToTeamCity

/// Publishes an artifact on the TeamcCity build server.
let PublishArticfact path =
    EncapsulateApostrophe path
      |> sendToTeamCity "##teamcity[publishArtifacts '%s']"

/// Sets the TeamCity build number.
let SetBuildNumber buildNumber =
    EncapsulateApostrophe buildNumber
      |> sendToTeamCity "##teamcity[buildNumber '%s']"

/// Reports a build statistic.
let SetBuildStatistic key value =
    sprintf "##teamcity[buildStatisticValue key='%s' value='%s']"
      (EncapsulateApostrophe key)
      (EncapsulateApostrophe value)
      |> sendStrToTeamCity

/// Reports a failed test.
let TestFailed name message details =  
    sprintf "##teamcity[testFailed name='%s' message='%s' details='%s']"
      (EncapsulateApostrophe name)
      (EncapsulateApostrophe message)
      (EncapsulateApostrophe details)
      |> sendStrToTeamCity
  
/// Reports a failed comparison.
let ComparisonFailure name message details expected actual =
    sprintf "##teamcity[testFailed type='comparisonFailure' name='%s' message='%s' details='%s' expected='%s' actual='%s']"
      (EncapsulateApostrophe name)
      (EncapsulateApostrophe message)
      (EncapsulateApostrophe details)
      (EncapsulateApostrophe expected)
      (EncapsulateApostrophe actual)
      |> sendStrToTeamCity 
       
/// Gets the recently failed tests
let getRecentlyFailedTests() =
    appSetting "teamcity.tests.recentlyFailedTests.file"
      |> ReadFile