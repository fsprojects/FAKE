[<AutoOpen>]
module Fake.AsyncHelper

/// Runs the given function on all items in parallel
let inline doParallel f items =
    items
    |> Seq.map (fun element -> async { return f element})
    |> Async.Parallel
    |> Async.RunSynchronously

let inline doParallelWithThrottle limit f items =
    use sem = new System.Threading.Semaphore(limit, limit)
    items
    |> Seq.map (fun element -> async {
            sem.WaitOne() |> ignore
            let result = Async.RunSynchronously <| async { return f element }
            sem.Release() |> ignore
            return result
        })
    |> Async.Parallel
    |> Async.RunSynchronously
