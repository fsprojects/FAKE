/// Contains code to configure FAKE for AppVeyor integration
module Fake.AppVeyor

/// AppVeyor environment variables as [described](http://www.appveyor.com/docs/environment-variables)
type AppVeyorEnvironment = 
    
    /// AppVeyor Build Agent API URL
    static member ApiUrl = environVar "APPVEYOR_API_URL"
    
    /// AppVeyor unique project ID
    static member ProjectId = environVar "APPVEYOR_PROJECT_ID"
    
    /// Project name
    static member ProjectName = environVar "APPVEYOR_PROJECT_NAME"
    
    /// Project slug (as seen in project details URL)
    static member ProjectSlug = environVar "APPVEYOR_PROJECT_SLUG"
    
    /// Path to clone directory
    static member BuildFolder = environVar "APPVEYOR_BUILD_FOLDER"
    
    /// AppVeyor unique build ID
    static member BuildId = environVar "APPVEYOR_BUILD_ID"
    
    /// Build number
    static member BuildNumber = environVar "APPVEYOR_BUILD_NUMBER"
    
    /// Build version
    static member BuildVersion = environVar "APPVEYOR_BUILD_VERSION"
    
    /// GitHub Pull Request number
    static member PullRequestNumber = environVar "APPVEYOR_PULL_REQUEST_NUMBER"
    
    /// GitHub Pull Request title
    static member PullRequestTitle = environVar "APPVEYOR_PULL_REQUEST_TITLE"
    
    /// AppVeyor unique job ID
    static member JobId = environVar "APPVEYOR_JOB_ID"
    
    /// GitHub, BitBucket or Kiln
    static member RepoProvider = environVar "APPVEYOR_REPO_PROVIDER"
    
    /// git or mercurial
    static member RepoScm = environVar "APPVEYOR_REPO_SCM"
    
    /// Repository name in format owner-name/repo-name
    static member RepoName = environVar "APPVEYOR_REPO_NAME"
    
    /// Build branch
    static member RepoBranch = environVar "APPVEYOR_REPO_BRANCH"
    
    /// Commit ID (SHA)
    static member RepoCommit = environVar "APPVEYOR_REPO_COMMIT"
    
    /// Commit author's name
    static member RepoCommitAuthor = environVar "APPVEYOR_REPO_COMMIT_AUTHOR"
    
    /// Commit author's email address
    static member RepoCommitAuthorEmail = environVar "APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL"
    
    /// Commit date/time
    static member RepoCommitTimestamp = environVar "APPVEYOR_REPO_COMMIT_TIMESTAMP"
    
    /// Commit message
    static member RepoCommitMessage = environVar "APPVEYOR_REPO_COMMIT_MESSAGE"
    
    /// true if build has started by pushed tag; otherwise false
    static member RepoTag =
        let rt = environVar "APPVEYOR_REPO_TAG"
        rt <> null && rt.Equals("true", System.StringComparison.OrdinalIgnoreCase)

    /// contains tag name for builds started by tag
    static member RepoTagName = environVar "APPVEYOR_REPO_TAG_NAME"

    /// Platform name set on Build tab of project settings (or through platform parameter in appveyor.yml).
    static member Platform = environVar "PLATFORM"

    /// Configuration name set on Build tab of project settings (or through configuration parameter in appveyor.yml).  
    static member Configuration  = environVar "CONFIGURATION"
    
let private sendToAppVeyor args = 
    ExecProcess (fun info -> 
        info.FileName <- "appveyor"
        info.Arguments <- args) (System.TimeSpan.MaxValue)
    |> ignore

let private add msg category = 
    sprintf "AddMessage %s -Category %s" (quoteIfNeeded msg) (quoteIfNeeded category) |> sendToAppVeyor
let private addNoCategory msg = sprintf "AddMessage %s" (quoteIfNeeded msg) |> sendToAppVeyor

// Add trace listener to track messages
if buildServer = BuildServer.AppVeyor then 
    { new ITraceListener with
          member this.Write msg = 
              match msg with
              | ErrorMessage x -> add x "Error"
              | ImportantMessage x -> add x "Warning"
              | LogMessage(x, _) -> add x "Information"
              | TraceMessage(x, _) -> 
                  if not enableProcessTracing then addNoCategory x
              | StartMessage | FinishedMessage | OpenTag(_, _) | CloseTag _ -> () }
    |> listeners.Add

/// Finishes the test suite.
let FinishTestSuite testSuiteName = () // Nothing in API yet

/// Starts the test suite.
let StartTestSuite testSuiteName = () // Nothing in API yet

/// Starts the test case.
let StartTestCase testSuiteName testCaseName = 
    sendToAppVeyor <| sprintf "AddTest \"%s\" -Outcome Running" (testSuiteName + " - " + testCaseName)

/// Reports a failed test.
let TestFailed testSuiteName testCaseName message details = 
    sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Failed -ErrorMessage %s -ErrorStackTrace %s" (testSuiteName + " - " + testCaseName)
        (EncapsulateSpecialChars message) (EncapsulateSpecialChars details)

/// Ignores the test case.      
let IgnoreTestCase testSuiteName testCaseName message = sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Passed" (testSuiteName + " - " + testCaseName)

/// Reports a succeeded test.
let TestSucceeded testSuiteName testCaseName = sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Passed" (testSuiteName + " - " + testCaseName)

/// Finishes the test case.
let FinishTestCase testSuiteName testCaseName (duration : System.TimeSpan) = 
    let duration = 
        duration.TotalMilliseconds
        |> round
        |> string

    sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Duration %s" (testSuiteName + " - " + testCaseName) duration

/// Union type representing the available test result formats accepted by AppVeyor.
type TestResultsType =
    | MsTest
    | NUnit
    | Xunit

/// Uploads the test results .xml file to the Test tab of the build console.
let UploadTestResultsXml testResultsType outputDir =
    if buildServer = BuildServer.AppVeyor then
        let resultsType = (sprintf "%A" testResultsType).ToLower()
        let url = sprintf "https://ci.appveyor.com/api/testresults/%s/%s" resultsType AppVeyorEnvironment.JobId
        let files = System.IO.Directory.GetFiles(path = outputDir, searchPattern = "*.xml")
        use wc = new System.Net.WebClient()
        files
        |> Seq.iter (fun file ->
            try
                wc.UploadFile(url, file) |> ignore
                printfn "Successfully uploaded test results %s" file
            with
            | ex -> printfn "An error occurred while uploading %s:\r\n%O" file ex
        )
