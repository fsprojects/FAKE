namespace Fake.Core

open System
open System.IO
open Fake.Core.ProcessHelpers

[<AutoOpen>]
module StreamExtensions =

    type Stream with
        static member CombineWrite (target1:Stream, target2:Stream)=
            if not target1.CanWrite || not target2.CanWrite then 
                raise <| ArgumentException("Streams need to be writeable to combine them.")
            let notSupported () = raise <| InvalidOperationException("operation not supported")
            { new Stream() with
                member _.CanRead = false
                member _.CanSeek = false
                member _.CanTimeout = target1.CanTimeout || target2.CanTimeout
                member _.CanWrite = true
                member _.Length = target1.Length
                member _.Position with get () = target1.Position and set _ = notSupported()
                member _.Flush () = target1.Flush(); target2.Flush()
                member _.FlushAsync tok = 
                    async {
                        do! target1.FlushAsync(tok)
                        do! target2.FlushAsync(tok)
                    }
                    |> Async.StartImmediateAsTask
                    :> System.Threading.Tasks.Task
                member _.Seek (_, _) = notSupported()
                member _.SetLength _ = notSupported()
                member _.Read (_, _, _) = notSupported()
                member _.Write (buffer, offset, count)=
                    target1.Write(buffer, offset, count)
                    target2.Write(buffer, offset, count)
                override _.WriteAsync(buffer, offset, count, tok) =
                    async {
                        let! child1 = 
                            target1.WriteAsync(buffer, offset, count, tok)
                            |> Async.AwaitTask
                            |> Async.StartChild
                        let! child2 =
                            target2.WriteAsync(buffer, offset, count, tok)
                            |> Async.AwaitTask
                            |> Async.StartChild
                        do! child1
                        do! child2
                    }
                    |> Async.StartImmediateAsTask
                    :> System.Threading.Tasks.Task
                }

        static member InterceptStream (readStream:Stream, track:Stream)=
            if not readStream.CanRead || not track.CanWrite then 
                raise <| ArgumentException("track Stream need to be writeable and readStream readable to intercept the readStream.")
            { new Stream() with
                member _.CanRead = true
                member _.CanSeek = readStream.CanSeek
                member _.CanTimeout = readStream.CanTimeout || track.CanTimeout
                member _.CanWrite = readStream.CanWrite
                member _.Length = readStream.Length
                member _.Position with get () = readStream.Position and set v = readStream.Position <- v
                member _.Flush () = readStream.Flush(); track.Flush()
                member _.FlushAsync tok = 
                    async {
                        do! readStream.FlushAsync(tok)
                        do! track.FlushAsync(tok)
                    }
                    |> Async.StartImmediateAsTask
                    :> System.Threading.Tasks.Task
                member _.Seek (offset, origin) = readStream.Seek(offset, origin)
                member _.SetLength l = readStream.SetLength(l)
                member _.Read (buffer, offset, count) =
                    let read = readStream.Read(buffer, offset, count)
                    track.Write(buffer, offset, read)
                    read
                override _.ReadAsync (buffer, offset, count, _) =
                  async {
                    let! read = readStream.ReadAsync(buffer, offset, count)
                    do! track.WriteAsync(buffer, offset, read)
                    return read
                  }
                  |> Async.StartImmediateAsTask
                member _.Write (buffer, offset, count)=
                    readStream.Write(buffer, offset, count)
                override _.WriteAsync(buffer, offset, count, tok) =
                    readStream.WriteAsync(buffer, offset, count, tok)
                override _.Dispose(t) = if t then readStream.Dispose()
                }


