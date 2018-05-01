[<AutoOpen>]
module Fake.Core.IntegrationTests.TestHelpers

open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open System
open NUnit.Framework
open System
open System.IO

exception FakeExecutionFailed of ProcessResult
  with
    override x.ToString() =
        let result = x.Data0
        let stdErr = String.Join(Environment.NewLine,result.Errors)
        let stdOut = String.Join(Environment.NewLine,result.Messages)
        sprintf "FAKE Process exited with %d:\n%s\nStdout: \n%s" result.ExitCode stdErr stdOut


let fakeToolPath = 
    let rawWithoutExtension = Path.getFullName(__SOURCE_DIRECTORY__ + "../../../../nuget/dotnetcore/Fake.netcore/current/fake")
    if Environment.isUnix then rawWithoutExtension
    else rawWithoutExtension + ".exe"
let integrationTestPath = Path.getFullName(__SOURCE_DIRECTORY__ + "../../../../integrationtests")
let scenarioTempPath scenario = integrationTestPath @@ scenario @@ "temp"
let originalScenarioPath scenario = integrationTestPath @@ scenario @@ "before"

let prepare scenario =
    let originalScenarioPath = originalScenarioPath scenario
    let scenarioPath = scenarioTempPath scenario
    if Directory.Exists scenarioPath then
      Directory.Delete(scenarioPath, true)
    Directory.ensure scenarioPath
    Shell.CopyDir scenarioPath originalScenarioPath (fun _ -> true)

let directFakeInPath command scenarioPath target =
    let result =
        Process.execWithResult (fun (info:Process.ProcStartInfo) ->
          { info with
                FileName = fakeToolPath
                WorkingDirectory = scenarioPath
                Arguments = command }
          |> Process.setEnvironmentVariable "target" target
          |> Process.setEnvironmentVariable "FAKE_DETAILED_ERRORS" "true") (System.TimeSpan.FromMinutes 15.)
    if result.ExitCode <> 0 then
        raise <| FakeExecutionFailed(result)
    result

let handleAndFormat f =
    try
        f()
    with FakeExecutionFailed(result) ->
        let stdOut = String.Join("\n", result.Messages).Trim()
        let stdErr = String.Join("\n", result.Errors)
        Assert.Fail(
            sprintf "fake.exe failed with code %d\nOut: %s\nError: %s" result.ExitCode stdOut stdErr)
        reraise() // for return value
let directFake command scenario =
    directFakeInPath command (scenarioTempPath scenario) null

let fake command scenario =
    prepare scenario

    directFake command scenario
//let fakeFlags = "--verbose"
let fakeFlags = "--silent"
let fakeRun runArgs scenario =
    fake (sprintf "%s run %s" fakeFlags runArgs) scenario
