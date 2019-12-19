namespace Fake.Core

open System
open System.Reflection
open Fake.Core
open Fake.Core.ProcessHelpers
open System.Collections.Immutable
open System.Collections.Generic


type IMap<'TKey, 'TValue> = IImmutableDictionary<'TKey, 'TValue>
module IMap =
    let inline empty<'key, 'value> = ImmutableDictionary.Empty :> IMap<'key, 'value>
    let inline tryFind k (m:IMap<_,_>) =
        match m.TryGetValue k with
        | true, v -> Some v
        | _ -> None
    let inline remove k (m:IMap<_,_>) : IMap<_,_> =
        m.Remove(k)
    let inline iter f (m:IMap<_,_>) =
        for kv in m do
            f kv.Key kv.Value
    let inline add k v (m:IMap<_,_>) : IMap<_,_> =
        m.SetItem(k, v)
    let inline toSeq (m:IMap<_,_>) :seq<_ * _> =
        m |> Seq.map (fun kv -> kv.Key, kv.Value)

type EnvMap = IMap<string, string>
module EnvMap =
    let empty = 
        if Environment.isWindows
        then ImmutableDictionary.Empty.WithComparers(StringComparer.OrdinalIgnoreCase) :> EnvMap
        else IMap.empty

    let ofSeq l : EnvMap =
        empty.AddRange(l |> Seq.map (fun (k, v) -> KeyValuePair<_,_>(k, v)))

    let create() =
        ofSeq (Environment.environVars ())

    let replace (l) (e:EnvMap) : EnvMap=
        e.SetItems(l)
        //|> IMap.add defaultEnvVar defaultEnvVar

    let ofMap (l) : EnvMap =
        create()
        |> replace l

/// The type of command to execute
type Command =
    | ShellCommand of string
    /// Windows: https://msdn.microsoft.com/en-us/library/windows/desktop/bb776391(v=vs.85).aspx
    /// Linux(mono): https://github.com/mono/mono/blob/0bcbe39b148bb498742fc68416f8293ccd350fb6/eglib/src/gshell.c#L32-L104 (because we need to create a commandline string internally which need to go through that code)
    /// Linux(netcore): See https://github.com/fsharp/FAKE/pull/1281/commits/285e585ec459ac7b89ca4897d1323c5a5b7e4558 and https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L443-L522
    | RawCommand of executable:FilePath * arguments:Arguments
    
    member x.CommandLine =
        match x with
        | ShellCommand s -> s
        | RawCommand (f, arg) -> sprintf "%s %s" f arg.ToWindowsCommandLine

    member x.Arguments =
        match x with
        | ShellCommand _ -> raise <| NotImplementedException "Cannot retrieve Arguments for ShellCommand"
        | RawCommand (_, arg) -> arg

    member x.Executable =
        match x with
        | ShellCommand _ -> raise <| NotImplementedException "Cannot retrieve Executable for ShellCommand"
        | RawCommand (f, _) -> f

