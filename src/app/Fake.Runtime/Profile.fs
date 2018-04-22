module Fake.Profile

open System.Diagnostics
open System
open Paket.Profile

type Category =
    | Cli
    | Paket
    | PaketDependencyCache
    | PaketRuntimeGraph
    | PaketGetAssemblies
    | Compiling
    | Analyzing
    | UserTime
    | Cleanup
    | Other

type EventBoundary = 
    | Start of DateTime
    | End of DateTime
    with
        static member GetTime(b: EventBoundary) = 
            match b with
            | Start(dt) -> dt
            | End(dt) -> dt
        static member IsEndBoundary(b: EventBoundary) = 
            match b with
            | End(_) -> true
            | _ -> false
        static member IsStartBoundary(b: EventBoundary) = 
            match b with
            | Start(_) -> true
            | _ -> false

type Event = { Category: Category; Start: EventBoundary; End: EventBoundary }

let private getNextSpan(startIndex: int, boundaries: EventBoundary array): (TimeSpan * (int * EventBoundary array)) option = 
    let mutable i = startIndex
    while (i < boundaries.Length) && EventBoundary.IsEndBoundary(boundaries.[i]) do
        i <- i + 1

    if i >= boundaries.Length then
        None
    else
        let mutable spanStart = i 
        i <- i + 1
        let mutable boundaryStartCount = 1

        while (boundaryStartCount > 0) && (i < boundaries.Length) do
            match boundaries.[i] with
            | Start(_) ->
                boundaryStartCount <- boundaryStartCount + 1
            | End(_) ->
                boundaryStartCount <- boundaryStartCount - 1
                
            i <- i + 1

        // Calculate the next time span.
        let startTime = EventBoundary.GetTime(boundaries.[spanStart])
        let endTime = EventBoundary.GetTime(boundaries.[Math.Min(Math.Max(0, boundaries.Length - 1), i - 1)])

        Some((endTime - startTime, (i, boundaries)))        

let getCoalescedEventTimeSpans(boundaries: EventBoundary array): TimeSpan array = 
    let sortedBoundaries = 
        boundaries
        |> Array.sortBy (fun b -> EventBoundary.GetTime(b))

    let spans = Array.unfold getNextSpan (0, sortedBoundaries)
    spans

let events =
    System.Collections.Concurrent.ConcurrentBag<Event>()
    
let trackEvent cat =
    let now = DateTime.Now
    events.Add({ Category = cat; Start = Start(now); End = End(now) })

let startCategory cat =
    let cw = Stopwatch.StartNew()
    let mutable wasDisposed = false
    { new System.IDisposable with
        member x.Dispose () = 
            if not wasDisposed then
                wasDisposed <- true
                let now = DateTime.Now
                let start = now - cw.Elapsed
                cw.Stop(); events.Add({ Category = cat; Start = Start(start); End = End(now)})
    }
    
let startCategoryF cat f =
    let cw = Stopwatch.StartNew()
    let res = f()
    cw.Stop()
    let now = DateTime.Now
    let start = now - cw.Elapsed
    events.Add({ Category = cat; Start = Start(start); End = End(now) })
    res

