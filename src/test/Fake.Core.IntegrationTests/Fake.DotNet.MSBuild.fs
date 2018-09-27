module Fake.DotNet.MSBuildIntegrationTests

open System
open Fake.Core
open Fake.DotNet
open Expecto

let buildWithRedirect setParams project =
    let msBuildParams, argsString = MSBuild.buildArgs setParams

    let args = Process.toParam project + " " +  argsString

    // used for detection
    let callMsBuildExe args =
        let result =
            Process.execWithResult (fun info ->
            { info with
                FileName = msBuildParams.ToolPath
                Arguments = args }
            |> Process.setEnvironment msBuildParams.Environment) TimeSpan.MaxValue
        if not result.OK then
            failwithf "msbuild failed with exitcode '%d'" result.ExitCode
        String.Join("\n", result.Messages)

    let binlogPath, args = MSBuild.addBinaryLogger msBuildParams.ToolPath callMsBuildExe args msBuildParams.DisableInternalBinLog
    let wd =
        if msBuildParams.WorkingDirectory = System.IO.Directory.GetCurrentDirectory()
        then ""
        else sprintf "%s>" msBuildParams.WorkingDirectory
    Trace.tracefn "%s%s %s" wd msBuildParams.ToolPath args

    let result =
        Process.execWithResult (fun info ->
        { info with
            FileName = msBuildParams.ToolPath
            WorkingDirectory = msBuildParams.WorkingDirectory
            Arguments = args }
        |> Process.setEnvironment msBuildParams.Environment) TimeSpan.MaxValue
    try 
        MSBuild.handleAfterRun "msbuild" binlogPath result.ExitCode project
        Choice1Of2 result
    with e -> Choice2Of2 (e, result)

let simplePropertyTest propValue =
    let dllPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location)
    let setParams (defaults:MSBuildParams) =
        { defaults with
            Verbosity = Some(MSBuildVerbosity.Minimal)
            Targets = ["Test"]
            NoLogo = true
            Properties =
                [
                    "Property1", propValue
                ]
            WorkingDirectory = dllPath
        }

    match buildWithRedirect setParams "testdata/testProperty.proj" with
    | Choice1Of2 result ->
        let lines = String.Join("\n", result.Results |> Seq.map (fun r -> r.Message))
        Expect.stringContains lines (sprintf "$Property1: '%s'" propValue) "Expected to find property value in msbuild output"
        Expect.stringContains lines "$Property2: ''" "Expected to find empty Property2"
    | Choice2Of2 (e, result) ->
        let lines = String.Join("\n", result.Results |> Seq.map (fun r -> sprintf "%s: %s" (if r.IsError then "stderr" else "stdout") r.Message))
        raise <| exn(sprintf "simplePropertyTest failed, msbuild output was: \n%s\n" lines, e)
    

[<Tests>]
let tests =
  testList "Fake.DotNet.MSBuild.IntegrationTests" [
    testCase "#2112" <| fun _ ->
        let value = "Data Source=xxx,1433;Initial Catalog=xxx;User Id=xxx;Password=xxx;Integrated Security=False;Persist Security Info=True;Connect Timeout=30;Encrypt=True;MultipleActiveResultSets=True"
        simplePropertyTest value
    testCase "#2112 (2)" <| fun _ ->
        let value = "=asd?*&(($!&%%_^$#_+=-['}{|\';\\'\"\\\"ad"
        simplePropertyTest value
  ]
