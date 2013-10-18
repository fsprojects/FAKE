[<AutoOpen>]
/// Contains some extensions to the F# async workflows.
module Fake.AsyncHelper
open System.Collections.Concurrent

/// Runs the given function on all items in parallel
let inline doParallel f items =
    items
    |> Seq.map (fun element -> async { return f element})
    |> Async.Parallel
    |> Async.RunSynchronously

/// Generic type for JobRequests
type JobRequest<'T> = {
    /// The job id
    Id : int
    /// The work item
    WorkItem : 'T }

/// Type to represent WorkRequests
type WorkRequest<'T> =
| Job of JobRequest<'T>
| End

/// Runs the given function on all items in parallel but limits the degree of parallismn
let inline doParallelWithThrottle<'a, 'b> limit f items =
    let itemArray = Seq.toArray items
    let itemCount = Array.length itemArray
    let resultMap = ConcurrentDictionary<int, 'b>()
    use block = new BlockingCollection<WorkRequest<'a>>(1)
    use completeBlock = new BlockingCollection<unit>(1)
    let monitor = 
        MailboxProcessor.Start(fun inbox ->
            let rec inner complete =
                async {
                    do! inbox.Receive()
                    if complete + 1 = limit then
                        completeBlock.Add(())
                        return ()
                    else
                        return! inner <| complete + 1
                }
            inner 0)
    let createAgent () =
        MailboxProcessor.Start(
            fun inbox ->
                let rec inner () = async {
                        let! request = async { return block.Take() }
                        match request with
                        | Job job ->
                            let! result = async { return f (job.WorkItem) }
                            resultMap.AddOrUpdate(job.Id, result, fun _ _ -> result) |> ignore
                            return! inner ()
                        | End  ->
                            monitor.Post ()                            
                            return ()
                    }
                inner ()
        )
    let agents =
        [| for i in 1..limit -> createAgent() |]
    itemArray
    |> Array.mapi (fun i item -> Job { Id = i; WorkItem = item })
    |> Array.iter (block.Add)

    [1..limit]
    |> Seq.iter (fun x -> block.Add(End))

    completeBlock.Take()
    let results = Array.zeroCreate itemCount
    resultMap
    |> Seq.iter (fun kv -> results.[kv.Key] <- kv.Value)
    results
