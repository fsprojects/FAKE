namespace Fake.ExpectoSupport
open Expecto
open System
open System.Threading
open System.Threading.Tasks

module ExpectoHelpers =
    let addFilter f (config:Impl.ExpectoConfig) =
        { config with
             filter = fun test ->
                let filteredTests = config.filter test
                f filteredTests }

    let withTimeout (timeout:TimeSpan) (labelPath:string) (testCode) =
        match testCode with
        | Sync (stest: (unit -> unit)) ->
            Async (async {
                let t = Async.StartAsTask(async { return stest() })
                let delay = Task.Delay(timeout)
                let! result = Task.WhenAny(t, delay) |> Async.AwaitTask
                if result = delay then
                    Tests.failtestf "Test '%s' timed out" labelPath
            })
        | SyncWithCancel (stest: (CancellationToken -> unit))  ->
            Async (async {
                let! tok = Async.CancellationToken
                let t = Async.StartAsTask(async { return stest tok })
                let delay = Task.Delay(timeout)
                let! result = Task.WhenAny(t, delay) |> Async.AwaitTask
                if result = delay then
                    Tests.failtestf "Test '%s' timed out" labelPath
            })
        
        | Async (atest: Async<unit>) ->
            Async (async {
                let! t = Async.StartChild(atest, int timeout.TotalMilliseconds)
                try
                    let! result = t
                    return result
                with :? System.TimeoutException ->
                    Tests.failtestf "Test '%s' timed out" labelPath
            })
        
        | AsyncFsCheck (testConfig: FsCheckConfig option,
                        stressConfig: FsCheckConfig option,
                        test: (FsCheckConfig -> Async<unit>)) ->
            AsyncFsCheck (testConfig, stressConfig, fun config -> async {
                let! t = Async.StartChild(test config, int timeout.TotalMilliseconds)
                try
                    let! result = t
                    return result
                with :? System.TimeoutException ->
                    Tests.failtestf "Test '%s' timed out" labelPath
            })
        


    let mapTest f test =
        let rec recMapping labelPath test =
            match test with
            | TestCase (code:TestCode, state:FocusState) ->
                TestCase(f labelPath code, state)
            | TestList (tests:Test list, state:FocusState) ->
                TestList (tests |> List.map (recMapping labelPath), state)
            | TestLabel (label:string, test:Test, state:FocusState) ->
                TestLabel(label, recMapping (labelPath + "/" + label) test, state)
            | Test.Sequenced (sequenceMethod, test) ->
                Test.Sequenced(sequenceMethod, recMapping labelPath test)
    
        recMapping "" test


    let addTimeout t config =
        config
        |> addFilter (mapTest (withTimeout (t)))