let print includePaket realTime =

    let fakeGroupedResults =
        events
        |> Seq.groupBy (fun (ev) -> ev.Category)
        |> Seq.map (fun (cat, group) ->
            let l = group |> Seq.toList
            let eventBoundaries = l |> List.collect(fun ev -> [ev.Start; ev.End])
            let mergedSpans = getCoalescedEventTimeSpans(eventBoundaries |> List.toArray)
            let mergedSpanLengths = mergedSpans |> Array.fold (+) (TimeSpan())

            cat, l.Length, mergedSpanLengths)
        |> Seq.toList
    Paket.Logging.tracefn "Performance:"
    fakeGroupedResults
    |> List.sortBy (fun (cat,_,_) ->
        match cat with
        | Cli -> 1
        | Paket -> 2
        | PaketDependencyCache -> 3
        | PaketRuntimeGraph -> 4
        | PaketGetAssemblies -> 5
        | Compiling -> 6
        | Analyzing -> 7
        | UserTime -> 8
        | Cleanup -> 9
        | Other -> 10)
    |> List.iter (fun (cat, _, elapsed) ->
        match cat with
        | Cli ->
            Paket.Logging.tracefn " - Cli parsing: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
        | Paket ->
            Paket.Logging.tracefn " - Packages: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
            if includePaket then
                let paketGroupedResults =
                    Paket.Profile.events
                    |> Seq.groupBy (fun (ev) -> ev.Category)
                    |> Seq.map (fun (cat, group) ->
                        let l = group |> Seq.toList
                        let eventBoundaries = l |> List.collect(fun ev -> [ev.Start; ev.End])
                        let mergedSpans = Paket.Profile.getCoalescedEventTimeSpans(eventBoundaries |> List.toArray)
                        let mergedSpanLengths = mergedSpans |> Array.fold (+) (TimeSpan())

                        cat, l.Length, mergedSpanLengths)
                    |> Seq.toList
                let paketBlockedRaw =
                    paketGroupedResults
                    |> List.filter (function Paket.Profile.Category.ResolverAlgorithmBlocked _, _, _ -> true | _ -> false)
                let paketBlocked =
                    paketBlockedRaw
                    |> List.map (fun (_,_,t) -> t)
                    |> Seq.fold (+) (TimeSpan())
                let resolver =
                    match paketGroupedResults |> List.tryPick (function Paket.Profile.Category.ResolverAlgorithm, _, s -> Some s | _ -> None) with
                    | Some s -> s
                    | None -> TimeSpan()
                paketGroupedResults
                |> List.sortBy (fun (cat,_,_) ->
                    match cat with
                    | Paket.Profile.Category.ResolverAlgorithm -> 1
                    | Paket.Profile.Category.ResolverAlgorithmBlocked b -> 2
                    | Paket.Profile.Category.ResolverAlgorithmNotBlocked b -> 3
                    | Paket.Profile.Category.FileIO -> 4
                    | Paket.Profile.Category.NuGetDownload -> 5
                    | Paket.Profile.Category.NuGetRequest -> 6
                    | Paket.Profile.Category.Other -> 7)
                |> List.iter (fun (cat, num, elapsed) ->
                    let reason b =
                        match b with
                        | Paket.Profile.BlockReason.PackageDetails -> "retrieving package details"
                        | Paket.Profile.BlockReason.GetVersion -> "retrieving package versions"
                    match cat with
                    | Paket.Profile.Category.ResolverAlgorithm ->
                        Paket.Logging.tracefn "   - Resolver: %s (%d runs)" (Paket.Utils.TimeSpanToReadableString elapsed) num
                        let realTime = resolver - paketBlocked
                        Paket.Logging.tracefn "      - Runtime: %s" (Paket.Utils.TimeSpanToReadableString realTime)
                    | Paket.Profile.Category.ResolverAlgorithmBlocked b ->
                        let reason = reason b
                        Paket.Logging.tracefn "      - Blocked (%s): %s (%d times)" reason (Paket.Utils.TimeSpanToReadableString elapsed) num
                    | Paket.Profile.Category.ResolverAlgorithmNotBlocked b ->
                        let reason = reason b
                        Paket.Logging.tracefn "      - Not Blocked (%s): %d times" reason num
                    | Paket.Profile.Category.FileIO ->
                        Paket.Logging.tracefn "   - Disk IO: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
                    | Paket.Profile.Category.NuGetDownload ->
                        let avg = TimeSpan.FromTicks(elapsed.Ticks / int64 num)
                        Paket.Logging.tracefn "   - Average Download Time: %s" (Paket.Utils.TimeSpanToReadableString avg)
                        Paket.Logging.tracefn "   - Number of downloads: %d" num
                    | Paket.Profile.Category.NuGetRequest ->
                        let avg = TimeSpan.FromTicks(elapsed.Ticks / int64 num)
                        Paket.Logging.tracefn "   - Average Request Time: %s" (Paket.Utils.TimeSpanToReadableString avg)
                        Paket.Logging.tracefn "   - Number of Requests: %d" num
                    | Paket.Profile.Category.Other ->
                        Paket.Logging.tracefn "   - Other: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
                    )

        | PaketDependencyCache ->
            Paket.Logging.tracefn "   - Dependency Cache: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
        | PaketRuntimeGraph ->
            Paket.Logging.tracefn "   - Creating Runtime Graph: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
        | PaketGetAssemblies ->
            Paket.Logging.tracefn "   - Retrieve Assembly List: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
        | Compiling ->
            Paket.Logging.tracefn " - Script compiling: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
        | Analyzing ->
            Paket.Logging.tracefn " - Script analyzing: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
        | UserTime -> 
            Paket.Logging.tracefn " - Script running: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
        | Cleanup ->
            Paket.Logging.tracefn " - Script cleanup: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
        | Other -> 
            Paket.Logging.tracefn " - Other: %s" (Paket.Utils.TimeSpanToReadableString elapsed)
    )


    Paket.Logging.tracefn " - Runtime: %s" (Paket.Utils.TimeSpanToReadableString realTime)
    let omitted = Paket.Logging.getOmittedWarningCount()
    if includePaket && not Paket.Logging.verbose && omitted > 0 then
      Paket.Logging.traceWarnfn "Paket omitted %d warnings similar to the ones above. You can see them in verbose mode." omitted
