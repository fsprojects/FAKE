module Fake.Core.IntegrationTests.SimpleHelloWorldTests

open Fake
open System
open NUnit.Framework
open System
open System.IO
open System.Diagnostics


[<Test>]
let ``no dependencies hello world``() =
    let result = fakeRun "hello_world.fsx" "core-no-dependencies-hello-world"
    let stdOut = String.Join("\n", result.Messages)
    let stdErr = String.Join("\n", result.Errors)

    Assert.AreEqual(stdOut.Trim(), "Hello FAKE")
    Assert.AreEqual(stdErr.Trim(), "")

[<Test>]
let ``simple failed to compile``() =
    try
        fakeRun "fail-to-compile.fsx" "core-simple-failed-to-compile" |> ignore
        Assert.Fail ("Expected an compilation error and a nonzero exit code!")
    with 
    | FakeExecutionFailed(result) ->
        let stdOut = String.Join("\n", result.Messages)
        let stdErr = String.Join("\n", result.Errors)
        Assert.IsTrue(stdErr.Contains("klajsdhgfasjkhd"), sprintf "Standard Error Output should contain 'klajsdhgfasjkhd', but was: '%s', Out: '%s'" stdErr stdOut)
        ()

[<Test>]
let ``simple runtime error``() =
    try
        fakeRun "runtime-error.fsx" "core-simple-runtime-error" |> ignore
        Assert.Fail ("Expected an runtime error and a nonzero exit code!")
    with
    | FakeExecutionFailed(result) ->
        let stdOut = String.Join("\n", result.Messages)
        let stdErr = String.Join("\n", result.Errors)
        Assert.IsTrue(stdErr.Contains("runtime error"), sprintf "Standard Error Output should contain 'runtime error', but was: '%s', Out: '%s'" stdErr stdOut)
        ()

[<Test>]
let ``reference fake runtime``() =
    handleAndFormat <| fun () ->
        fakeRun "reference_fake-runtime.fsx" "core-reference-fake-runtime" |> ignore

[<Test>]
let ``context exists``() =
    handleAndFormat <| fun () ->
        fakeRun "context.exists.fsx" "core-context-exists" |> ignore

[<Test>]
let ``use external paket.dependencies``() =
    handleAndFormat <| fun () ->
        fakeRun "use_external_dependencies.fsx" "core-use-external-paket-dependencies" |> ignore

[<Test>]
let ``reference fake core targets``() = 
    let result = fakeRun "reference_fake-targets.fsx --test" "core-reference-fake-core-targets"
    let stdOut = String.Join("\n", result.Messages).Trim()
    let stdErr = String.Join("\n", result.Errors)

    let expected = "Arguments: [\"--test\"]"
    Assert.IsTrue(stdOut.Contains expected, sprintf "stdout should contain '%s', but was: '%s'" expected stdOut)