/// Represents basically an "out" parameter, allows to retrieve a value after a certain point in time.
/// Used to retrieve "pipes"
type DataRef<'T> =
    internal { mutable value : 'T option; mutable onSet : 'T -> unit }
    static member Empty =
        { value = None; onSet = ignore } : DataRef<'T>
    static member Map f (inner:DataRef<'T>) =
        let newCell = { value = inner.value |> Option.map f; onSet = fun _ -> invalidOp "cannot set this ref cell" }
        let previousSet = inner.onSet
        inner.onSet <- fun newVal -> previousSet newVal; newCell.value <- Some (f newVal)
        newCell
    member x.Value = match x.value with Some s -> s | None -> invalidOp "Cannot retrieve data cell before it has been set!"

type StreamRef = DataRef<System.IO.Stream>

/// Various options to redirect streams.
type StreamSpecification =
    /// Do not redirect, or use normal process inheritance
    | Inherit
    /// Redirect to the given stream (the stream must be provided by the user and be writeable for 'stdout' & 'stderr' and readable for 'stdin')
    | UseStream of closeOnExit:bool * stream:System.IO.Stream
    /// Retrieve the raw pipe from the process (the StreamRef is set with a stream you can write into for 'stdin' and read from for 'stdout' and 'stderr')
    | CreatePipe of StreamRef // The underlying framework creates pipes already

type internal StreamSpecs =
    { StandardInput : StreamSpecification 
      StandardOutput : StreamSpecification 
      StandardError : StreamSpecification }
    member x.SetStartInfo (p:System.Diagnostics.ProcessStartInfo) =
        match x.StandardInput with
        | Inherit ->
            p.RedirectStandardInput <- false
        | UseStream _ | CreatePipe _ ->
            p.RedirectStandardInput <- true
        match x.StandardOutput with
        | Inherit ->
            p.RedirectStandardOutput <- false
        | UseStream _ | CreatePipe _ ->
            p.RedirectStandardOutput <- true
        match x.StandardError with
        | Inherit ->
            p.RedirectStandardError <- false
        | UseStream _ | CreatePipe _ ->
            p.RedirectStandardError <- true
                

type internal IRawProcessHook =
    abstract member Prepare : StreamSpecs -> StreamSpecs
    abstract member OnStart : System.Diagnostics.Process -> unit
    //abstract member Retrieve : IDisposable * System.Threading.Tasks.Task<int> -> Async<'TRes>

/// A raw (untyped) way to start a process
type internal RawCreateProcess =
    internal {
        Command : Command
        TraceCommand : bool
        WorkingDirectory : string option
        Environment : EnvMap option
        Streams : StreamSpecs
        OutputHook : IRawProcessHook
    }
    member internal x.ToStartInfo =
        let p = System.Diagnostics.ProcessStartInfo()
        match x.Command with
        | ShellCommand s ->
            p.UseShellExecute <- true
            p.FileName <- s
            p.Arguments <- null
        | RawCommand (filename, args) ->
            p.UseShellExecute <- false
            p.FileName <- filename
            p.Arguments <- args.ToStartInfo
        let setEnv key var =
            p.Environment.[key] <- var
        x.Environment
            |> Option.iter (fun env ->
                p.Environment.Clear()
                env |> IMap.iter (fun key value -> setEnv key value))
#if FX_WINDOWSTLE    
        p.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden
#endif
        match x.WorkingDirectory with
        | Some work ->
            p.WorkingDirectory <- work
        | None -> ()        
        p

    member x.CommandLine = x.Command.CommandLine

type RawProcessResult = { RawExitCode : int }

type internal IProcessStarter =
    abstract Start : RawCreateProcess -> Async<System.Threading.Tasks.Task<RawProcessResult>>

module internal RawProc =

    // mono sets echo off for some reason, therefore interactive mode doesn't work as expected
    // this enables this tty feature which makes the interactive mode work as expected
    let private setEcho (b:bool) =
        if Environment.isMono then
            // See https://github.com/mono/mono/blob/master/mcs/class/corlib/System/ConsoleDriver.cs#L289
            let t =
                match System.Type.GetType("System.ConsoleDriver") with
                | null -> null
                | cd -> cd.GetTypeInfo()
            let flags = System.Reflection.BindingFlags.Static ||| System.Reflection.BindingFlags.NonPublic
            if isNull t then
                Trace.traceFAKE "Expected to find System.ConsoleDriver type"
                false
            else
                let setEchoMethod = t.GetMethod("SetEcho", flags)
                if isNull setEchoMethod then
                    Trace.traceFAKE "Expected to find System.ConsoleDriver.SetEcho"
                    false
                else
                    setEchoMethod.Invoke(null, [| b :> obj |]) :?> bool
        else false
        
    open System.Diagnostics
    open System.IO
    let internal createProcessStarter startProcessRaw =
        { new IProcessStarter with
            member __.Start c = async {
                let p = c.ToStartInfo
                let streamSpec = c.OutputHook.Prepare c.Streams
                streamSpec.SetStartInfo p

                let toolProcess = new Process(StartInfo = p)
                
                let mutable isStarted = false
                let mutable startTrigger = System.Threading.Tasks.TaskCompletionSource<_>()
                let mutable readOutputTask = System.Threading.Tasks.Task.FromResult Stream.Null
                let mutable readErrorTask = System.Threading.Tasks.Task.FromResult Stream.Null
                let mutable redirectStdInTask = System.Threading.Tasks.Task.FromResult Stream.Null
                let tok = new System.Threading.CancellationTokenSource()
                let start() =
                    if not <| isStarted then
                        toolProcess.EnableRaisingEvents <- true
                        setEcho true |> ignore
                        try
                            startProcessRaw c toolProcess
                        finally
                            setEcho false |> ignore
                        c.OutputHook.OnStart (toolProcess)
                        
                        let handleStream originalParameter parameter processStream isInputStream =
                            async {
                                match parameter with
                                | Inherit ->
                                    return failwithf "Unexpected value"
                                | UseStream (shouldClose, stream) ->
                                    if isInputStream then
                                        do! stream.CopyToAsync(processStream, 81920, tok.Token)
                                            |> Async.AwaitTaskWithoutAggregate
                                        processStream.Close()
                                    else
                                        do! processStream.CopyToAsync(stream, 81920, tok.Token)
                                            |> Async.AwaitTaskWithoutAggregate
                                    return
                                        if shouldClose then stream else Stream.Null
                                | CreatePipe (r) ->
                                    match originalParameter with
                                    | CreatePipe o ->
                                        // first set the "original" cell
                                        o.value <- Some processStream
                                        // Call onSet to "produce" the high-level stream
                                        o.onSet processStream
                                        // Set the "high"-level stream to the "original" cell
                                        let stream = r.Value
                                        o.value <- Some stream
                                        if not (obj.ReferenceEquals(o, r)) then
                                            // Mark the "high"-level stream empty in order to prevent invalid usage
                                            r.value <- None
                                    | _ -> failwithf "Unexpected value"
                                    return Stream.Null
                            }
        
                        if p.RedirectStandardInput then
                            redirectStdInTask <-
                              handleStream c.Streams.StandardInput streamSpec.StandardInput toolProcess.StandardInput.BaseStream true
                              // Immediate makes sure we set the ref cell before we return...
                              |> fun a -> Async.StartImmediateAsTask(a, cancellationToken = tok.Token)
                              
                        if p.RedirectStandardOutput then
                            readOutputTask <-
                              handleStream c.Streams.StandardOutput streamSpec.StandardOutput toolProcess.StandardOutput.BaseStream false
                              // Immediate makes sure we set the ref cell before we return...
                              |> fun a -> Async.StartImmediateAsTask(a, cancellationToken = tok.Token)
        
                        if p.RedirectStandardError then
                            readErrorTask <-
                              handleStream c.Streams.StandardError streamSpec.StandardError toolProcess.StandardError.BaseStream false
                              // Immediate makes sure we set the ref cell before we return...
                              |> fun a -> Async.StartImmediateAsTask(a, cancellationToken = tok.Token)
            
                let syncStart () =
                    try
                        start()
                        startTrigger.SetResult()
                    with e -> startTrigger.SetException(e)

                // Wait for the process to finish
                let exitEvent = 
                    toolProcess.Exited
                        // This way the handler gets added before actually calling start or "EnableRaisingEvents"
                        |> Event.guard syncStart
                        |> Async.AwaitEvent
                        |> Async.StartImmediateAsTask

                do! startTrigger.Task |> Async.AwaitTaskWithoutAggregate
                let exitCode =
                    async {
                        do! exitEvent |> Async.AwaitTaskWithoutAggregate |> Async.Ignore
                        // Waiting for the process to exit (buffers)
                        toolProcess.WaitForExit()
                
                        let code = toolProcess.ExitCode
                        toolProcess.Dispose()
                
                        let all =  System.Threading.Tasks.Task.WhenAll([readErrorTask; readOutputTask; redirectStdInTask])
                        let tryWait () =
                            async {
                                let delay = System.Threading.Tasks.Task.Delay 500
                                let! t =
                                    System.Threading.Tasks.Task.WhenAny(all, delay)
                                    |> Async.AwaitTaskWithoutAggregate
                                return t <> delay
                            }
                        let mutable allFinished = false
                        let mutable retries = 10
                        while not allFinished && retries > 0 do
                            let! ok = tryWait()
                            retries <- retries - 1
                            if retries = 2 then
                                tok.Cancel()
                            if not ok && retries < 6 then
                                Trace.traceFAKE "At least one redirection task did not finish: \nReadErrorTask: %O, ReadOutputTask: %O, RedirectStdInTask: %O" readErrorTask.Status readOutputTask.Status redirectStdInTask.Status
                            allFinished <- ok
                        
                        // wait for finish -> AwaitTask has a bug which makes it unusable for chanceled tasks.
                        // workaround with continuewith
                        if allFinished || Environment.GetEnvironmentVariable("FAKE_DEBUG_PROCESS_HANG") = "true" then
                            if not allFinished && Environment.GetEnvironmentVariable("FAKE_ATTACH_DEBUGGER") = "true" then
                                System.Diagnostics.Debugger.Launch() |> ignore
                                System.Diagnostics.Debugger.Break() |> ignore
                            if not allFinished && Environment.GetEnvironmentVariable("FAKE_FAIL_PROCESS_HANG") = "true" then
                                Environment.FailFast(sprintf "At least one redirection task did not finish: \nReadErrorTask: %O, ReadOutputTask: %O, RedirectStdInTask: %O" readErrorTask.Status readOutputTask.Status redirectStdInTask.Status)

                            // wait for finish -> AwaitTask has a bug which makes it unusable for chanceled tasks.
                            // workaround with continuewith
                            let! streams = all.ContinueWith (new System.Func<System.Threading.Tasks.Task<Stream[]>, Stream[]> (fun t -> t.GetAwaiter().GetResult())) |> Async.AwaitTaskWithoutAggregate
                            for s in streams do s.Dispose()
                        else
                            let msg = "We encountered https://github.com/fsharp/FAKE/issues/2401, please help to resolve this issue! You can set 'FAKE_IGNORE_PROCESS_HANG' to true to ignore this error. But please consider sending a full process dump or volunteer with debugging."
                            if Environment.GetEnvironmentVariable("FAKE_IGNORE_PROCESS_HANG") <> "true" then
                                failwith msg
                            else
                                async {
                                    try
                                        let! streams = all.ContinueWith (new System.Func<System.Threading.Tasks.Task<Stream[]>, Stream[]> (fun t -> t.GetAwaiter().GetResult())) |> Async.AwaitTaskWithoutAggregate
                                        Trace.traceFAKE "The hanging redirect task has finished eventually! Disposing streams."
                                        for s in streams do s.Dispose()
                                    with e ->
                                        Trace.traceFAKE "Waiting for the hanging redirect task failed: %O" e
                                }
                                |> Async.Start
                                Trace.traceFAKE "%s" msg
                            
                        return { RawExitCode = code } 
                    }
                    |> Async.StartImmediateAsTask

                return exitCode }
        }
