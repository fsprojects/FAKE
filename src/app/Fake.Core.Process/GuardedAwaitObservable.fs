module internal Fake.Core.GuardedAwaitObservable

// from https://github.com/fsprojects/fsharpx/blob/f99a8f669ab49166c854c479d17c3add2b39f8d7/src/FSharpx.Core/Observable.fs
// TODO: reference FSharpx.Async once it supports netstandard...
open System
open System.Threading

/// Helper that can be used for writing CPS-style code that resumes
/// on the same thread where the operation was started.
let private synchronize f = 
    let ctx = System.Threading.SynchronizationContext.Current
    f (fun g -> 
        let nctx = System.Threading.SynchronizationContext.Current
        if not (isNull ctx) && ctx <> nctx then ctx.Post((fun _ -> g()), null)
        else g())

type Microsoft.FSharp.Control.Async with
    /// Behaves like AwaitObservable, but calls the specified guarding function
    /// after a subscriber is registered with the observable.
    static member GuardedAwaitObservable (ev1 : IObservable<'T1>) guardFunction = 
        let removeObj : IDisposable option ref = ref None
        let removeLock = new obj()
        let setRemover r = lock removeLock (fun () -> removeObj := Some r)
        
        let remove() = 
            lock removeLock (fun () -> 
                match !removeObj with
                | Some d -> 
                    removeObj := None
                    d.Dispose()
                | None -> ())
        synchronize (fun f -> 
            let workflow = 
                Async.FromContinuations((fun (cont, econt, ccont) -> 
                    let rec finish cont value = 
                        remove()
                        f (fun () -> cont value)
                    setRemover <| ev1.Subscribe({ new IObserver<_> with
                                                      member __.OnNext(v) = finish cont v
                                                      member __.OnError(e) = finish econt e
                                                      member __.OnCompleted() = 
                                                          let msg = 
                                                              "Cancelling the workflow, because the Observable awaited using AwaitObservable has completed."
                                                          finish ccont (new System.OperationCanceledException(msg)) })
                    guardFunction()))
            async { 
                let! cToken = Async.CancellationToken
                let token : CancellationToken = cToken
                use registration = token.Register(fun () -> remove())
                return! workflow
            })
