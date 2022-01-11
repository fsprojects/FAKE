namespace Fake.ExpectoSupport

open System
open System.Threading
open System.Threading.Tasks

module ExpectoHelpers =
    let addFilter f (config:Expecto.Impl.ExpectoConfig) =
        { config with
             filter = fun test ->
                let filteredTests = config.filter test
                f filteredTests }

    let withTimeout (timeout:TimeSpan) (labelPath:string) (test: Expecto.TestCode) : Expecto.TestCode =
        let timeoutAsync testAsync =
          async {
              let t = Async.StartAsTask(testAsync)
              let delay = Task.Delay(timeout)
              let! result = Task.WhenAny(t, delay) |> Async.AwaitTask
              if result = delay then
                Expecto.Tests.failtestf "Test '%s' timed out" labelPath
              return result.GetAwaiter().GetResult()
          }

        match test with
        | Expecto.Sync test -> async { test() } |> timeoutAsync |> Expecto.Async
        | Expecto.SyncWithCancel test ->
          Expecto.SyncWithCancel (fun ct ->
            Async.StartImmediate(async { test (CancellationToken.None) } |> timeoutAsync)
          )
        | Expecto.Async test -> timeoutAsync test |> Expecto.Async
        | Expecto.AsyncFsCheck (testConfig, stressConfig, test) ->
          Expecto.AsyncFsCheck (testConfig, stressConfig, test >> timeoutAsync)

    let mapTest f test =
        let rec recMapping labelPath test =
            match test with
            | Expecto.TestCase (code:Expecto.TestCode, state:Expecto.FocusState) ->
                Expecto.TestCase(f labelPath code, state)
            | Expecto.TestList (tests:Expecto.Test list, state:Expecto.FocusState) ->
                Expecto.TestList (tests |> List.map (recMapping labelPath), state)
            | Expecto.TestLabel (label:string, test:Expecto.Test, state:Expecto.FocusState) ->
                Expecto.TestLabel(label, recMapping (labelPath + "/" + label) test, state)
            | Expecto.Test.Sequenced (sequenceMethod, test) ->
                Expecto.Test.Sequenced(sequenceMethod, recMapping labelPath test)
    
        recMapping "" test


    let addTimeout t config =
        config
        |> addFilter (mapTest (withTimeout (t)))
