[<AutoOpen>]
module Fake.Core.IntegrationTests.TestHelpers

open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open System
open NUnit.Framework
open System
open System.IO

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
        let errors = String.Join(Environment.NewLine,result.Errors)
        printfn "%s" <| String.Join(Environment.NewLine,result.Messages)
        failwithf "FAKE Process exited with %d: %s" result.ExitCode errors
    String.Join(Environment.NewLine,result.Messages)

let directFake command scenario =
    directFakeInPath command (scenarioTempPath scenario) null

let fake command scenario =
    prepare scenario

    directFake command scenario
#if DEBUG
let fakeVerboseFlag = "--verbose"
#else
let fakeVerboseFlag = ""
#endif
let fakeRun scriptName scenario =
    fake (sprintf "%s run %s" fakeVerboseFlag scriptName) scenario |> ignore
