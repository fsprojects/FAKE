namespace Fake.ExpectoSupport
open Expecto
open System
open System.Threading
open System.Threading.Tasks

module ExpectoHelpers =
    let setThreadPool () =
      ThreadPool.SetMinThreads(100, 100)
    let addFilter f (config:Impl.ExpectoConfig) =
        { config with
             filter = fun test ->
                let filteredTests = config.filter test
                f filteredTests }

    let withTimeout (timeout:TimeSpan) (labelPath:string) (test: TestCode) : TestCode =
        let timeoutAsync testAsync =
          async {
              let t = Async.StartAsTask(testAsync)
              let delay = Task.Delay(timeout)
              let! result = Task.WhenAny(t, delay) |> Async.AwaitTask
              if result = delay then
                Tests.failtestf "Test '%s' timed out" labelPath
          }

        match test with
        | Sync test -> async { test() } |> timeoutAsync |> Async
        | SyncWithCancel test ->
          SyncWithCancel (fun ct ->
            Async.StartImmediate(async { test ct } |> timeoutAsync)
          )
        | Async test -> timeoutAsync test |> Async
        | AsyncFsCheck (testConfig, stressConfig, test) ->
          AsyncFsCheck (testConfig, stressConfig, test >> timeoutAsync)

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
