module Fake.Core.IntegrationTests.SimpleHelloWorldTests

open Fake
open System
open NUnit.Framework
open System
open System.IO
open System.Diagnostics

[<Test>]
let ``test fake context``() = 
    let c =
       let f = Fake.Core.Context.FakeExecutionContext.Create false "C:\Testfile" []
       Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake f)
       let myC = Fake.Core.Context.forceFakeContext()
       Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Unknown)
       myC
    try
       Fake.Core.Context.forceFakeContext() |> ignore
       Assert.Fail "Expected exception"
    with :? System.InvalidOperationException as e -> ()
      
    


[<Test>]
let ``simple printfn hello world``() = 
    fakeRun "hello_world.fsx" "core-no-dependencies-hello-world"

[<Test>]
let ``simple compilation error``() =
    try
        fakeRun "fail-to-compile.fsx" "core-simple-failed-to-compile"
        Assert.Fail ("Expected an compilation error and a nonzero exit code!")
    with e -> ()

[<Test>]
let ``simple runtime error``() =
    try
        fakeRun "runtime-error.fsx" "core-simple-runtime-error"
        Assert.Fail ("Expected an runtime error and a nonzero exit code!")
    with e -> ()
    
[<Test>]
let ``reference fake runtime``() = 
    fakeRun "reference_fake-runtime.fsx" "core-reference-fake-runtime"

[<Test>]
let ``context exists``() = 
    fakeRun "context-exists.fsx" "core-context-exists"

[<Test>]
let ``use external paket.dependencies``() = 
    fakeRun "use_external_dependencies.fsx" "core-use-external-paket-dependencies"

[<Test>]
let ``reference fake core targets``() = 
    fakeRun "reference_fake-targets.fsx" "core-reference-fake-core-targets"