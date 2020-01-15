module Fake.Core.IntegrationTests.SimpleHelloWorldTests

open Expecto
open Expecto.Flip
open System
open System
open System.IO
open System.Diagnostics
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Fake.Core

let fail s = Expect.isTrue s false
let ignProc = ignore<Fake.Core.ProcessResult>



type Declaration =
    { File : string
      Line : int
      Column : int 
      ErrorDetail : string }
    static member Empty =
        { File = ""; Line = 0; Column = 1; ErrorDetail = ""}
/// a target dependency, either a hard or a soft dependency.
type Dependency =
    { Name : string
      Declaration : Declaration }
/// a FAKE target, its description and its relations to other targets (dependencies), including the declaration lines of the target and the dependencies.
type Target =
    { Name : string
      HardDependencies : Dependency list
      SoftDependencies : Dependency list
      Declaration : Declaration
      Description : string }

let getInfoVersion () =
    let attr = typeof<Fake.Core.Process.ProcessList>.Assembly.GetCustomAttributes(typeof<System.Reflection.AssemblyInformationalVersionAttribute>, false)
    match attr |> Seq.tryHead with
    | Some (:? Reflection.AssemblyInformationalVersionAttribute as attr) -> attr.InformationalVersion
    | _ -> failwithf "Could not retrieve version"

let skipIfNoVersion () =
    if getInfoVersion() = "1.0.0" then
        skiptestf "This test is skipped as 1.0.0 is probably not a valid version and this test requires the current version of the package. Use 'fake build -t DotNetCoreIntegrationTests' to run this test."

