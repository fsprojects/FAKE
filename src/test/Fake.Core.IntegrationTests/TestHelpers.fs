[<AutoOpen>]
module Fake.Core.IntegrationTests.TestHelpers

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open System
//open NUnit.Framework
open Expecto
open Expecto.Flip
open System
open System.IO

type TestDir =
    { Dir : string }
    interface System.IDisposable with
        member x.Dispose() =
            try
                Directory.Delete(x.Dir, true)
            with e ->
                eprintf "Failed to delete '%s': %O" x.Dir e
                ()

let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson

let runDotNetRaw args =
    let options = dotnetSdk.Value (Fake.DotNet.DotNet.Options.Create())

    let dir = System.IO.Path.GetDirectoryName options.DotNetCliPath
    let oldPath =
        options
        |> Process.getEnvironmentVariable "PATH"

    args
    |> CreateProcess.fromRawCommand options.DotNetCliPath
    |> CreateProcess.withEnvironment (options.Environment |> Map.toList)
    |> CreateProcess.setEnvironmentVariable "PATH" (
        match oldPath with
        | Some oldPath -> sprintf "%s%c%s" dir System.IO.Path.PathSeparator oldPath
        | None -> dir)
    |> CreateProcess.withWorkingDirectory options.WorkingDirectory

let createTestDir () =
    let testFile = Path.combine (Path.GetTempPath ()) (Path.GetRandomFileName ())
    Directory.CreateDirectory(testFile)
        |> ignore<DirectoryInfo>
    { Dir = testFile }

let testDirLocation = System.IO.Path.GetDirectoryName (typeof<TestDir>.Assembly.Location)

let createTestDirInCurrent () =
    let folder = testDirLocation </> ((Guid.NewGuid ()).ToString())
    Directory.CreateDirectory folder 
        |> ignore<DirectoryInfo>
    { Dir = folder }

let getTestFile testFile =
    Path.Combine(testDirLocation, "testdata", testFile)

exception FakeExecutionFailed of ProcessResult
  with
    override x.ToString() =
        let result = x.Data0
        let stdErr = String.Join(Environment.NewLine,result.Errors)
        let stdOut = String.Join(Environment.NewLine,result.Messages)
        sprintf "FAKE Process exited with %d:\n%s\nStdout: \n%s" result.ExitCode stdErr stdOut

let fakeRootPath = Path.getFullName(__SOURCE_DIRECTORY__ + "../../../../")
let releaseDir = Path.getFullName(fakeRootPath + "/release")
let releaseDotnetCoreDir = Path.getFullName(releaseDir + "/dotnetcore")

let fakeToolPath = 
    let rawWithoutExtension = Path.getFullName(releaseDir + "/dotnetcore/Fake.netcore/current/fake")
    if Environment.isUnix then rawWithoutExtension
    else rawWithoutExtension + ".exe"
let integrationTestPath = Path.getFullName(__SOURCE_DIRECTORY__ + "../../../../integrationtests")
let scenarioTempPath scenario = integrationTestPath @@ scenario @@ "temp"
let originalScenarioPath scenario = integrationTestPath @@ scenario @@ "before"

let resolvePath scenario (path:string) =
    if Path.IsPathRooted path then path
    else scenarioTempPath scenario @@ path

let prepare scenario =
    let originalScenarioPath = originalScenarioPath scenario
    let scenarioPath = scenarioTempPath scenario
    if Directory.Exists scenarioPath then
      Directory.Delete(scenarioPath, true)
    Directory.ensure scenarioPath
    Shell.copyDir scenarioPath originalScenarioPath (fun file ->
        // this should be in sync with integrationtests/.gitignore, but CI should ensure that
        let fp = Path.GetFullPath file
        let scfp = Path.GetFullPath scenarioPath
        let isFakeTmp = fp.Substring(scfp.Length).Contains ".fake"
        let isLockFile = fp.EndsWith ".lock"
        not isFakeTmp && not isLockFile)

let directFakeInPath command workingDir target =
    let result =
        Process.execWithResult (fun (info:ProcStartInfo) ->
          { info with
                FileName = fakeToolPath
                WorkingDirectory = workingDir
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
        Expect.isTrue (sprintf "fake.exe failed with code %d\nOut: %s\nError: %s" result.ExitCode stdOut stdErr) false
        reraise() // for return value

let expectFailure msg f =
    try
        f()
        Expect.isTrue msg false
        failwithf "%s" msg
    with FakeExecutionFailed(result) ->
        result


let directFake command scenario =
    directFakeInPath command (scenarioTempPath scenario) null

let fakeInPath command scenario path =
    prepare scenario

    directFakeInPath command (resolvePath scenario path) null

let fake command scenario =
    fakeInPath command scenario (scenarioTempPath scenario)

//let fakeFlags = "--verbose"
let fakeFlags = "--silent"

let fakeRunInPath runArgs scenario path =
    fakeInPath (sprintf "%s run %s" fakeFlags runArgs) scenario path
 
let fakeRun runArgs scenario =
    fakeRunInPath runArgs scenario (scenarioTempPath scenario)

let checkIntellisenseInPath scriptName path =
    let cachePath = path </> ".fake" </> scriptName
    File.Exists (cachePath </> "intellisense.fsx")
        |> Expect.isTrue "Expect intellisense.fsx to exist"
    File.Exists (cachePath </> "intellisense_lazy.fsx")
        |> Expect.isTrue "Expect intellisense_lazy.fsx to exist"
    let lines = File.ReadAllLines(cachePath </> "intellisense.fsx") |> Seq.toList
    let expected =
       [ "// This file is automatically generated by FAKE"
         "// This file is needed for IDE support only"
         "#if !FAKE"
         sprintf "#load \"%s\"" Fake.Runtime.Runners.loadScriptLazyName
         "#endif" ]
    Expect.equal "intellisense.fsx should be forwarding" expected lines

let checkIntellisense scriptName scenario =
    let scenarioPath = scenarioTempPath scenario
    checkIntellisenseInPath scriptName scenarioPath

let fakeRunAndCheckInPath scriptName runArgs scenario path =
    let result = fakeRunInPath runArgs scenario path
    checkIntellisenseInPath scriptName (resolvePath scenario path)
    result

let fakeRunAndCheck scriptName runArgs scenario =
    fakeRunAndCheckInPath scriptName runArgs scenario (scenarioTempPath scenario)
