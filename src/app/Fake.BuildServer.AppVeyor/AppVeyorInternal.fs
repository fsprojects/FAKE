/// Contains support for various build servers
namespace Fake.BuildServer

open System.IO
open Fake.Core
open Fake.IO
open Fake.Net

module internal AppVeyorInternal =
    let environVar = Environment.environVar
    let getJobId () = environVar "APPVEYOR_JOB_ID"
    let internal sendToAppVeyor args =
        let argsList = Arguments.OfStartInfo args
        CreateProcess.fromCommand <| RawCommand("appveyor", argsList)
        |> CreateProcess.disableTraceCommand
        |> Proc.run
        |> ignore

    type MessageCategory =
        | Information
        | Warning
        | Error
        | NoCategory
        member x.AsString =
            match x with
            | Information -> Some "Information"
            | Warning -> Some "Warning"
            | Error -> Some "Error"
            | NoCategory -> None

    let AddMessage (category:MessageCategory) details msg =
        if not <| String.isNullOrEmpty msg then
            //let enableProcessTracingPreviousValue = Process.enableProcessTracing
            //Process.enableProcessTracing <- false
            try
                [ yield "AddMessage"
                  yield msg
                  match category.AsString with
                  | Some cat ->
                      yield "-Category"
                      yield cat
                  | None -> ()
                  if not (System.String.IsNullOrEmpty details) then
                      yield "-Details"
                      yield details ]
                |> Args.toWindowsCommandLine
                |> sendToAppVeyor
            with e ->
                // because otherwise there might be recursive failure...
                eprintfn "AppVeyor 'AddMessage' failed: %O" e             
            //sprintf "AddMessage %s -Category %s" (Process.quoteIfNeeded msg) (category.ToString())
            //Process.enableProcessTracing <- enableProcessTracingPreviousValue
    //let private addNoCategory msg = sprintf "AddMessage %s" (Process.quoteIfNeeded msg) |> sendToAppVeyor

    /// Starts the test case.
    let StartTestCase testSuiteName testCaseName =
        sendToAppVeyor <| sprintf "AddTest \"%s\" -Outcome Running" (testSuiteName + " - " + testCaseName)

    /// Updates test info
    let UpdateTest testSuiteName testCaseName outcome =
        sendToAppVeyor <| sprintf "UpdateTest %s -Outcome %s"
            (Process.quoteIfNeeded (testSuiteName + " - " + testCaseName)) outcome

    /// Updates test info
    let UpdateTestEx testSuiteName testCaseName outcome message stackTrace stdOut stdErr =
        sendToAppVeyor <| sprintf "UpdateTest %s -Outcome %s -ErrorMessage %s -ErrorStackTrace %s -StdOut %s -StdErr %s"
            (Process.quoteIfNeeded (testSuiteName + " - " + testCaseName)) outcome
            (Process.quoteIfNeeded message) (Process.quoteIfNeeded stackTrace)
            (Process.quoteIfNeeded stdOut) (Process.quoteIfNeeded stdErr)

    /// Reports a failed test.
    let TestFailed testSuiteName testCaseName message details =
        sendToAppVeyor <| sprintf "UpdateTest %s -Outcome Failed -ErrorMessage %s -ErrorStackTrace %s"
            (Process.quoteIfNeeded (testSuiteName + " - " + testCaseName))
            (Process.quoteIfNeeded message) (Process.quoteIfNeeded details)

    /// Ignores the test case.
    let IgnoreTestCase testSuiteName testCaseName message = sendToAppVeyor <| sprintf "UpdateTest \"%s\" -Outcome Ignored" (testSuiteName + " - " + testCaseName)

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
        | Xunit
        | NUnit
        | NUnit3
        | JUnit

    /// Uploads a test result file to make them visible in Test tab of the build console.
    let UploadTestResultsFile (testResultsType : TestResultsType) file =
        let resultsType = (sprintf "%A" testResultsType).ToLower()
        let url = sprintf "https://ci.appveyor.com/api/testresults/%s/%s" resultsType (getJobId())
        try
            Http.upload url file
            printfn "Successfully uploaded test results %s" file
        with
        | ex -> printfn "An error occurred while uploading %s:\r\n%O" file ex

    /// Uploads all the test results ".xml" files in a directory to make them visible in Test tab of the build console.
    let UploadTestResultsXml (testResultsType : TestResultsType) outputDir =
        System.IO.Directory.EnumerateFiles(path = outputDir, searchPattern = "*.xml")
        |> Seq.map(fun file -> async { UploadTestResultsFile testResultsType file })
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore

    /// Set environment variable
    let SetVariable name value =
        sendToAppVeyor <| sprintf "SetVariable -Name \"%s\" -Value \"%s\"" name value

    /// Type of artifact that is pushed
    type ArtifactType = Auto | WebDeployPackage

    /// AppVeyor parameters for artifact push as [described](https://www.appveyor.com/docs/build-worker-api/#push-artifact)
    type PushArtifactParams =
        {
            /// The full local path to the artifact
            Path: string
            /// File name to display in the artifact tab
            FileName: string
            /// Deployment name
            DeploymentName: string
            /// Type of the artifact
            Type: ArtifactType
        }

    /// AppVeyor artifact push default parameters
    let defaultPushArtifactParams =
        {
            Path = ""
            FileName = ""
            DeploymentName = ""
            Type = Auto
        }

    let internal appendArgIfNotNullOrEmpty value name builder =
        if (String.isNotNullOrEmpty value) then
            StringBuilder.appendWithoutQuotes (sprintf "-%s \"%s\"" name value) builder
        else
            builder

    /// Push an artifact
    let PushArtifact (setParams : PushArtifactParams -> PushArtifactParams) =
        let parameters = setParams defaultPushArtifactParams
        new System.Text.StringBuilder()
        |> StringBuilder.append "PushArtifact"
        |> StringBuilder.append parameters.Path
        |> appendArgIfNotNullOrEmpty parameters.FileName "FileName"
        |> appendArgIfNotNullOrEmpty parameters.DeploymentName "DeploymentName"
        |> appendArgIfNotNullOrEmpty (sprintf "%A" parameters.Type) "Type"
        |> StringBuilder.toText
        |> sendToAppVeyor

    /// Push multiple artifacts
    let PushArtifacts paths =
        for path in paths do
            PushArtifact (fun p -> { p with Path = path; FileName = Path.GetFileName(path) })