[<Tests>]
let tests = 
  testList "Fake.Core.IntegrationTests" [
    testCase "fake-cli local tool works, #2425" <| fun _ ->
        skipIfNoVersion ()
        let scenario = "i002425-dotnet-cli-tool-works"
        prepare scenario
        let scenarioPath = resolvePath scenario ""
        let version = getInfoVersion ()
        // dotnet tool install --version 5.19.0-alpha.local.1 fake-cli --add-source /e/Projects/FAKE/release/dotnetcore/
        [
            yield! ["tool"; "install"; "--version"; version; "fake-cli"; "--add-source"; releaseDotnetCoreDir ]
        ]
        |> runDotNetRaw
        |> CreateProcess.withWorkingDirectory scenarioPath
        |> CreateProcess.ensureExitCode
        |> CreateProcess.map ignore
        |> Proc.run

        let output =
            [
                yield! ["fake"; "--version" ]
            ]
            |> runDotNetRaw
            |> CreateProcess.withWorkingDirectory scenarioPath
            |> CreateProcess.redirectOutput
            |> CreateProcess.ensureExitCode
            |> Proc.run
        
        Expect.stringContains "Expected version in stderror string" version output.Result.Error 
        Expect.stringContains "Expected Fake.Runtime.dll in stderror string" "Fake.Runtime.dll" output.Result.Error


    testCase "no dependencies hello world and casing #2314" <| fun _ ->
        let result =
            if Paket.Utils.isWindows then
                // #2314
                fakeRunAndCheck "HELLO_world.fsx" "HELLO_world.fsx" "core-no-dependencies-hello-world"
                |> ignProc
                directFake "--silent run hello_world.fsx" "core-no-dependencies-hello-world"
            else fakeRunAndCheck "hello_world.fsx" "hello_world.fsx" "core-no-dependencies-hello-world"
        let stdOut = String.Join("\n", result.Messages)
        let stdErr = String.Join("\n", result.Errors)

        stdOut.Trim() |> Expect.equal "Hello FAKE exected" "Hello FAKE"
        stdErr.Trim() |> Expect.equal "empty exected" ""

    testCase "simple failed to compile" <| fun _ ->
        let result =
            expectFailure "Expected an compilation error and a nonzero exit code!" (fun _ ->
                fakeRunAndCheck "fail-to-compile.fsx" "fail-to-compile.fsx" "core-simple-failed-to-compile" |> ignProc)

        let stdOut = String.Join("\n", result.Messages)
        let stdErr = String.Join("\n", result.Errors)
        stdErr.Contains("klajsdhgfasjkhd")
            |> Expect.isTrue (sprintf "Standard Error Output should contain 'klajsdhgfasjkhd', but was: '%s', Out: '%s'" stdErr stdOut)

        checkIntellisense "fail-to-compile.fsx" "core-simple-failed-to-compile"

    testCase "simple runtime error" <| fun _ ->
        let result =
            expectFailure "Expected an runtime error and a nonzero exit code!" (fun _ ->
                fakeRunAndCheck "runtime-error.fsx" "runtime-error.fsx" "core-simple-runtime-error" |> ignProc)

        let stdOut = String.Join("\n", result.Messages)
        let stdErr = String.Join("\n", result.Errors)
        stdErr.Contains("runtime error")
            |> Expect.isTrue (sprintf "Standard Error Output should contain 'runtime error', but was: '%s', Out: '%s'" stdErr stdOut)
        checkIntellisense "runtime-error.fsx" "core-simple-runtime-error"

    testCase "reference fake runtime" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheck "reference_fake-runtime.fsx" "reference_fake-runtime.fsx" "core-reference-fake-runtime" |> ignProc

    testCase "context exists" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheck "context.exists.fsx" "context.exists.fsx" "core-context-exists" |> ignProc

    testCase "use external paket.dependencies" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheck "use_external_dependencies.fsx" "use_external_dependencies.fsx" "core-use-external-paket-dependencies" |> ignProc

    testCase "reference fake core targets" <| fun _ ->
        let result =
            handleAndFormat <| fun () -> fakeRunAndCheck "reference_fake-targets.fsx" "reference_fake-targets.fsx --test" "core-reference-fake-core-targets"
        let stdOut = String.Join("\n", result.Messages).Trim()
        let stdErr = String.Join("\n", result.Errors)

        let expected = "Arguments: [\"--test\"]"
        stdOut.Contains expected
            |> Expect.isTrue (sprintf "stdout should contain '%s', but was: '%s'" expected stdOut)
        let expected = "GlobalArgs: [|\"--test\"|]"
        stdOut.Contains expected
            |> Expect.isTrue (sprintf "stdout should contain '%s', but was: '%s'" expected stdOut)
        stdErr.Trim() |> Expect.equal "empty exected" ""

        // Check if --write-info <file> works
        let tempFile = Path.GetTempFileName()
        try
            let tmpPath = scenarioTempPath "core-reference-fake-core-targets"
            let scriptFile = Path.Combine(tmpPath, "reference_fake-targets.fsx")
            let otherScriptFile = Path.Combine(tmpPath, "otherscript.fsx")
            let otherFileFile = Path.Combine(tmpPath, "otherfile.fs")
            handleAndFormat <| fun () ->
                directFake (sprintf "run --fsiargs \"--debug:portable --optimize-\" reference_fake-targets.fsx -- --write-info \"%s\"" tempFile) "core-reference-fake-core-targets" |> ignProc
            let json = File.ReadAllText tempFile
            let obj = JObject.Parse json
            let targets = obj.["targets"] :?> JArray
            let parseDecl (t:JToken) =
                { File = string t.["file"]; Line = int t.["line"]; Column = int t.["column"]; ErrorDetail = string t.["errorDetail"] }
            let parseDep (t:JToken) =
                { Name = string t.["name"]; Declaration = parseDecl t.["declaration"] }
            let parseArray parseItem (a:JToken) =
                (a :?> JArray)
                |> Seq.map parseItem
                |> Seq.toList
            let parseTarget (t:JToken) =
                { Name = string t.["name"]; Declaration = parseDecl t.["declaration"]
                  HardDependencies = parseArray parseDep t.["hardDependencies"]
                  SoftDependencies = parseArray parseDep t.["softDependencies"]
                  Description = string t.["description"] }

            let dict =
                targets |> Seq.map (fun t -> let t = parseTarget t in t.Name, t) |> dict

            Expect.equal "Expected correct number of targets" 4 dict.Count

            let startTarget = dict.["Start"]
            Expect.equal "Expected correct declaration of 'Start'" { Declaration.Empty with File = scriptFile; Line = 36 } startTarget.Declaration
            Expect.equal "Expected correct hard dependencies of 'Start'" [] startTarget.HardDependencies
            Expect.equal "Expected correct soft dependencies of 'Start'" [] startTarget.SoftDependencies
            Expect.equal "Expected correct description of 'Start'" "Test description" startTarget.Description
            let testTarget = dict.["TestTarget"]
            Expect.equal "Expected correct declaration of 'TestTarget'" { Declaration.Empty with File = scriptFile; Line = 38 } testTarget.Declaration
            Expect.equal "Expected correct hard dependencies of 'TestTarget'" [ { Name = "Start"; Declaration = { Declaration.Empty with File = scriptFile; Line = 45 } } ] testTarget.HardDependencies
            Expect.equal "Expected correct description of 'TestTarget'" "" testTarget.Description
            let scriptTarget = dict.["OtherScriptTarget"]
            Expect.equal "Expected correct declaration of 'OtherScriptTarget'" { Declaration.Empty with File = otherScriptFile; Line = 4 } scriptTarget.Declaration
            Expect.equal "Expected correct hard dependencies of 'OtherScriptTarget'" [ ] scriptTarget.HardDependencies
            Expect.equal "Expected correct description of 'OtherScriptTarget'" "" scriptTarget.Description
            let fileTarget = dict.["OtherFileTarget"]
            Expect.equal "Expected correct declaration of 'OtherFileTarget'" { Declaration.Empty with File = otherFileFile; Line = 7; Column = 5; } fileTarget.Declaration
            Expect.equal "Expected correct hard dependencies of 'OtherFileTarget'" [ ] fileTarget.HardDependencies
            Expect.equal "Expected correct description of 'OtherFileTarget'" "" fileTarget.Description
        finally
            try File.Delete tempFile with e -> ()


    testCase "issue #2025" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheckInPath "build.fsx" "build.fsx" "i002025" "script" |> ignProc

    testCase "issue #2007 - native libs work" <| fun _ ->
        // should "just" work
        handleAndFormat <| fun () ->
            fakeRunAndCheck "build.fsx" "build.fsx" "i002007-native-libs" |> ignProc

        // Should tell FAKE error story
        let result =     
            expectFailure "Expected missing entrypoint error" <| fun () ->
                directFake (sprintf "run build.fsx -t FailWithMissingEntry") "i002007-native-libs" |> ignProc
        let stdOut = String.Join("\n", result.Messages).Trim()
        let stdErr = String.Join("\n", result.Errors)
        (stdErr.Contains "Fake_ShouldNotExistExtryPoint" && stdErr.Contains "EntryPointNotFoundException:")
            |> Expect.isTrue (sprintf "Standard Error Output should contain 'Fake_ShouldNotExistExtryPoint' and 'EntryPointNotFoundException:', but was: '%s', Out: '%s'" stdErr stdOut)
        
        let result =     
            expectFailure "Expected missing entrypoint error" <| fun () ->
                directFake (sprintf "run build.fsx -t FailWithUnknown") "i002007-native-libs" |> ignProc
        let stdOut = String.Join("\n", result.Messages).Trim()
        let stdErr = String.Join("\n", result.Errors)
        (stdErr.Contains "unknown_dependency.dll" && stdErr.Contains "DllNotFoundException:")
            |> Expect.isTrue (sprintf "Standard Error Output should contain 'unknown_dependency.dll' and 'DllNotFoundException:', but was: '%s', Out: '%s'" stdErr stdOut)
        // TODO: enable instead of the above
        //stdErr.Contains("Could not resolve native library 'unknown_dependency.dll'")
        //    |> Expect.isTrue (sprintf "Standard Error Output should contain \"Could not resolve native library 'unknown_dependency.dll'\", but was: '%s', Out: '%s'" stdErr stdOut)
        //stdErr.Contains("This can happen for various reasons")
        //    |> Expect.isTrue (sprintf "Standard Error Output should contain \"This can happen for various reasons\", but was: '%s', Out: '%s'" stdErr stdOut)

  ]
