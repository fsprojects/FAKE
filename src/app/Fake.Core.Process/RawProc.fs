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

    let ofSeq l =
        empty.AddRange(l |> Seq.map (fun (k, v) -> KeyValuePair<_,_>(k, v)))

    let create() =
        ofSeq (Environment.environVars ())
        //|> IMap.add defaultEnvVar defaultEnvVar


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

/// Represents basically an "out" parameter, allows to retrieve a value after a certain point in time.
/// Used to retrieve "pipes"
type DataRef<'T> =
    internal { retrieveRaw : (unit -> 'T) ref }
    static member Empty =
        { retrieveRaw = ref (fun _ -> invalidOp "Can retrieve only when a process has been started!") } : DataRef<'T>
    static member Map f (inner:DataRef<'T>) =
        { retrieveRaw = ref (fun _ -> f inner.Value) }
    member x.Value = (!x.retrieveRaw)()

type StreamRef = DataRef<System.IO.Stream>

/// Various options to redirect streams.
type StreamSpecification =
    /// Do not redirect, or use normal process inheritance
    | Inherit
    /// Redirect to the given stream (the stream is provided by the user and is written only for 'stdout' & 'stderr' and read only for 'stdin')
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
        WorkingDirectory : string option
        Environment : EnvMap option
        Streams : StreamSpecs
        OutputHook : IRawProcessHook
    }
    member internal x.ToStartInfo =
        let p = new System.Diagnostics.ProcessStartInfo()
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
                            startProcessRaw toolProcess
                        finally
                            setEcho false |> ignore
                        c.OutputHook.OnStart (toolProcess)
                        
                        let handleStream parameter processStream isInputStream =
                            async {
                                match parameter with
                                | Inherit ->
                                    return failwithf "Unexpected value"
                                | UseStream (shouldClose, stream) ->
                                    if isInputStream then
                                        do! stream.CopyToAsync(processStream, 81920, tok.Token)
                                            |> Async.AwaitTaskWithoutAggregate
                                    else
                                        do! processStream.CopyToAsync(stream, 81920, tok.Token)
                                            |> Async.AwaitTaskWithoutAggregate
                                    return
                                        if shouldClose then stream else Stream.Null
                                | CreatePipe (r) ->
                                    r.retrieveRaw := fun _ -> processStream
                                    return Stream.Null
                            }
        
                        if p.RedirectStandardInput then
                            redirectStdInTask <-
                              handleStream streamSpec.StandardInput toolProcess.StandardInput.BaseStream true
                              // Immediate makes sure we set the ref cell before we return...
                              |> fun a -> Async.StartImmediateAsTask(a, cancellationToken = tok.Token)
                              
                        if p.RedirectStandardOutput then
                            readOutputTask <-
                              handleStream streamSpec.StandardOutput toolProcess.StandardOutput.BaseStream false
                              // Immediate makes sure we set the ref cell before we return...
                              |> fun a -> Async.StartImmediateAsTask(a, cancellationToken = tok.Token)
        
                        if p.RedirectStandardError then
                            readErrorTask <-
                              handleStream streamSpec.StandardError toolProcess.StandardError.BaseStream false
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
                            if not ok then
                                Trace.traceFAKE "At least one redirection task did not finish: \nReadErrorTask: %O, ReadOutputTask: %O, RedirectStdInTask: %O" readErrorTask.Status readOutputTask.Status redirectStdInTask.Status
                            allFinished <- ok
                        //tok.Cancel()
                        
                        // wait for finish -> AwaitTask has a bug which makes it unusable for chanceled tasks.
                        // workaround with continuewith
                        let! streams = all.ContinueWith (new System.Func<System.Threading.Tasks.Task<Stream[]>, Stream[]> (fun t -> t.GetAwaiter().GetResult())) |> Async.AwaitTaskWithoutAggregate
                        for s in streams do s.Dispose()
                        
                        return { RawExitCode = code } 
                    }
                    |> Async.StartImmediateAsTask

                return exitCode }
        }
