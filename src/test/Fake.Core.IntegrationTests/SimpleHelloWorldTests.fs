module Fake.Core.IntegrationTests.SimpleHelloWorldTests

open Expecto
open Expecto.Flip
open System
open System
open System.IO
open System.Diagnostics
open Newtonsoft.Json
open Newtonsoft.Json.Linq

let fail s = Expect.isTrue s false
let ignProc = ignore<Fake.Core.ProcessResult>


type Declaration =
    { File : string
      Line : int
      Column : int }
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


[<Tests>]
let tests = 
  testList "Fake.Core.IntegrationTests" [
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
        try
            fakeRunAndCheck "fail-to-compile.fsx" "fail-to-compile.fsx" "core-simple-failed-to-compile" |> ignProc
            fail "Expected an compilation error and a nonzero exit code!"
        with 
        | FakeExecutionFailed(result) ->
            let stdOut = String.Join("\n", result.Messages)
            let stdErr = String.Join("\n", result.Errors)
            stdErr.Contains("klajsdhgfasjkhd")
                |> Expect.isTrue (sprintf "Standard Error Output should contain 'klajsdhgfasjkhd', but was: '%s', Out: '%s'" stdErr stdOut)

            checkIntellisense "fail-to-compile.fsx" "core-simple-failed-to-compile"

    testCase "simple runtime error" <| fun _ ->
        try
            fakeRunAndCheck "runtime-error.fsx" "runtime-error.fsx" "core-simple-runtime-error" |> ignProc
            fail "Expected an runtime error and a nonzero exit code!"
        with
        | FakeExecutionFailed(result) ->
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
        stdErr.Trim() |> Expect.equal "empty exected" ""

        // Check if --write-info <file> works
        let tempFile = Path.GetTempFileName()
        try
            handleAndFormat <| fun () ->
                directFake (sprintf "run --fsiargs \"--debug:portable --optimize-\" reference_fake-targets.fsx -- --write-info \"%s\"" tempFile) "core-reference-fake-core-targets" |> ignProc
            let json = File.ReadAllText tempFile
            let obj = JObject.Parse json
            let targets = obj.["targets"] :?> JArray
            let parseDecl (t:JToken) =
                { File = string t.["file"]; Line = int t.["line"]; Column = int t.["column"] }
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

            Expect.equal "Expected correct number of targets" 2 dict.Count

            let startTarget = dict.["Start"]
            Expect.equal "Expected correct declaration of 'Start'" startTarget.Declaration { File = ""; Line = 25; Column = 1 }
            Expect.equal "Expected correct hard dependencies of 'Start'" startTarget.HardDependencies []
            Expect.equal "Expected correct soft dependencies of 'Start'" startTarget.SoftDependencies []
            Expect.equal "Expected correct description of 'Start'" startTarget.Description "Test description"
            let testTarget = dict.["TestTarget"]
            Expect.equal "Expected correct declaration of 'TestTarget'" testTarget.Declaration { File = ""; Line = 27; Column = 1 }
            Expect.equal "Expected correct hard dependencies of 'TestTarget'" testTarget.HardDependencies [ { Name = "Start"; Declaration = { File = ""; Line = 34; Column = 1 } } ]
            Expect.equal "Expected correct description of 'TestTarget'" testTarget.Description ""
        finally
            try File.Delete tempFile with e -> ()


    testCase "issue #2025" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheckInPath "build.fsx" "build.fsx" "i002025" "script" |> ignProc

    testCase "issue #2007 - native libs work" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheck "build.fsx" "build.fsx" "i002007-native-libs" |> ignore
  ]