module internal InternalStreams =
    open System.Threading
    open System.Collections.Generic
    module AsyncHelper = 
        let FromBeginEndCancel beginAction endAction cancelAction =
            let asyncResult = ref null
            Async.FromBeginEnd(
                (fun (callback, state) -> 
                    asyncResult.Value <- beginAction(callback, state)
                    asyncResult.Value), 
                (fun res -> 
                    endAction res), 
                cancelAction  = (fun () -> 
                    while asyncResult.Value = null do Thread.Sleep 20
                    cancelAction(asyncResult.Value)))

    type ConcurrentQueueMessage<'a> =
        | Enqueue of 'a * AsyncReplyChannel<exn option>
        | Dequeue of AsyncReplyChannel<Choice<'a, exn>>
        | TryDequeue of AsyncReplyChannel<Choice<'a option,exn>>

    type ConcurrentQueue<'a>() =
        let core =
            let queue = Queue<'a>()
            let waitingQueue = Queue<AsyncReplyChannel<Choice<'a, exn>>>()
            MailboxProcessor.Start(fun inbox ->            
                let rec loop () = async {
                    let! msg = inbox.Receive()                
                    match msg with
                    | Enqueue (item,reply) ->
                        try
                            if waitingQueue.Count > 0 then
                                let waiting = waitingQueue.Dequeue()
                                waiting.Reply (Choice1Of2 item)
                            else
                                queue.Enqueue item
                            reply.Reply None
                        with exn ->
                            reply.Reply (Some exn)
                    | Dequeue reply ->
                        try
                            if queue.Count > 0 then
                                let item = queue.Dequeue()
                                reply.Reply (Choice1Of2 item)
                            else
                                waitingQueue.Enqueue reply
                        with exn ->
                            reply.Reply (Choice2Of2 exn)
                    | TryDequeue reply ->
                        try
                            let item = 
                                if queue.Count > 0 then
                                    Some <| queue.Dequeue()
                                else None
                            reply.Reply (Choice1Of2 item)
                        with exn ->
                            reply.Reply (Choice2Of2 exn)
                    return! loop() }
                loop())
        member x.EnqueueAsync(item) = async {
            let! item = core.PostAndAsyncReply(fun reply -> Enqueue (item, reply))
            return
                match item with
                | Some exn -> raise exn
                | None -> () }
        member x.DequeAsyncTimeout(?timeout) = async {
            let! result = 
                core.PostAndTryAsyncReply(
                    (fun reply -> Dequeue reply), ?timeout = timeout)
            return
                match result with
                | Some r ->
                    match r with
                    | Choice1Of2 item -> Some item
                    | Choice2Of2 exn -> raise exn
                | None -> None }
        member x.DequeAsync() = async {
            let! result = 
                core.PostAndAsyncReply (fun reply -> Dequeue reply)
            return
                match result with
                | Choice1Of2 item -> item
                | Choice2Of2 exn -> raise exn }
                
        member x.TryDequeAsync() = async {
            let! result = core.PostAndAsyncReply(fun reply -> TryDequeue reply)
            return
                match result with
                | Choice1Of2 item -> item
                | Choice2Of2 exn -> raise exn }
        member x.Enqueue(item) = x.EnqueueAsync item |> Async.RunSynchronously
        member x.Deque() = x.DequeAsync () |> Async.RunSynchronously
        member x.TryDeque() = x.TryDequeAsync () |> Async.RunSynchronously
    exception ReadCanceledException
    type MyIAsyncReadResult<'a> (callback:AsyncCallback, state) = 
        let event = new AutoResetEvent(false)
        let mutable completed = false
        let mutable canceled = false
        let mutable data = None
        let syncRoot = obj()
        
        interface IAsyncResult with
            member x.AsyncState with get() = state
            member x.IsCompleted with get() = completed
            member x.AsyncWaitHandle with get() = event :> WaitHandle
            member x.CompletedSynchronously with get() = false
        member x.End(resultData:option<'a>) =
            lock syncRoot (fun () ->
                if canceled then
                    raise ReadCanceledException
                data <- resultData
                event.Set() |> ignore
                if callback <> null then
                    callback.Invoke (x:>IAsyncResult)
                completed <- true)
        member x.Read 
            with get () = 
                data
        member x.Cancel() = 
            lock syncRoot (fun () ->
                if completed then
                    failwith "operation already completed!"
                canceled <- true)
        member x.IsCanceled with get() = canceled
        
    type IStream<'a> =
        inherit IDisposable
        abstract member Read : unit -> Async<'a option>
        abstract member Write : 'a -> Async<unit>
    type AsyncStreamHelper<'a> (innerStream:IStream<'a>) = 
        let queue = ConcurrentQueue<MyIAsyncReadResult<'a>>() 
        let workerCts = new CancellationTokenSource()
        let worker = 
            Async.StartAsTask (async {
                let! (cts:CancellationToken) = Async.CancellationToken
                while not cts.IsCancellationRequested do
                    let! data = innerStream.Read()
                    let finished = ref false
                    while not finished.Value do
                        let! (asyncResult:MyIAsyncReadResult<'a>) = queue.DequeAsync()
                        if not asyncResult.IsCanceled then
                            try
                                asyncResult.End(data)
                                finished.Value <- true
                            with ReadCanceledException -> () // find next
                return ()
            }, cancellationToken = workerCts.Token)
                
           
        let beginRead(callback, state) = 
            let result = new MyIAsyncReadResult<'a>(callback, state)
            queue.Enqueue result
            result :> IAsyncResult

        let endRead(asyncResult:IAsyncResult) = 
            let readResult = asyncResult :?> MyIAsyncReadResult<'a>
            if asyncResult.IsCompleted then
                readResult.Read
            else
                // block for exit
                WaitHandle.WaitAll [|asyncResult.AsyncWaitHandle|] |> ignore
                readResult.Read

        let cancelRead(asyncResult:IAsyncResult) =
            let readResult = asyncResult :?> MyIAsyncReadResult<'a>
            readResult.Cancel()

        let read ()  = 
            AsyncHelper.FromBeginEndCancel beginRead endRead cancelRead

        interface IStream<'a> with
            member x.Read() = read()
            member x.Write d = innerStream.Write(d)
            member x.Dispose () = innerStream.Dispose()
        member x.BaseStream with get() = innerStream
        static member FromAdvancedRead advancedRead count = 
            async {
                let buffer = Array.zeroCreate count
                let! read = advancedRead(buffer, 0, count)
                return Array.sub buffer 0 read
            }

    module StreamModule =
        let createUnsupported() =
            { new IStream<'a> with
                member x.Dispose () = ()
                member x.Read () = raise <| NotSupportedException ""
                member x.Write input = raise <| NotSupportedException "" }

        type StreamHelper(iStream:IStream<byte array>) =
            inherit Stream()
            let mutable cache = [||]
            let mutable currentIndex = 0
            let mutable isDisposed = false
            let read (dst:byte array) offset count = async {
                let! newCache = 
                    if cache.Length - currentIndex > 0 then
                        async.Return cache
                    else
                      async {
                        currentIndex <- 0
                        let! data = iStream.Read()
                        return
                            match data with
                            | Some d -> d
                            | None -> [||] }
                cache <- newCache
                // Use cache
                let realCount = 
                    Math.Min(
                        cache.Length - currentIndex,
                        count)
                Array.Copy(cache, currentIndex, dst, offset, realCount)
                currentIndex <- currentIndex + realCount
                return realCount }
            let write dst offset count = async {
                if count > 0 then
                    let newDst =
                         Array.sub dst offset count
                    return! iStream.Write newDst }
            let readOne () = async {
                let dst = Array.zeroCreate 1
                let! result = read dst 0 1
                return
                    if result = 0 then
                        None
                    else Some dst[0] }
                
            let writeOne b = iStream.Write [|b|]
            let _beginWrite, _endWrite, cancelRead = 
                Async.AsBeginEnd(fun (dst, offset, count) -> read dst offset count)
            let _beginWrite, _endWrite, cancelWrite = 
                Async.AsBeginEnd(fun (src, offset, count) -> write src offset count)
                
            let checkDisposed() =
                if isDisposed then
                    raise <| ObjectDisposedException("onetimestream")
            override x.ReadAsync (dst, offset, count, tok) = 
                Async.StartAsTask(read dst offset count, cancellationToken = tok)
            override x.WriteAsync (dst, offset, count, tok) = 
                Async.StartAsTask(write dst offset count, cancellationToken = tok)
                :> System.Threading.Tasks.Task
            override x.Flush () = ()        
            override x.Seek(_offset:int64, _origin:SeekOrigin) =
                raise <| NotSupportedException()        
            override x.SetLength(_value:int64) =
                raise <| NotSupportedException()        
            //override x.BeginRead(dst, offset, count, callback, state) =
            //    beginRead((dst, offset, count), callback, state)        
            //override x.EndRead(res) =
            //    endRead res
            member x.CancelRead(res) = 
                cancelRead(res)
            //override x.BeginWrite(src, offset, count, callback, state) =
            //    beginWrite((src, offset, count), callback, state)
            //override x.EndWrite(res) =
            //    endWrite res
            member x.CancelWrite(res) = 
                cancelWrite(res)
            override x.Read(dst, offset, count) = 
                read dst offset count |> Async.RunSynchronously            
            override x.Write(src, offset, count) = 
                write src offset count |> Async.RunSynchronously
            override x.ReadByte() =
                if isDisposed then -1
                else
                    match readOne() |> Async.RunSynchronously with
                    | Some s -> int s
                    | None -> -1
            override x.WriteByte item =
                if not isDisposed then
                    writeOne item |> Async.RunSynchronously        
            override x.CanRead 
                with get() = 
                    checkDisposed()
                    true        
            override x.CanSeek
                with get() =
                    checkDisposed()
                    false                
            override x.CanWrite
                with get() =
                    checkDisposed()
                    true        
            override x.Length
                with get() =
                    raise <| NotSupportedException()                
            override x.Position
                with get() =
                    raise <| NotSupportedException()
                and set _value =            
                    raise <| NotSupportedException()             
            override x.Dispose disposing =
                if not isDisposed then
                    isDisposed <- true
                    if disposing then
                        iStream.Dispose()
                base.Dispose disposing
                    
        let fromInterface iStream = new StreamHelper(iStream) :> Stream
        [<AutoOpen>]
        module StreamExtensions = 
            type Stream with
                member s.AsyncRead c = AsyncStreamHelper<_>.FromAdvancedRead (s.ReadAsync >> Async.AwaitTask) c
                member s.AsyncRead (buffer, offset, count) = Async.AwaitTask(s.ReadAsync(buffer, offset, count))
                member s.AsyncWrite (buffer, offset, count) = Async.AwaitTask(s.WriteAsync(buffer, offset, count))
            type IStream<'a> with
                member s.ReadWait() = 
                    s.Read() |> Async.RunSynchronously
                member s.WriteWait(d) = 
                    s.Write(d) |> Async.RunSynchronously

        open StreamExtensions
        let toCancelAbleStream s = new AsyncStreamHelper<_>(s) :> IStream<_>
        let fromReadWriteDispose dis read write = 
            { new IStream<_> with
                member x.Read () =
                    read()
                member x.Write item = 
                    write item
              interface IDisposable with
                member x.Dispose () = dis () } |> toCancelAbleStream
        let fromReadWrite read write = fromReadWriteDispose id read write
        let toInterface bufferSize (stream:Stream) = 
            let buffer = Array.zeroCreate bufferSize
            let read () = async {
                let! read = stream.ReadAsync(buffer, 0, buffer.Length)
                let readData =
                    Array.sub buffer 0 read
                return
                    if readData.Length > 0 then
                        Some readData
                    else None }
            let write (src:byte array) = async {
                do! stream.AsyncWrite(src, 0, src.Length)
                stream.Flush()
                }
            let dispose () = 
                // BUG: Make all AsyncRead calls end!
                stream.Flush()
                //stream.Close()
                stream.Dispose()
            fromReadWriteDispose dispose read write
        let toMaybeRead read () = async {
            let! data = read()
            return Some data }
            
            
        let infiniteStream () = 
            let queue = new ConcurrentQueue<'a>()
            fromReadWrite (toMaybeRead queue.DequeAsync) queue.EnqueueAsync

        let toLimitedStream (raw:IStream<_>) = 
            //let raw = infiniteStream()
            let readFinished = ref false
            let read () =
                if readFinished.Value then
                   async.Return None
                else 
                  async {
                    let! data = raw.Read()
                    return
                        match data with
                        | Some s -> 
                            match s with
                            | Some d -> Some d
                            | None -> 
                                readFinished.Value <- true
                                None
                        | None -> 
                            failwith "stream should not be limited as we are using an infiniteStream!" }
            let isFinished = ref false
            let finish () = async {
                do! raw.Write None
                isFinished.Value <- true }
            let write item = 
                if isFinished.Value then
                    failwith "stream is in finished state so it should not be written to!"
                raw.Write (Some item)
            finish,
            fromReadWriteDispose raw.Dispose read write
        let limitedStream () = toLimitedStream (infiniteStream())

        let createWriteOnlyPart onDispose (s:IStream<'a>) =
            { new IStream<'a> with
                member x.Dispose () = onDispose()
                member x.Read () = raise <| NotSupportedException "Read is not supported"
                member x.Write input = s.Write input }

        let buffer (stream:IStream<_>) =        
            let queue = infiniteStream()
            let write item = async {            
                do! queue.Write item
                do! stream.Write item }
            fromReadWrite queue.Read (fun _item -> invalidOp "Write is not allowed"),
            fromReadWriteDispose stream.Dispose stream.Read write

        let combineReadAndWrite (s1:IStream<_>) (s2:IStream<_>) = 
            fromReadWrite s1.Read s2.Write
        let appendFront data (s:IStream<_>) = 
            let first = ref true
            let read () = 
                if first.Value then
                    first.Value <- false
                    async.Return (Some data)
                else
                    s.Read()
            fromReadWriteDispose s.Dispose read s.Write

        let crossStream (s1:IStream<_>) (s2:IStream<_>) = 
            combineReadAndWrite s1 s2,
            combineReadAndWrite s2 s1
        let map f g (s:IStream<_>) = 
            let read () =  async {
                let! read = s.Read()
                return f read }
            let write item = s.Write (g item)
            fromReadWriteDispose s.Dispose read write
        
        let filterRead f (s:IStream<_>) =         
            let rec read () = async {
                let! data = s.Read()
                return!
                    if f data then
                        async.Return data
                    else
                        read () }
            fromReadWriteDispose s.Dispose read s.Write

        let filterWrite f (s:IStream<_>) = 
            let write item = 
                if f item then
                    s.Write item
                else async.Return ()
            fromReadWriteDispose s.Dispose s.Read write

        /// Duplicates the given stream, which means returning two stream instances
        /// which will read the same data. 
        /// At the same time buffers all data (ie read from s as fast as possible).
        /// Any data written to the returned instances will be written to the given instance.
        let duplicate (s:IStream<_>) = 
            let close1, s1 = limitedStream()
            let close2, s2 = limitedStream()
            
            let closed = ref false
            async {
                while not closed.Value do
                    let! data = s.Read()
                    match data with
                    | Some item ->
                        do! s1.Write item
                        do! s2.Write item
                    | None ->
                        do! close1()
                        do! close2() 
                        closed.Value <- true } |> Async.Start
            combineReadAndWrite s1 s,
            combineReadAndWrite s2 s

        let split f s = 
            let s1, s2 = duplicate s
            s1 |> filterRead f,
            s2 |> filterRead (not << f)
        //let toSeq (s:IStream<_>) = 
        //  asyncSeq {
        //    while true do
        //        let! data = s.Read()
        //        yield data }
        //let ofSeq write (s:AsyncSeq<_>) = 
        //    let current = ref s
        //    let read () = async {
        //        let! next = !current
        //        return
        //            match next with
        //            | Nil -> failwith "end of sequence"
        //            | Cons(item, next) ->
        //                current := next
        //                item }            
        //    fromReadWrite read write

        let redirect bufferLen (toStream:IStream<_>) (fromStream:IStream<_>) = 
            let closeRead = ref false 
            let cts = new CancellationTokenSource()
            let ev = new ManualResetEvent(false)
            let regularFinish = new ManualResetEvent(false)
            let redirectRun =
                async {
                    do! Async.SwitchToThreadPool()
                    try
                        let _buffer = Array.zeroCreate bufferLen
                        let streamFinished = ref false
                        while not closeRead.Value do
                            let! (read:Option<_>) = fromStream.Read()
                            closeRead.Value <-
                                match read with
                                | Some _s -> false
                                | None -> 
                                    streamFinished.Value <- true
                                    true

                            if read.IsSome then
                                do! toStream.Write(read.Value)                 
                        toStream.Dispose()
                        if streamFinished.Value then
                            fromStream.Dispose()
                        regularFinish.Set() |> ignore
                    finally
                        ev.Set() |> ignore
                } 
            let t = Async.StartAsTask(redirectRun, cancellationToken = cts.Token)
            let nT = 
                t.ContinueWith(
                    Action<Tasks.Task<unit>>(fun t ->
                        ev.Set() |> ignore))
            
            let closeRedirect (timeout:int) waitFinish =             
                let regularFinished = 
                    if waitFinish then
                        // BUG: Reset the timeout when we are still doing something
                        WaitHandle.WaitAll([|regularFinish :> WaitHandle|], timeout)
                    else false
                if not regularFinished then
                    closeRead.Value <- true
                    cts.Cancel()
                    ManualResetEvent.WaitAll [|ev|] |> ignore

            closeRedirect
        
        let defaultInput, defaultOutput, defaultError = Console.OpenStandardInput(), Console.OpenStandardOutput(), Console.OpenStandardError()
        
        let getStandardInput = 
            let iStream = toInterface 1024 defaultInput
            let modified = iStream |> fromInterface
            Console.SetIn(new StreamReader(modified))
            fun () -> iStream
        
        let getStandardOutput = 
            let iStream = defaultOutput |> toInterface 1024
            fun () -> iStream
        let getStandardError = 
            let iStream = defaultError |> toInterface 1024
            fun () -> iStream
