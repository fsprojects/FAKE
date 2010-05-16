[<AutoOpen>]
module Fake.TeamCityHelper

/// Send message to TeamCity
let sendToTeamCity format message =
    if buildServer = TeamCity then
        let m = 
            message 
              |> RemoveLineBreaks 
              |> EncapsulateApostrophe
              |> toRelativePath 
              |> sprintf format
        buffer.Post {defaultMessage with Text = m }

    
/// Send message to TeamCity
let sendStrToTeamCity s =
    if buildServer = TeamCity then
        buffer.Post {defaultMessage with Text = RemoveLineBreaks s }
  
/// Sends an error to TeamCity
let sendTeamCityError x =
  sendToTeamCity "##teamcity[buildStatus status='FAILURE' text='{build.status.text} %s']" x

/// Sends an NUnit results filename to TeamCity
let sendTeamCityNUnitImport x =  
  sendToTeamCity "##teamcity[importData type='nunit' file='%s']" x

/// Sends an FXCop results filename to TeamCity    
let sendTeamCityFXCopImport x =      
  sendToTeamCity "##teamcity[importData type='FxCop' path='%s']" x
  
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

/// Tests the failed.
let TestFailed name message details =  
  sprintf "##teamcity[testFailed name='%s' message='%s' details='%s']"
    (EncapsulateApostrophe name)
    (EncapsulateApostrophe message)
    (EncapsulateApostrophe details)
    |> sendStrToTeamCity 
  
/// ComparisonFailure.
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
    System.Configuration.ConfigurationManager.AppSettings.["teamcity.tests.recentlyFailedTests.file"]    
      |> ReadFile