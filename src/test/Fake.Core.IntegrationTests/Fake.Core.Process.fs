module Fake.Core.ProcessIntegrationTests

open Fake.IO
open Fake.Core
open Fake.DotNet
open System.IO
open Expecto
open Fake.Core.IntegrationTests.TestHelpers

let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson

let dllPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location)

let runProject proj args =
    let options = dotnetSdk.Value (DotNet.Options.Create())
    
    let dir = System.IO.Path.GetDirectoryName options.DotNetCliPath
    let oldPath =
        options
        |> Process.getEnvironmentVariable "PATH"
    
    let result =
        [
            yield! [ "run"; "--project"; proj; "--" ]
            yield! args |> Args.fromWindowsCommandLine |> Seq.toList
        ]
        |> CreateProcess.fromRawCommand options.DotNetCliPath 
        |> CreateProcess.redirectOutput
        |> CreateProcess.withEnvironment (options.Environment |> Map.toList)
        |> CreateProcess.setEnvironmentVariable "PATH" (
            match oldPath with
            | Some oldPath -> sprintf "%s%c%s" dir System.IO.Path.PathSeparator oldPath
            | None -> dir)
        |> CreateProcess.withWorkingDirectory options.WorkingDirectory
        |> Proc.run
    result

let runTestTool name args =
    runProject (sprintf "%s/../../../../TestTools/%s" dllPath name) args

[<Tests>]
let tests =
    testList "Fake.Core.ProcessIntegrationTests" [
        testCase "Make sure process with lots of output and error doesn't hang - #2401" <| fun _ ->
            
            let r = runTestTool "StandardOutputErrorTool" ""
            Expect.isGreaterThan r.Result.Error.Length 1000 (sprintf "Expected an error string, but was: %s" r.Result.Error)
            Expect.isGreaterThan r.Result.Output.Length 1000 (sprintf "Expected an output string, but was: %s" r.Result.Output)
            Expect.stringEnds r.Result.Error "ERR: 0123456789abcdefghijklmnopqrstuvwxyz, 99999" (sprintf "Last message was not found in standard error: %s" r.Result.Error)
            Expect.stringEnds r.Result.Output "OUT: 0123456789abcdefghijklmnopqrstuvwxyz, 99999" (sprintf "Last message was not found in standard output: %s" r.Result.Output)
            ()
    ]
