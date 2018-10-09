module Fake.DotNet.MSBuildIntegrationTests

open System
open Fake.Core
open Fake.DotNet
open Expecto

// Use `dotnet msbuild` for now as it will be the same version across all CI servers
let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson

let inline opts () = DotNet.Options.lift dotnetSdk.Value

let inline dtntWorkDir wd =
    DotNet.Options.lift dotnetSdk.Value
    >> DotNet.Options.withWorkingDirectory wd
    >> DotNet.Options.withRedirectOutput true

let simplePropertyTest propValue =
    let dllPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location)
    let setMSBuildParams (defaults:MSBuild.CliArguments) =
        { defaults with
            Verbosity = Some(MSBuildVerbosity.Minimal)
            Targets = ["Test"]
            NoLogo = true
            Properties =
                [
                    "Property1", propValue
                ]
        }
    let setParams (p:DotNet.MSBuildOptions) =
        p.WithMSBuildParams setMSBuildParams
        |> dtntWorkDir dllPath


    match DotNet.msbuildWithResult setParams "testdata/testProperty.proj" with
    | Choice1Of2 result ->
        let lines = String.Join("\n", result.Results |> Seq.map (fun r -> r.Message))
        Expect.stringContains lines (sprintf "$Property1: '%s'" propValue) "Expected to find property value in msbuild output"

        // This happens on xbuild?
        //if Environment.isWindows then
        //else
        //    // TODO: Report me as msbuild bug?
        //    let fixedPropValue = propValue.Replace("\\", "/")
        //    Expect.stringContains lines (sprintf "$Property1: '%s'" fixedPropValue) "Expected to find property value in msbuild output"
        Expect.stringContains lines "$Property2: ''" "Expected to find empty Property2"
    | Choice2Of2 (e, result) ->
        let lines = String.Join("\n", result.Results |> Seq.map (fun r -> sprintf "%s: %s" (if r.IsError then "stderr" else "stdout") r.Message))
        raise <| exn(sprintf "simplePropertyTest failed, msbuild output was: \n%s\n" lines, e)
    

let simplPathTest pathCased propValue =
    let dllPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location)
    let setMSBuildParams (defaults:MSBuild.CliArguments) =
        { defaults with
            Verbosity = Some(MSBuildVerbosity.Minimal)
            Targets = ["Test"]
            NoLogo = true
            Properties =
                [
                ]
        }
    let setParams (p:DotNet.MSBuildOptions) =
        p.WithMSBuildParams setMSBuildParams
        |> dtntWorkDir dllPath
        |> Process.setEnvironmentVariable pathCased propValue

    let expected propValue =
        let options = setParams (DotNet.MSBuildOptions.Create())
        let dir = System.IO.Path.GetDirectoryName options.Common.DotNetCliPath
        sprintf "%s%c%s" dir System.IO.Path.PathSeparator propValue

    match DotNet.msbuildWithResult setParams "testdata/testPath.proj" with
    | Choice1Of2 result ->
        let lines = String.Join("\n", result.Results |> Seq.map (fun r -> r.Message))
        if Environment.isWindows || pathCased = "PATH" then
            Expect.stringContains lines (sprintf "$PATH: '%s'" (expected propValue)) "Expected to find property value in msbuild output"
        else
            let va = Environment.GetEnvironmentVariable ("PATH")
            Expect.stringContains lines (sprintf "$PATH: '%s'" (expected va)) "Expected to find property value in msbuild output"

    | Choice2Of2 (e, result) ->
        let lines = String.Join("\n", result.Results |> Seq.map (fun r -> sprintf "%s: %s" (if r.IsError then "stderr" else "stdout") r.Message))
        raise <| exn(sprintf "simplePropertyTest failed, msbuild output was: \n%s\n" lines, e)
    
[<Tests>]
let tests =
  testList "Fake.DotNet.MSBuild.IntegrationTests" [
    Process.setEnableProcessTracing true
    let prefixPath =
        if Environment.isWindows then ""
        else
            match Process.tryFindFileOnPath "sh" with
            | Some shPath ->
                System.IO.Path.GetDirectoryName shPath + string System.IO.Path.PathSeparator
            | None -> failwithf "sh is required for msbuild"
    yield testCase "#2134" <| fun _ ->
        let value = 
            if Environment.isWindows
            then "C:\\Test\\Dir"
            else "/some/root/path"
        simplPathTest "Path" (prefixPath + value)
    yield testCase "#2134 (2)" <| fun _ ->
        let value = 
            if Environment.isWindows
            then "C:\\Test\\Dir"
            else "/some/root/path"
        simplPathTest "PATH" (prefixPath + value)
    yield testCase "#2134 (3)" <| fun _ ->
        let value = 
            if Environment.isWindows
            then "C:\\Test\\Dir"
            else "/some/root/path"
        simplPathTest "path" (prefixPath + value)
    yield testCase "#2112" <| fun _ ->
        let value = "Data Source=xxx,1433;Initial Catalog=xxx;User Id=xxx;Password=xxx;Integrated Security=False;Persist Security Info=True;Connect Timeout=30;Encrypt=True;MultipleActiveResultSets=True"
        simplePropertyTest value
    yield testCase "#2112 (2)" <| fun _ ->
        let value = "=asd?*&(($!&%%_^$#_+=-['}{|\';\\'\"\\\"ad"
        simplePropertyTest value
  ]
