namespace Fake.Core

open System
open System.IO
open System.Diagnostics
open Fake.Core.ProcessHelpers

/// Hook for events when an CreateProcess is executed.
type internal IProcessHook<'TRes> =
    abstract member PrepareState : unit -> IDisposable
    abstract member PrepareStreams : IDisposable * StreamSpecs -> StreamSpecs
    abstract member ProcessStarted : IDisposable * System.Diagnostics.Process -> unit
    abstract member RetrieveResult : IDisposable * System.Threading.Tasks.Task<RawProcessResult> -> Async<'TRes>

type internal IProcessHookImpl<'TState, 'TRes when 'TState :> IDisposable> =
    abstract member PrepareState : unit -> 'TState
    abstract member PrepareStreams : 'TState * StreamSpecs -> StreamSpecs
    abstract member ProcessStarted : 'TState * System.Diagnostics.Process -> unit
    abstract member RetrieveResult : 'TState * System.Threading.Tasks.Task<RawProcessResult> -> Async<'TRes>

module internal ProcessHook =
    let toRawHook (h:IProcessHookImpl<'TState,'TRes>) =
        { new IProcessHook<'TRes> with
            member x.PrepareState () = 
                let state = h.PrepareState ()
                state :> IDisposable
            member x.PrepareStreams (state, specs) = 
                h.PrepareStreams (state :?> 'TState, specs)
            member x.ProcessStarted (state, proc) =
                h.ProcessStarted (state :?> 'TState, proc)
            member x.RetrieveResult (state, exitCode) =
                h.RetrieveResult (state :?> 'TState, exitCode) }


/// The output of the process. If ordering between stdout and stderr is important you need to use streams.
type ProcessOutput = { Output : string; Error : string }

type ProcessResult<'a> = { Result : 'a; ExitCode : int }

/// Generator for results
//type ResultGenerator<'TRes> =
//    {   GetRawOutput : unit -> ProcessOutput
//        GetResult : ProcessOutput -> 'TRes }
/// Handle for creating a process and returning potential results.
type CreateProcess<'TRes> =
    internal {
        Command : Command
        TraceCommand : bool
        WorkingDirectory : string option
        Environment : EnvMap option
        Streams : StreamSpecs
        Hook : IProcessHook<'TRes>
    }

    //member x.OutputRedirected = x.HasRedirect 
    member x.CommandLine = x.Command.CommandLine


/// Module for creating and modifying CreateProcess<'TRes> instances
module CreateProcess =
    let internal emptyHook =
        { new IProcessHook<ProcessResult<unit>> with
            member __.PrepareState () = null
            member __.PrepareStreams (_, specs) = specs
            member __.ProcessStarted (_,_) = ()
            member __.RetrieveResult (_, t) =
                async {
                    let! raw = Async.AwaitTaskWithoutAggregate t
                    return { ExitCode = raw.RawExitCode; Result = () } 
                } }

    (*let internal ofProc (x:RawCreateProcess) =
      { Command = x.Command
        WorkingDirectory = x.WorkingDirectory
        Environment = x.Environment
        Streams = x.Streams
        Hook =
            { new IProcessHook<int> with
                member __.PrepareStart specs = x.OutputHook.Prepare specs
                member __.ProcessStarted (state, p) = x.OutputHook.OnStart(state, p)
                member __.RetrieveResult (s, t) = 
                    x.OutputHook.Retrieve(s, t) } }*)

    let fromCommand command =
        {   Command = command
            WorkingDirectory = None
            TraceCommand = true
            // Problem: Environment not allowed when using ShellCommand
            Environment = None
            Streams =
                { // Problem: Redirection not allowed when using ShellCommand
                  StandardInput = Inherit
                  // Problem: Redirection not allowed when using ShellCommand
                  StandardOutput = Inherit
                  // Problem: Redirection not allowed when using ShellCommand
                  StandardError = Inherit }
            Hook = emptyHook }
    let fromRawWindowsCommandLine command windowsCommandLine =
        fromCommand <| RawCommand(command, Arguments.OfWindowsCommandLine windowsCommandLine)
    let fromRawCommand command args =
        fromCommand <| RawCommand(command, Arguments.OfArgs args)

    let ofStartInfo (p:System.Diagnostics.ProcessStartInfo) =
        {   Command = if p.UseShellExecute then ShellCommand p.FileName else RawCommand(p.FileName, Arguments.OfStartInfo p.Arguments)
            TraceCommand = true
            WorkingDirectory = if System.String.IsNullOrWhiteSpace p.WorkingDirectory then None else Some p.WorkingDirectory
            Environment = 
                p.Environment
                |> Seq.map (fun kv -> kv.Key, kv.Value)
                |> EnvMap.ofSeq
                |> Some
            Streams =
                {   StandardInput = if p.RedirectStandardError then CreatePipe StreamRef.Empty else Inherit
                    StandardOutput = if p.RedirectStandardError then CreatePipe StreamRef.Empty else Inherit
                    StandardError = if p.RedirectStandardError then CreatePipe StreamRef.Empty else Inherit
                }
            Hook = emptyHook
        }
    let internal interceptStreamFallback onInherit target (s:StreamSpecification) =
        match s with
        | Inherit -> onInherit()
        | UseStream (close, stream) ->
            let combined = Stream.CombineWrite(stream, target)
            UseStream(close, combined)
        | CreatePipe pipe ->
            CreatePipe (StreamRef.Map (fun s -> Stream.InterceptStream(s, target)) pipe)
    let interceptStream target (s:StreamSpecification) =
        interceptStreamFallback (fun _ -> failwithf "cannot intercept stream when it is not redirected. Please redirect the stream first!") target s
    
    let copyRedirectedProcessOutputsToStandardOutputs (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardOutput =
                        let stdOut = System.Console.OpenStandardOutput()
                        interceptStream stdOut c.Streams.StandardOutput
                    StandardError =
                        let stdErr = System.Console.OpenStandardError()
                        interceptStream stdErr c.Streams.StandardError } }
    
    let withWorkingDirectory workDir (c:CreateProcess<_>)=
        { c with
            WorkingDirectory = Some workDir }

    let disableTraceCommand (c:CreateProcess<_>)=
        { c with
            TraceCommand = false }
    
    let withCommand command (c:CreateProcess<_>)=
        { c with
            Command = command }

    let replaceFilePath newFilePath (c:CreateProcess<_>)=
        { c with
            Command =
                match c.Command with
                | ShellCommand s -> failwith "Expected RawCommand"
                | RawCommand (_, c) -> RawCommand(newFilePath, c) }
    let mapFilePath f (c:CreateProcess<_>)=
        c
        |> replaceFilePath (f (match c.Command with ShellCommand s -> failwith "Expected RawCommand" | RawCommand (file, _) -> f file))


    let internal withHook h (c:CreateProcess<_>) =
      { Command = c.Command
        TraceCommand = c.TraceCommand
        WorkingDirectory = c.WorkingDirectory
        Environment = c.Environment
        Streams = c.Streams
        Hook = h }
    let internal withHookImpl h (c:CreateProcess<_>) =
        c
        |> withHook (h |> ProcessHook.toRawHook)

    let internal simpleHook prepareState prepareStreams onStart onResult =
        { new IProcessHookImpl<_, _> with
            member __.PrepareState () =
                prepareState ()
            member __.PrepareStreams (state, streams) =
                prepareStreams state streams
            member __.ProcessStarted (state, p) = 
                onStart state p
            member __.RetrieveResult (state, exitCode) = 
                onResult state exitCode }

    type internal CombinedState<'a when 'a :> IDisposable > =
        { State1 : IDisposable; State2 : 'a }
        interface IDisposable with
            member x.Dispose() =
                if not (isNull x.State1) then
                    x.State1.Dispose()
                x.State2.Dispose()
    let internal hookAppendFuncs prepareState prepareStreams onStart onResult (c:IProcessHook<'TRes>) =    
        { new IProcessHookImpl<_, _> with
            member __.PrepareState () =
                let state1 = c.PrepareState ()
                let state2 = prepareState ()
                { State1 = state1; State2 = state2 }
            member __.PrepareStreams (state, streams) =
                let newStreams = c.PrepareStreams(state.State1, streams)
                let finalStreams = prepareStreams state.State2 newStreams
                finalStreams
            member __.ProcessStarted (state, p) =
                c.ProcessStarted (state.State1, p)
                onStart state.State2 p
            member __.RetrieveResult (state, exitCode) = 
                async {
                    let d = c.RetrieveResult(state.State1, exitCode)
                    return! onResult d state.State2 exitCode
                } }

    let internal appendFuncs prepareState prepareStreams onStart onResult (c:CreateProcess<_>) =
        c
        |> withHookImpl (
            c.Hook
            |> hookAppendFuncs prepareState prepareStreams onStart onResult
        )

    type internal DisposableWrapper<'a> =
        { State: 'a; OnDispose : 'a -> unit }
        interface IDisposable with
            member x.Dispose () = x.OnDispose x.State
        
    let internal appendFuncsDispose prepareState prepareStreams onStart onResult onDispose (c:CreateProcess<_>) =
        c
        |> appendFuncs
            (fun () ->
                let state = prepareState ()
                { State = state; OnDispose = onDispose })
            (fun state streams -> prepareStreams state.State streams)
            (fun state p -> onStart state.State p)
            (fun prev state exitCode -> onResult prev state.State exitCode)   
    let appendSimpleFuncs prepareState onStart onResult onDispose (c:CreateProcess<_>) =
        c
        |> appendFuncsDispose
            prepareState
            (fun state streams -> streams)
            onStart
            onResult
            onDispose                     

    let addOnSetup f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            (fun _ -> f())
            (fun state p -> ())
            (fun prev state exitCode -> prev)
            (fun _ -> ())
    let addOnFinally f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            (fun _ -> ())
            (fun state p -> ())
            (fun prev state exitCode -> prev)
            (fun _ -> f ())
    let addOnStarted f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            (fun _ -> ())
            (fun state p -> f ())
            (fun prev state exitCode -> prev)
            (fun _ -> ())

    let withEnvironment (env: (string * string) list) (c:CreateProcess<_>)=
        { c with
            Environment = Some (EnvMap.ofSeq env) }
            
    let withEnvironmentMap (env: EnvMap) (c:CreateProcess<_>)=
        { c with
            Environment = Some env }
    let getEnvironmentMap (c:CreateProcess<_>)=
        match c.Environment with
        | Some en -> en
        | None -> EnvMap.create()

    let setEnvironmentVariable envKey (envVar:string) (c:CreateProcess<_>) =
        { c with
            Environment =
                getEnvironmentMap c
                |> IMap.add envKey envVar
                |> Some }

    let withStandardOutput stdOut (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardOutput = stdOut } }
    let withStandardError stdErr (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardError = stdErr } }
    let withStandardInput stdIn (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardInput = stdIn } }

    let map f c =
        c
        |> appendSimpleFuncs 
            (fun _ -> ())
            (fun state p -> ())
            (fun prev state exitCode -> 
                async {
                    let! old = prev
                    return f old
                })
            (fun _ -> ())
    
    let mapResult f (c:CreateProcess<ProcessResult<_>>) =
        c
        |> map (fun r ->
            { ExitCode = r.ExitCode; Result = f r.Result })
    let redirectOutput (c:CreateProcess<_>) =
        c
        |> appendFuncsDispose 
            (fun streams ->
                let outMem = new MemoryStream()
                let errMem = new MemoryStream()
                outMem, errMem)
            (fun (outMem, errMem) streams ->
                { streams with
                    StandardOutput =
                        interceptStreamFallback (fun _ -> UseStream (false, outMem)) outMem streams.StandardOutput
                    StandardError =
                        interceptStreamFallback (fun _ -> UseStream (false, errMem)) errMem streams.StandardError
                })
            (fun (outMem, errMem) p -> ())
            (fun prev (outMem, errMem) exitCode ->
                async {
                    let! prevResult = prev
                    let! exitCode = exitCode |> Async.AwaitTaskWithoutAggregate
                    outMem.Position <- 0L
                    errMem.Position <- 0L
                    let stdErr = (new StreamReader(errMem)).ReadToEnd()
                    let stdOut = (new StreamReader(outMem)).ReadToEnd()
                    let r = { Output = stdOut; Error = stdErr }
                    return { ExitCode = exitCode.RawExitCode; Result = r }
                })
            (fun (outMem, errMem) ->
                outMem.Dispose()
                errMem.Dispose())
  
    let withOutputEvents onStdOut onStdErr (c:CreateProcess<_>) =
        let watchStream onF (stream:System.IO.Stream) =
            async {
                let reader = new System.IO.StreamReader(stream)
                let mutable finished = false
                while not finished do
                    let! line = reader.ReadLineAsync()
                    finished <- isNull line
                    onF line
            }
            |> fun a -> Async.StartImmediateAsTask(a)
        c
        |> appendFuncsDispose
            (fun () ->
                let closeOut, outMem = InternalStreams.StreamModule.limitedStream()
                let closeErr, errMem = InternalStreams.StreamModule.limitedStream()
                
                let outMemS = InternalStreams.StreamModule.fromInterface outMem
                let errMemS = InternalStreams.StreamModule.fromInterface errMem
                let tOut = watchStream onStdOut outMemS
                let tErr = watchStream onStdErr errMemS
                (closeOut, outMem, closeErr, errMem, tOut, tErr))
            (fun (closeOut, outMem, closeErr, errMem, tOut, tErr) streams ->
                { streams with
                    StandardOutput =
                        outMem
                        |> InternalStreams.StreamModule.createWriteOnlyPart (fun () -> closeOut() |> Async.RunSynchronously)
                        |> InternalStreams.StreamModule.fromInterface
                        |> fun s -> interceptStream s streams.StandardOutput
                    StandardError =
                        errMem
                        |> InternalStreams.StreamModule.createWriteOnlyPart (fun () -> closeErr() |> Async.RunSynchronously)
                        |> InternalStreams.StreamModule.fromInterface
                        |> fun s -> interceptStream s streams.StandardError 
                })
            (fun state p -> ())
            (fun prev (closeOut, outMem, closeErr, errMem, tOut, tErr) exitCode ->
                async {
                    let! prevResult = prev
                    do! closeOut()
                    do! closeErr()
                    do! tOut
                    do! tErr
                    return prevResult
                })
            (fun (closeOut, outMem, closeErr, errMem, tOut, tErr) ->
                
                outMem.Dispose()
                errMem.Dispose())
    
    let addOnExited f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            (fun _ -> ())
            (fun state p -> ())
            (fun prev state exitCode -> 
                async {
                    let! prevResult = prev
                    let! e = exitCode
                    let s = f prevResult e.RawExitCode
                    return s
                })
            (fun _ -> ())
        
    let ensureExitCodeWithMessage msg (r:CreateProcess<_>) =
        r
        |> addOnExited (fun data exitCode ->
            if exitCode <> 0 then failwith msg
            else data)
            

    let internal tryGetOutput (data:obj) =
        match data with
        | :? ProcessResult<ProcessOutput> as output ->
            Some output.Result
        | :? ProcessOutput as output ->
            Some output
        | _ -> None
        
    let ensureExitCode (r:CreateProcess<_>) =
        r
        |> addOnExited (fun data exitCode ->
            if exitCode <> 0 then
                let output = tryGetOutput (data :> obj)
                let msg =
                    match output with
                    | Some output ->
                        (sprintf "Process exit code '%d' <> 0. Command Line: %s\nStdOut: %s\nStdErr: %s" exitCode r.CommandLine output.Output output.Error)
                    | None ->
                        (sprintf "Process exit code '%d' <> 0. Command Line: %s" exitCode r.CommandLine)                
                failwith msg
            else
                data
                )
    
    let warnOnExitCode msg (r:CreateProcess<_>) =
        r
        |> addOnExited (fun data exitCode ->
            if exitCode <> 0 then
                let output = tryGetOutput (data :> obj)
                let msg =
                    match output with
                    | Some output ->
                        (sprintf "%s. exit code '%d' <> 0. Command Line: %s\nStdOut: %s\nStdErr: %s" msg exitCode r.CommandLine output.Output output.Error)
                    | None ->
                        (sprintf "%s. exit code '%d' <> 0. Command Line: %s" msg exitCode r.CommandLine)
                //if Env.isVerbose then
                eprintfn "%s" msg    
            else data)

    type internal TimeoutState =
        { Stopwatch : System.Diagnostics.Stopwatch
          mutable HasExited : bool }
    let withTimeout (timeout:System.TimeSpan) (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            (fun _ -> 
                { Stopwatch = System.Diagnostics.Stopwatch.StartNew()
                  HasExited = false })
            (fun state proc -> 
                state.Stopwatch.Restart()
                async {
                    let ms = int64 timeout.TotalMilliseconds
                    let msMax = int <| Math.Min(ms, int64 Int32.MaxValue)
                    do! Async.Sleep(msMax)
                    try
                        if not state.HasExited && not proc.HasExited then
                            proc.Kill()
                    with exn ->
                        Trace.traceError 
                        <| sprintf "Could not kill process %s  %s after timeout: %O" proc.StartInfo.FileName 
                               proc.StartInfo.Arguments exn
                }
                |> Async.StartImmediate)
            (fun prev state exitCode -> 
                async {
                    let! e = exitCode |> Async.AwaitTaskWithoutAggregate
                    state.HasExited <- true
                    state.Stopwatch.Stop()
                    let! prevResult = prev
                    match e.RawExitCode with
                    | 0 ->
                        return prevResult
                    | _ when state.Stopwatch.Elapsed > timeout -> 
                        return failwithf "Process '%s' timed out." c.CommandLine
                    | _ ->
                        return prevResult
                })
            (fun state -> state.Stopwatch.Stop())

    type internal ProcessState =
        { mutable Process : Process }
    let internal getProcess (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            (fun _ -> { Process = null })
            (fun state proc -> 
                state.Process <- proc)
            (fun prev state exitCode -> 
                async.Return (state.Process, prev, exitCode))
            (fun state -> ())
