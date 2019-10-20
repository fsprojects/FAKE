module Fake.Core.ProcessIntegrationTests

open Fake.IO
open Fake.Core
open Fake.DotNet
open System.IO
open Expecto
open Fake.Core.IntegrationTests.TestHelpers

let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson

let dllPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location)

let runProject noBuild f proj args =
    let options = dotnetSdk.Value (DotNet.Options.Create())
    
    let dir = System.IO.Path.GetDirectoryName options.DotNetCliPath
    let oldPath =
        options
        |> Process.getEnvironmentVariable "PATH"
    
    let result =
        [
            yield "run"
            if noBuild then yield "--no-build"
            yield! [ "--project"; proj; "--" ]
            yield! args |> Args.fromWindowsCommandLine |> Seq.toList
        ]
        |> CreateProcess.fromRawCommand options.DotNetCliPath
        |> CreateProcess.withEnvironment (options.Environment |> Map.toList)
        |> CreateProcess.setEnvironmentVariable "PATH" (
            match oldPath with
            | Some oldPath -> sprintf "%s%c%s" dir System.IO.Path.PathSeparator oldPath
            | None -> dir)
        |> CreateProcess.withWorkingDirectory options.WorkingDirectory
        |> f
        |> Proc.run
    result

let runTestTool noBuild f name args =
    runProject noBuild f (sprintf "%s/../../../../TestTools/%s" dllPath name) args

let runCmd f args =
    let result =
        args |> Args.fromWindowsCommandLine |> Seq.toList
        |> CreateProcess.fromRawCommand "cmd"
        //|> CreateProcess.withEnvironment (options.Environment |> Map.toList)
        //|> CreateProcess.withWorkingDirectory options.WorkingDirectory
        |> f
        |> Proc.run
    result

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
            let r = runCmd redirect (sprintf "/c echo %s" guid)
            //Expect.isGreaterThan r.Result.Error.Length 1000 (sprintf "Expected an error string, but was: %s" r.Result.Error)
            let output = r.Result.Output.Trim()
            let error = r.Result.Error.Trim()
            Expect.equal error "" (sprintf "standard error should be empty (run '%d'): %s" run error)
            Expect.equal output guid (sprintf "standard output should be the given guuid (run '%d'): %s" run output)
            ()
    ]
