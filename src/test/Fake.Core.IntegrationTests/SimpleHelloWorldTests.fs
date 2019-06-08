module Fake.Core.IntegrationTests.SimpleHelloWorldTests

open Expecto
open Expecto.Flip
open System
open System
open System.IO
open System.Diagnostics

let fail s = Expect.isTrue s false

[<Tests>]
let tests = 
  testList "Fake.Core.IntegrationTests" [
    testCase "no dependencies hello world" <| fun _ ->
        let result = fakeRunAndCheck "hello_world.fsx" "hello_world.fsx" "core-no-dependencies-hello-world"
        let stdOut = String.Join("\n", result.Messages)
        let stdErr = String.Join("\n", result.Errors)

        stdOut.Trim() |> Expect.equal "Hello FAKE exected" "Hello FAKE"
        stdErr.Trim() |> Expect.equal "empty exected" ""

    testCase "simple failed to compile" <| fun _ ->
        try
            fakeRunAndCheck "fail-to-compile.fsx" "fail-to-compile.fsx" "core-simple-failed-to-compile" |> ignore
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
            fakeRunAndCheck "runtime-error.fsx" "runtime-error.fsx" "core-simple-runtime-error" |> ignore
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
            fakeRunAndCheck "reference_fake-runtime.fsx" "reference_fake-runtime.fsx" "core-reference-fake-runtime" |> ignore

    testCase "context exists" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheck "context.exists.fsx" "context.exists.fsx" "core-context-exists" |> ignore

    testCase "use external paket.dependencies" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheck "use_external_dependencies.fsx" "use_external_dependencies.fsx" "core-use-external-paket-dependencies" |> ignore

    testCase "reference fake core targets" <| fun _ ->
        let result =
            handleAndFormat <| fun () -> fakeRunAndCheck "reference_fake-targets.fsx" "reference_fake-targets.fsx --test" "core-reference-fake-core-targets"
        let stdOut = String.Join("\n", result.Messages).Trim()
        let stdErr = String.Join("\n", result.Errors)

        let expected = "Arguments: [\"--test\"]"
        stdOut.Contains expected
            |> Expect.isTrue (sprintf "stdout should contain '%s', but was: '%s'" expected stdOut)
        stdErr.Trim() |> Expect.equal "empty exected" ""

    testCase "issue #2025" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheckInPath "build.fsx" "build.fsx" "i002025" "script" |> ignore

    testCase "issue #2007 - native libs work" <| fun _ ->
        handleAndFormat <| fun () ->
            fakeRunAndCheck "build.fsx" "build.fsx" "i002007-native-libs" |> ignore
    
  ]