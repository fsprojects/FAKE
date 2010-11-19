[<AutoOpen>]
module Fake.AsyncHelper

/// Runs the given function on all items in parallel
let inline doParallel f items =
    items
    |> Seq.map (fun element -> async { return f element})
    |> Async.Parallel
    |> Async.RunSynchronously