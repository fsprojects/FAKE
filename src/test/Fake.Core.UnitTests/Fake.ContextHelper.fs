module Fake.ContextHelper

open Fake.Core
open Expecto
open System.Diagnostics


let time f =
    let sw = Stopwatch.StartNew()
    f ()
    sw.Stop()
    sw.Elapsed

let withFakeContext name f =
    use execContext = Fake.Core.Context.FakeExecutionContext.Create false (sprintf "text.fsx - %s" name) []
    Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
    try f ()
    finally 
        Fake.Core.Context.removeExecutionContext()


let withMaxTime maxTime name f =
    let t = time f
    printfn "Test '%s' finished in '%O'" name t
    Expect.isLessThanOrEqual t maxTime "Expected test to finish faster than the given maxTime."


let testCaseAssertTime maxTime name f =
    testCase name <| fun arg ->
        withMaxTime maxTime name (fun () -> f arg)

let fakeContextTestCase name f =
    testCase name <| fun arg ->
        withFakeContext name (fun () -> f arg)

let fakeContextTestCaseAssertTime maxTime name f =   
    testCase name <| fun arg ->
        withFakeContext name (fun () -> 
            withMaxTime maxTime name (fun () -> f arg))