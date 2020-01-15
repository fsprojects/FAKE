namespace Fake.ExpectoSupport

open System
open System.Threading
open System.Threading.Tasks

module ExpectoHelpers =

    let inline internal commaString (i:int) = i.ToString("#,##0")
    // because of https://github.com/haf/expecto/issues/367
    let fakeDefaultPrinter : Expecto.Impl.TestPrinters =
      { beforeRun = fun _tests ->
            printfn "EXPECTO? Running tests..."
            async.Zero()

        beforeEach = fun n ->
            printfn "EXPECTO? %s starting..." n
            async.Zero()

        info = fun s ->
            printfn "EXPECTO? %s" s
            async.Zero()

        passed = fun n d ->
            printfn "EXPECTO? %s passed in %O." n d
            async.Zero()

        ignored = fun n m ->
            printfn "EXPECTO? %s was ignored. %s" n m
            async.Zero()

        failed = fun n m d ->
            printfn "EXPECTO? %s failed in %O. %s" n d m
            async.Zero()

        exn = fun n e d ->
            printfn "EXPECTO? %s errored in %O: %O" n d e
            async.Zero()

        summary = fun _config summary ->
          let spirit =
            if summary.successful then "Success!" else String.Empty
          let commonAncestor =
            let rec loop ancestor (descendants : string list) =
              match descendants with
              | [] -> ancestor
              | hd::tl when hd.StartsWith(ancestor)->
                loop ancestor tl
              | _ ->
                if ancestor.Contains("/") then
                  loop (ancestor.Substring(0, ancestor.LastIndexOf "/")) descendants
                else
                  "miscellaneous"

            let parentNames =
              summary.results
              |> List.map (fun (flatTest, _)  ->
                if flatTest.name.Contains("/") then
                  flatTest.name.Substring(0, flatTest.name.LastIndexOf "/")
                else
                  flatTest.name )

            match parentNames with
            | [x] -> x
            | hd::tl ->
              loop hd tl
            | _ -> "miscellaneous" //we can't get here
          printfn "EXPECTO! %s tests run in %O for %s – %s passed, %s ignored, %s failed, %s errored. %s"
                 (summary.results |> List.sumBy (fun (_,r) -> if r.result.isIgnored then 0 else r.count) |> commaString)
                 summary.duration
                 commonAncestor
                 (summary.passed  |> List.sumBy (fun (_,r) -> r.count) |> commaString)
                 (summary.ignored |> List.sumBy (fun (_,r) -> r.count) |> commaString)
                 (summary.failed  |> List.sumBy (fun (_,r) -> r.count) |> commaString)
                 (summary.errored  |> List.sumBy (fun (_,r) -> r.count) |> commaString)
                 spirit
          async.Zero()
          }

    let setPrinter printer (config:Expecto.Impl.ExpectoConfig) =
        { config with printer = printer }
    let setFakePrinter (config:Expecto.Impl.ExpectoConfig) =
        setPrinter fakeDefaultPrinter config

    let appendSummaryHandler summaryPrinter (config:Expecto.Impl.ExpectoConfig) =
        config.appendSummaryHandler summaryPrinter

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
