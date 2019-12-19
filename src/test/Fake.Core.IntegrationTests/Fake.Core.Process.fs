module Fake.Core.ProcessIntegrationTests

open Fake.IO
open Fake.Core
open Fake.DotNet
open System.IO
open Expecto
open Fake.Core.IntegrationTests.TestHelpers
open System.Diagnostics

let dllPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location)

let runProjectRaw noBuild proj args =
    [
        yield "run"
        if noBuild then yield "--no-build"
        yield! [ "--project"; proj; "--" ]
        yield! args |> Args.fromWindowsCommandLine |> Seq.toList
    ]
    |> runDotNetRaw
    
let runProject noBuild f proj args =
    runProjectRaw noBuild proj args
    |> f
    |> Proc.run

let runTestToolRaw noBuild name args =
    runProjectRaw noBuild (sprintf "%s/../../../../TestTools/%s" dllPath name) args

let runTestTool noBuild f name args =
    runTestToolRaw noBuild name args
    |> f
    |> Proc.run

let runCmdOrShRaw cmdArgs shArgs =
    let shell, args =
        if Environment.isWindows then
            "cmd", cmdArgs
        else
            "sh", shArgs 

    args
    |> Args.fromWindowsCommandLine |> Seq.toList
    |> CreateProcess.fromRawCommand shell
    //|> CreateProcess.withEnvironment (options.Environment |> Map.toList)
    //|> CreateProcess.withWorkingDirectory options.WorkingDirectory

let runCmdOrSh f cmdArgs shArgs =
    runCmdOrShRaw cmdArgs shArgs
    |> f
    |> Proc.run

let runEchoRaw text =
    runCmdOrShRaw (sprintf "/c echo %s" text) (sprintf "-c \"echo '%s'\"" text)

let runEcho f text = runEchoRaw text |> f |> Proc.run

let redirectNormal = CreateProcess.redirectOutput
let redirectAdvanced c =
    let results = new System.Collections.Generic.List<Fake.Core.ConsoleMessage>()
    let errorF msg =
        results.Add (ConsoleMessage.CreateError msg)

    let messageF msg =
        results.Add (ConsoleMessage.CreateOut msg)
    c 
    |> CreateProcess.redirectOutputIfNotRedirected
    |> CreateProcess.withOutputEventsNotNull messageF errorF
    |> CreateProcess.map (fun prev ->
        let stdOut = System.String.Join("\n", results |> Seq.choose (fun msg -> if msg.IsError then None else Some msg.Message))
        let stdErr = System.String.Join("\n", results |> Seq.choose (fun msg -> if msg.IsError then Some msg.Message else None))
        let r = { Output = stdOut; Error = stdErr }
        { ExitCode = prev.ExitCode; Result = r })

let redirectTestCases runs name f =
    let cases = 
        [ "regular redirect", redirectNormal
          "lazy redirect", redirectAdvanced ]
    testList name [
        for name, redirect in cases do
            yield testCase name <| fun _ ->
                for i in 1 .. runs do
                    f i redirect
    ]

let runs = 10

[<Tests>]
let tests =
    // warm-up and build
    runTestTool false redirectNormal "StandardOutputErrorTool" "" |> ignore
    let mem = new System.IO.MemoryStream()
    runTestToolRaw false "TeeTool" ""
        |> CreateProcess.withStandardInput (UseStream(false, mem)) 
        |> Proc.run
        |> ignore

    testList "Fake.Core.ProcessIntegrationTests" [
        redirectTestCases (runs / 10) "Make sure process with lots of output and error doesn't hang - #2401" <| fun run redirect ->
            
            let r = runTestTool true redirect "StandardOutputErrorTool" ""
            Expect.isGreaterThan r.Result.Error.Length 1000 (sprintf "Expected an error string (run '%d'), but was: %s" run r.Result.Error)
            Expect.isGreaterThan r.Result.Output.Length 1000 (sprintf "Expected an output string (run '%d'), but was: %s" run r.Result.Output)
            Expect.stringEnds r.Result.Error "ERR: 0123456789abcdefghijklmnopqrstuvwxyz, 99999" (sprintf "Last message was not found in standard error (run '%d'): %s" run r.Result.Error)
            Expect.stringEnds r.Result.Output "OUT: 0123456789abcdefghijklmnopqrstuvwxyz, 99999" (sprintf "Last message was not found in standard output (run '%d'): %s" run r.Result.Output)
            ()

        // From https://github.com/msugakov/CsharpRedirectStandardOutput/blob/master/RedirectStandardOutputLibrary.Tests
        redirectTestCases runs "Make sure cmd-echo works with regular redirect" <| fun run redirect ->
            let guid = System.Guid.NewGuid().ToString()
            let r = runEcho redirect guid
            //Expect.isGreaterThan r.Result.Error.Length 1000 (sprintf "Expected an error string, but was: %s" r.Result.Error)
            let output = r.Result.Output.Trim()
            let error = r.Result.Error.Trim()
            Expect.equal error "" (sprintf "standard error should be empty (run '%d'): %s" run error)
            Expect.equal output guid (sprintf "standard output should be the given guuid (run '%d'): %s" run output)
            ()

        testCase "ProcessUtils.findLocalTool doesn't fail on non-existent path, #2390" <| fun _ ->
            let p = ProcessUtils.findLocalTool "test_not_existing" "test_not_existing" [ "./not/existing/path" ]
            Expect.equal p "test_not_existing" "Expected findLocalTool to fallback to parameter"

        testCase "Pipe based redirect should work, #2445" <| fun _ ->
            let input = StreamRef.Empty
            let p1 =
                runTestToolRaw true "TeeTool" ""
                |> CreateProcess.withStandardInput (CreatePipe input)
                |> Proc.start
            let p2 =
                runEchoRaw "can not pipe output"
                |> CreateProcess.withStandardOutput (UseStream (true, input.Value))
                |> Proc.run
            p1.Wait()
    ]
