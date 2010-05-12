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
  sendToTeamCity "##teamcity[testSuiteFinished name='%s']" (EncapsulateApostrophe testSuiteName)

/// Starts the test suite.
let StartTestSuite testSuiteName =
  sendToTeamCity "##teamcity[testSuiteStarted name='%s']" (EncapsulateApostrophe testSuiteName)

/// Reports the progress.
let ReportProgress message =
  sendToTeamCity "##teamcity[progressMessage '%s']" (EncapsulateApostrophe message)

let ReportProgressStart message =
  sendToTeamCity "##teamcity[progressStart '%s']" (EncapsulateApostrophe message)

let ReportProgressFinish message =
  sendToTeamCity "##teamcity[progressFinish '%s']" (EncapsulateApostrophe message)

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
       
let showRecentlyFailedTests() =
    let s = System.Configuration.ConfigurationManager.AppSettings.["teamcity.tests.recentlyFailedTests.file"]    
    ReadFile s
      |> Seq.iter (printfn "%s")      

let prepareURL restURL (serverURL:string) = 
  serverURL.Trim('/') + restURL

/// Returns the REST version of the TeamCity server
let getRESTVersion serverURL username password =      
 serverURL
   |> prepareURL "/httpAuth/app/rest/version"
   |> REST.ExecuteGetCommand username password 

type VCSRoot =
 {URL: string;
  Properties: Map<string,string>;
  VCSName: string;
  Name: string}

type Build =
  { ID:string; 
    Number:string;
    Status:string;
    WebURL:string }

type BuildConfiguration =
  { ID: string;
    Name: string; 
    WebURL: string;
    ProjectID: string; 
    Paused: bool;
    Description: string;
    Builds: Build seq}

let getFirstNode serverURL username password url =
    serverURL
      |> prepareURL url
      |> REST.ExecuteGetCommand username password 
      |> XMLDoc
      |> DocElement
 
/// Gets a projects from the TeamCity server
let getBuildConfig serverURL username password id =
  id 
    |> sprintf "/httpAuth/app/rest/buildTypes/id:%s"
    |> getFirstNode serverURL username password 
    |> parse "buildType" (fun n ->             
          {ID = getAttribute "id" n;
           Name = getAttribute "name" n;
           Description = getAttribute "description" n;
           WebURL = getAttribute "webUrl" n;
           Paused = getAttribute "paused" n |> System.Boolean.Parse;
           ProjectID = parseSubNode "project" (getAttribute "id") n;
           Builds =[]})

type Project = 
  {ID: string;
   Name: string;
   Description: string;
   WebURL: string;
   Archived: bool;
   BuildConfigs: string seq}

/// Gets a projects from the TeamCity server
let getProject serverURL username password id =
  id 
    |> sprintf "/httpAuth/app/rest/projects/id:%s"
    |> getFirstNode serverURL username password 
    |> parse "project" (fun n ->             
          {ID = getAttribute "id" n;
           Name = getAttribute "name" n;
           Description = getAttribute "description" n;
           WebURL = getAttribute "webUrl" n;
           Archived = getAttribute "archived" n |> System.Boolean.Parse;
           BuildConfigs = 
             parseSubNode "buildTypes" getChilds n
               |> Seq.map  (getAttribute "id") })

/// Gets all projects on the TeamCity 
let getProjects serverURL username password =  
  getFirstNode serverURL username password "/httpAuth/app/rest/projects"  
    |> parse "projects" getChilds
    |> Seq.map (getAttribute "id")