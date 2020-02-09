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

/// Handle for creating a process and returning potential results.
type CreateProcess<'TRes> =
    internal {
        InternalCommand : Command
        TraceCommand : bool
        InternalWorkingDirectory : string option
        InternalEnvironment : EnvMap option
        Streams : StreamSpecs
        Hook : IProcessHook<'TRes>
    }
    member x.CommandLine = x.InternalCommand.CommandLine
    member x.Command = x.InternalCommand
    member x.WorkingDirectory = x.InternalWorkingDirectory
    member x.Environment = x.InternalEnvironment

/// Some information regaring the started process
type StartedProcessInfo =
    internal {
        InternalProcess : Process
    }
    member x.Process = x.InternalProcess

/// Module for creating and modifying CreateProcess<'TRes> instances.
/// You can manage:
/// 
/// - The command (ie file to execute and arguments)
/// - The working directory
/// - The process environment
/// - Stream redirection and pipes
/// - Timeout for the process to exit
/// - The result and the result transformations (`map`, `mapResult`)
/// 
/// More extensions can be found in the [CreateProcess Extensions](apidocs/v5/fake-core-createprocessext-createprocess.html)
/// 
/// ### Example
/// 
///     Command.RawCommand("file", Arguments.OfArgs ["arg1"; "arg2"])
///     |> CreateProcess.fromCommand
///     |> Proc.run
///     |> ignore
/// 
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

    /// Create a simple `CreateProcess<_>` instance from the given command.
    /// 
    /// ### Example
    /// 
    ///     Command.RawCommand("file", Arguments.OfArgs ["arg1"; "arg2"])
    ///     |> CreateProcess.fromCommand
    ///     |> Proc.run
    ///     |> ignore
    let fromCommand command =
        {   InternalCommand = command
            InternalWorkingDirectory = None
            TraceCommand = true
            // Problem: Environment not allowed when using ShellCommand
            InternalEnvironment = None
            Streams =
                { // Problem: Redirection not allowed when using ShellCommand
                  StandardInput = Inherit
                  // Problem: Redirection not allowed when using ShellCommand
                  StandardOutput = Inherit
                  // Problem: Redirection not allowed when using ShellCommand
                  StandardError = Inherit }
            Hook = emptyHook }
    
    /// Create a CreateProcess from the given file and arguments
    /// 
    /// ### Example
    /// 
    ///     CreateProcess.fromRawCommandLine "cmd" "/C \"echo test\""
    ///     |> Proc.run
    ///     |> ignore
    /// 
    /// ### Using BlackFox.CommandLine
    /// 
    /// See [`BlackFox.CommandLine`](https://github.com/vbfox/FoxSharp/tree/master/src/BlackFox.CommandLine) for details
    /// 
    ///     open BlackFox.CommandLine
    /// 
    ///     CmdLine.empty
    ///     |> CmdLine.append "build"
    ///     |> CmdLine.appendIf noRestore "--no-restore"
    ///     |> CmdLine.appendPrefixIfSome "--framework" framework
    ///     |> CmdLine.appendPrefixf "--configuration" "%A" configuration
    ///     |> CmdLine.toString
    ///     |> CreateProcess.fromRawCommandLine "dotnet.exe"
    ///     |> Proc.run
    ///     |> ignore
    /// 
    let fromRawCommandLine command windowsCommandLine =
        fromCommand <| RawCommand(command, Arguments.OfWindowsCommandLine windowsCommandLine)


    /// Create a CreateProcess from the given file and arguments
    [<Obsolete("Use fromRawCommandLine instead.")>]
    let fromRawWindowsCommandLine command windowsCommandLine =
        fromCommand <| RawCommand(command, Arguments.OfWindowsCommandLine windowsCommandLine)

    /// Create a CreateProcess from the given file and arguments
    /// 
    /// ### Example
    /// 
    ///     CreateProcess.fromRawCommand "cmd" [ "/C";  "echo test" ]
    ///     |> Proc.run
    ///     |> ignore
    let fromRawCommand command args =
        fromCommand <| RawCommand(command, Arguments.OfArgs args)

    /// Create a CreateProcess from the given `ProcessStartInfo`
    let ofStartInfo (p:System.Diagnostics.ProcessStartInfo) =
        {   InternalCommand = if p.UseShellExecute then ShellCommand p.FileName else RawCommand(p.FileName, Arguments.OfStartInfo p.Arguments)
            TraceCommand = true
            InternalWorkingDirectory = if System.String.IsNullOrWhiteSpace p.WorkingDirectory then None else Some p.WorkingDirectory
            InternalEnvironment = 
                p.Environment
                |> Seq.map (fun kv -> kv.Key, kv.Value)
                |> EnvMap.ofSeq
                |> Some
            Streams =
                {   StandardInput = if p.RedirectStandardInput then CreatePipe StreamRef.Empty else Inherit
                    StandardOutput = if p.RedirectStandardOutput then CreatePipe StreamRef.Empty else Inherit
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
    
    /// intercept the given StreamSpecification and writes the intercepted data into target.
    /// Throws if the stream is not redirected (ie is Inherit).
    let interceptStream target (s:StreamSpecification) =
        interceptStreamFallback (fun _ -> failwithf "cannot intercept stream when it is not redirected. Please redirect the stream first!") target s
    
    /// Copies std-out and std-err into the corresponding `System.Console` streams (by using interceptStream).
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
    
    /// Set the working directory of the new process.
    let withWorkingDirectory workDir (c:CreateProcess<_>)=
        { c with
            InternalWorkingDirectory = Some workDir }

    /// Disable the default trace of started processes.
    let disableTraceCommand (c:CreateProcess<_>)=
        { c with
            TraceCommand = false }
    
    /// Set the command to the given one.
    let withCommand command (c:CreateProcess<_>)=
        { c with
            InternalCommand = command }

    /// Replace the file-path
    let replaceFilePath newFilePath (c:CreateProcess<_>)=
        { c with
            InternalCommand =
                match c.Command with
                | ShellCommand s -> failwith "Expected RawCommand"
                | RawCommand (_, c) -> RawCommand(newFilePath, c) }

    /// Map the file-path according to the given function.            
    let mapFilePath f (c:CreateProcess<_>)=
        c
        |> replaceFilePath (f (match c.Command with ShellCommand s -> failwith "Expected RawCommand" | RawCommand (file, _) -> f file))


    let internal withHook h (c:CreateProcess<_>) =
      { InternalCommand = c.Command
        TraceCommand = c.TraceCommand
        InternalWorkingDirectory = c.InternalWorkingDirectory
        InternalEnvironment = c.InternalEnvironment
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

    /// Attaches the given functions to the current CreateProcess instance.
    let appendSimpleFuncs prepareState onStart onResult onDispose (c:CreateProcess<_>) =
        c
        |> appendFuncsDispose
            prepareState
            (fun state streams -> streams)
            onStart
            onResult
            onDispose                     

    /// Execute the given function before the process is started 
    let addOnSetup f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs
            (fun _ -> f())
            (fun state p -> ())
            (fun prev state exitCode -> prev)
            ignore

    /// Execute the given function when the process is cleaned up.        
    let addOnFinally f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            ignore
            (fun state p -> ())
            (fun prev state exitCode -> prev)
            (fun _ -> f ())
    /// Execute the given function right after the process is started.
    let addOnStarted f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            ignore
            (fun state p -> f ())
            (fun prev state exitCode -> prev)
            ignore
    
    /// Execute the given function right after the process is started.
    /// PID for process can be obtained from p parameter (p.Process.Id).
    let addOnStartedEx (f:StartedProcessInfo -> _) (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            ignore
            (fun state p -> f { InternalProcess = p })
            (fun prev state exitCode -> prev)
            ignore

    /// Sets the given environment variables
    let withEnvironment (env: (string * string) list) (c:CreateProcess<_>)=
        { c with
            InternalEnvironment = Some (EnvMap.ofSeq env) }
            
    /// Sets the given environment map.        
    let withEnvironmentMap (env: EnvMap) (c:CreateProcess<_>)=
        { c with
            InternalEnvironment = Some env }

    /// Retrieve the current environment map.    
    let getEnvironmentMap (c:CreateProcess<_>)=
        match c.Environment with
        | Some en -> en
        | None -> EnvMap.create()

    /// Set the given environment variable.
    let setEnvironmentVariable envKey (envVar:string) (c:CreateProcess<_>) =
        { c with
            InternalEnvironment =
                getEnvironmentMap c
                |> IMap.add envKey envVar
                |> Some }

    /// Set the standard output stream.
    let withStandardOutput stdOut (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardOutput = stdOut } }
    /// Set the standard error stream.
    let withStandardError stdErr (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardError = stdErr } }
    /// Set the standard input stream.                    
    let withStandardInput stdIn (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardInput = stdIn } }

    /// Map the current result to a new type.
    let map f c =
        c
        |> appendSimpleFuncs 
            ignore
            (fun state p -> ())
            (fun prev state exitCode -> 
                async {
                    let! old = prev
                    return f old
                })
            ignore
    
    /// Map only the result object and leave the exit code in the result type.
    let mapResult f (c:CreateProcess<ProcessResult<_>>) =
        c
        |> map (fun r ->
            { ExitCode = r.ExitCode; Result = f r.Result })

    /// Starts redirecting the output streams and collects all data at the end.        
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
        
    /// Starts redirecting the output streams if they are not already redirected.
    /// Be careful when using this function. Using redirectOutput is the preferred variant
    let redirectOutputIfNotRedirected (c:CreateProcess<_>) =
        let refOut = StreamRef.Empty
        let refErr = StreamRef.Empty
        let pipeOut = CreatePipe refOut
        let pipeErr = CreatePipe refErr

        let startReadStream t (s:Stream) =
            async {
                try
                    let pool = System.Buffers.ArrayPool<byte>.Shared
                    let length = 1024 * 10
                    let rented = pool.Rent(length)
                    try
                        let mutable lastRead = 1
                        while lastRead > 0 do
                            let! read = s.ReadAsync(rented, 0, length)
                            lastRead <- read
                    finally
                        pool.Return rented
                with e ->
                    Trace.traceError <| sprintf "Error in startReadStream ('%s') of process '%s': %O" t c.CommandLine e
            }
            |> Async.StartAsTask

        { c with
            Streams =
                { c.Streams with
                    StandardOutput =
                        match c.Streams.StandardOutput with
                        | Inherit -> pipeOut
                        | _ -> c.Streams.StandardOutput
                    StandardError =
                        match c.Streams.StandardError with
                        | Inherit -> pipeErr
                        | _ -> c.Streams.StandardError
                }
        }
        |> appendFuncsDispose 
            (fun () -> ())
            (fun r streams -> streams)
            (fun () p -> ())
            (fun prev () exitCode ->
                async {
                    // Make sure to read the streams
                    let outTask =
                        match refOut.value with
                        | None -> System.Threading.Tasks.Task.CompletedTask
                        | Some s -> startReadStream "Standard Output" s :> _
                    let errTask =                    
                        match refErr.value with
                        | None -> System.Threading.Tasks.Task.CompletedTask
                        | Some s -> startReadStream "Standard Error" s :> _
                    let! prevResult = prev

                    // follow up mappings have the complete stream read (for example 'withOutputEvents')
                    do! Threading.Tasks.Task.WhenAll([errTask; outTask])

                    return prevResult
                })
            (fun () -> ())
        
    /// Calls the given functions whenever a new output-line is received.
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
    
    /// Like `withOutputEvents` but skips `null` objects.
    let withOutputEventsNotNull onStdOut onStdErr (c:CreateProcess<_>) =
        c
        |> withOutputEvents
            (fun m -> if isNull m |> not then onStdOut m)
            (fun m -> if isNull m |> not then onStdErr m)
    /// Execute the given function after the process has been exited and the previous result has been calculated.
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

    /// throws an exception with the given message if `exitCode <> 0`     
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

    /// Makes sure the exit code is `0`, otherwise a detailed exception is thrown (showing the command line).    
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
    /// Like`ensureExitCode` but only triggers a warning instead of failing.
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
    /// Set the given timeout, kills the process after the specified timespan
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
                        return raise <| TimeoutException(sprintf "Process '%s' timed out." c.CommandLine)
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
