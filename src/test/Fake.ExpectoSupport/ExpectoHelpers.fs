namespace Fake.ExpectoSupport

open System
open System.Threading
open System.Threading.Tasks
open Expecto.Impl
open Expecto.Logging

module ExpectoHelpers =

    let inline internal commaString (i: int) = i.ToString("#,##0")

    let mutable logger = Log.create "ExpectoFake"

    let literateConsoleTarget =
        LiterateConsoleTarget([| "ExpectoFake" |], ExpectoConfig.defaultConfig.verbosity)

    // because of https://github.com/haf/expecto/issues/367
    let fakeDefaultPrinter: TestPrinters =
        { beforeRun =
            fun _tests ->
                printfn "EXPECTO? Running tests..."
                async.Zero()

          beforeEach =
            fun n ->
                printfn $"{n} starting..."
                async.Zero()

          info =
            fun s ->
                printfn $"{s}"
                async.Zero()

          passed =
            fun n d ->
                printfn $"{n} passed in {d}."
                async.Zero()

          ignored =
            fun n m ->
                printfn $"{n} was ignored. {m}"
                async.Zero()

          failed =
            fun n m d ->
                printfn $"{n} failed in {d}. {m}"
                async.Zero()

          exn =
            fun n e d ->
                printfn $"{n} errored in {d}: {e}"
                async.Zero()

          summary =
            fun _config summary ->
                let splitSign = _config.joinWith.asString
                let spirit = if summary.successful then "Success!" else String.Empty

                let commonAncestor =
                    let rec loop (ancestor: string) (descendants: string list) =
                        match descendants with
                        | [] -> ancestor
                        | hd :: tl when hd.StartsWith(ancestor) -> loop ancestor tl
                        | _ ->
                            if ancestor.Contains(splitSign) then
                                loop (ancestor.Substring(0, ancestor.LastIndexOf splitSign)) descendants
                            else
                                "miscellaneous"

                    let parentNames =
                        summary.results
                        |> List.map (fun (flatTest, _) ->
                            if flatTest.name.Length > 1 then
                                let size = flatTest.name.Length - 1
                                _config.joinWith.format flatTest.name.[0..size]
                            else
                                _config.joinWith.format flatTest.name)

                    match parentNames with
                    | [ x ] -> x
                    | hd :: tl -> loop hd tl
                    | _ -> "miscellaneous" //we can't get here

                printfn
                    "EXPECTO! %s tests run in %O for %s – %s passed, %s ignored, %s failed, %s errored. %s"
                    (List.sumBy (fun (_, r: TestSummary) -> if r.result.isIgnored then 0 else r.count) summary.results
                     |> commaString)
                    summary.duration
                    commonAncestor
                    (List.sumBy (fun (_, r) -> r.count) summary.passed |> commaString)
                    (List.sumBy (fun (_, r) -> r.count) summary.ignored |> commaString)
                    (List.sumBy (fun (_, r) -> r.count) summary.failed |> commaString)
                    (List.sumBy (fun (_, r) -> r.count) summary.errored |> commaString)
                    spirit

                async.Zero() }

    let setPrinter printer (config: Expecto.Impl.ExpectoConfig) = { config with printer = printer }

    let setFakePrinter (config: Expecto.Impl.ExpectoConfig) = setPrinter fakeDefaultPrinter config

    let appendSummaryHandler summaryPrinter (config: Expecto.Impl.ExpectoConfig) =
        config.appendSummaryHandler summaryPrinter

    let addFilter f (config: Expecto.Impl.ExpectoConfig) =
        { config with
            filter =
                fun test ->
                    let filteredTests = config.filter test
                    f filteredTests }

    let withTimeout (timeout: TimeSpan) (labelPath: string) (test: Expecto.TestCode) : Expecto.TestCode =
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
        | Expecto.Sync test -> async { test () } |> timeoutAsync |> Expecto.Async
        | Expecto.SyncWithCancel test ->
            Expecto.SyncWithCancel(fun ct ->
                Async.StartImmediate(async { test (CancellationToken.None) } |> timeoutAsync))
        | Expecto.Async test -> timeoutAsync test |> Expecto.Async
        | Expecto.AsyncFsCheck(testConfig, stressConfig, test) ->
            Expecto.AsyncFsCheck(testConfig, stressConfig, test >> timeoutAsync)

    let mapTest f test =
        let rec recMapping labelPath test =
            match test with
            | Expecto.TestCase(code: Expecto.TestCode, state: Expecto.FocusState) ->
                Expecto.TestCase(f labelPath code, state)
            | Expecto.TestList(tests: Expecto.Test list, state: Expecto.FocusState) ->
                Expecto.TestList(tests |> List.map (recMapping labelPath), state)
            | Expecto.TestLabel(label: string, test: Expecto.Test, state: Expecto.FocusState) ->
                Expecto.TestLabel(label, recMapping (labelPath + "/" + label) test, state)
            | Expecto.Test.Sequenced(sequenceMethod, test) ->
                Expecto.Test.Sequenced(sequenceMethod, recMapping labelPath test)

        recMapping "" test


    let addTimeout t config =
        config |> addFilter (mapTest (withTimeout (t)))
