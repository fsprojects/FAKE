namespace Fake.Core.ProcessHelpers

module internal Async = 
    let lift f =
        f >> async.Return
    let bind f a =
        async.Bind(a, f)
    let map f a =
        bind (lift f) a


[<AutoOpen>]
module internal AsyncExtensions =
    open System.Threading
    open System.Threading.Tasks

    type internal VolatileBarrier() =
        [<VolatileField>]
        let mutable isStopped = false
        member __.Proceed = not isStopped
        member __.Stop() = isStopped <- true
    open System
    // This uses a trick to get the underlying OperationCanceledException
    let inline internal getCancelledException (completedTask:Task) (waitWithAwaiter) =
        let fallback = new TaskCanceledException(completedTask) :> OperationCanceledException
        // sadly there is no other public api to retrieve it, but to call .GetAwaiter().GetResult().
        try waitWithAwaiter()
            // should not happen, but just in case...
            fallback
        with
        | :? OperationCanceledException as o -> o
        | other ->
            // shouldn't happen, but just in case...
            new TaskCanceledException(fallback.Message, other) :> OperationCanceledException
    let inline internal startCatchCancellation(work, cancellationToken) =
            Async.FromContinuations(fun (cont, econt, _) ->
              // When the child is cancelled, report OperationCancelled
              // as an ordinary exception to "error continuation" rather
              // than using "cancellation continuation"
              let ccont e = econt e
              // Start the workflow using a provided cancellation token
              Async.StartWithContinuations( work, cont, econt, ccont,
                                            ?cancellationToken=cancellationToken) )
    let inline internal startAsTaskHelper start computation cancellationToken taskCreationOptions =
        let token = defaultArg cancellationToken Async.DefaultCancellationToken
        let taskCreationOptions = defaultArg taskCreationOptions TaskCreationOptions.None
        let tcs = new TaskCompletionSource<_>(taskCreationOptions)

        let a =
            async {
                try
                    // To ensure we don't cancel this very async (which is required to properly forward the error condition)
                    let! result = startCatchCancellation(computation, Some token)
                    do
                        tcs.SetResult(result)
                with exn ->
                    tcs.SetException(exn)
            }
        start(a)
        tcs.Task
    type Async with
        static member StartCatchCancellation(work, ?cancellationToken) =
            startCatchCancellation (work, cancellationToken)

        /// Like StartAsTask but gives the computation time to so some regular cancellation work
        static member StartAsTaskProperCancel (computation : Async<_>, ?taskCreationOptions, ?cancellationToken:CancellationToken) : Task<_> =
            startAsTaskHelper Async.Start computation cancellationToken taskCreationOptions 

        static member StartImmediateAsTask (computation,?taskCreationOptions,?cancellationToken) =
            startAsTaskHelper Async.StartImmediate computation cancellationToken taskCreationOptions 

        static member AwaitTaskWithoutAggregate (task:Task<'T>) : Async<'T> =
            Async.FromContinuations(fun (cont, econt, ccont) ->
                let continuation (completedTask : Task<_>) =
                    if completedTask.IsCanceled then
                        let cancelledException =
                            getCancelledException completedTask (fun () -> completedTask.GetAwaiter().GetResult() |> ignore)
                        econt (cancelledException)
                    elif completedTask.IsFaulted then
                        if completedTask.Exception.InnerExceptions.Count = 1 then
                            econt completedTask.Exception.InnerExceptions.[0]
                        else
                            econt completedTask.Exception
                    else
                        cont completedTask.Result
                task.ContinueWith(Action<Task<'T>>(continuation)) |> ignore)
        static member AwaitTaskWithoutAggregate (task:Task) : Async<unit> =
            Async.FromContinuations(fun (cont, econt, ccont) ->
                let continuation (completedTask : Task) =
                    if completedTask.IsCanceled then
                        let cancelledException =
                            getCancelledException completedTask (fun () -> completedTask.GetAwaiter().GetResult() |> ignore)
                        econt (cancelledException)
                    elif completedTask.IsFaulted then
                        if completedTask.Exception.InnerExceptions.Count = 1 then
                            econt completedTask.Exception.InnerExceptions.[0]
                        else
                            econt completedTask.Exception
                    else
                        cont ()
                task.ContinueWith(Action<Task>(continuation)) |> ignore)

    type Microsoft.FSharp.Control.AsyncBuilder with
      /// An extension method that overloads the standard 'Bind' of the 'async' builder. The new overload awaits on
      /// a standard .NET task
      member x.Bind(t : Task<'T>, f:'T -> Async<'R>) : Async<'R> =
        async.Bind(Async.AwaitTaskWithoutAggregate t, f)

      /// An extension method that overloads the standard 'Bind' of the 'async' builder. The new overload awaits on
      /// a standard .NET task which does not commpute a value
      member x.Bind(t : Task, f : unit -> Async<'R>) : Async<'R> =
        async.Bind(Async.AwaitTaskWithoutAggregate t, f)