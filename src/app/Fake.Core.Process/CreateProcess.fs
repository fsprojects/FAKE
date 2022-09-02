namespace Fake.Core

open System
open System.IO
open System.Diagnostics
open Fake.Core.ProcessHelpers

/// <summary>
/// Hook for events when an CreateProcess is executed.
/// </summary>
type internal IProcessHook<'TRes> =
    abstract member PrepareState : unit -> IDisposable
    abstract member PrepareStreams : IDisposable * StreamSpecs -> StreamSpecs
    abstract member ProcessStarted : IDisposable * Process -> unit
    abstract member RetrieveResult : IDisposable * System.Threading.Tasks.Task<RawProcessResult> -> Async<'TRes>

type internal IProcessHookImpl<'TState, 'TRes when 'TState :> IDisposable> =
    abstract member PrepareState : unit -> 'TState
    abstract member PrepareStreams : 'TState * StreamSpecs -> StreamSpecs
    abstract member ProcessStarted : 'TState * Process -> unit
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


/// <summary>
/// The output of the process. If ordering between stdout and stderr is important you need to use streams.
/// </summary>
type ProcessOutput = { Output : string; Error : string }

type ProcessResult<'a> = { Result : 'a; ExitCode : int }

/// <summary>
/// Handle for creating a process and returning potential results.
/// </summary>
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

/// <summary>
/// Some information regarding the started process
/// </summary>
type StartedProcessInfo =
    internal {
        InternalProcess : Process
    }
    member x.Process = x.InternalProcess

/// <summary>
/// Module for creating and modifying CreateProcess&lt;&apos;TRes&gt; instances.
/// </summary>
/// <remarks>
/// You can manage: <br/>
/// <list type="number">
/// <item>
/// The command (ie file to execute and arguments)
/// </item>
/// <item>
/// The working directory
/// </item>
/// <item>
/// The process environment
/// </item>
/// <item>
/// Stream redirection and pipes
/// </item>
/// <item>
/// Timeout for the process to exit
/// </item>
/// <item>
/// The result and the result transformations (<c>map</c>, <c>mapResult</c>)
/// </item>
/// </list>
/// <br/>
/// More extensions can be found in the <a href="reference/fake-core-createprocessext-createprocess.html">
/// CreateProcess Extensions</a>
/// </remarks>
/// 
/// <example>
/// <code lang="fsharp">
/// Command.RawCommand("file", Arguments.OfArgs ["arg1"; "arg2"])
///     |&gt; CreateProcess.fromCommand
///     |&gt; Proc.run
///     |&gt; ignore
/// </code>
/// </example>
module CreateProcess =
    let internal emptyHook =
        { new IProcessHook<ProcessResult<unit>> with
            member _.PrepareState () = null
            member _.PrepareStreams (_, specs) = specs
            member _.ProcessStarted (_,_) = ()
            member _.RetrieveResult (_, t) =
                async {
                    let! raw = Async.AwaitTaskWithoutAggregate t
                    return { ExitCode = raw.RawExitCode; Result = () } 
                } }
        
    /// <summary>
    /// Create a simple <c>CreateProcess&lt;_&gt;</c> instance from the given command.
    /// </summary>
    /// 
    /// <example>
    /// <code lang="fsharp">
    /// Command.RawCommand("file", Arguments.OfArgs ["arg1"; "arg2"])
    ///     |&gt; CreateProcess.fromCommand
    ///     |&gt; Proc.run
    ///     |&gt; ignore
    /// </code>
    /// </example>
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
    
    /// <summary>
    /// Create a CreateProcess from the given file and arguments
    /// </summary>
    /// 
    /// <example>
    /// <code lang="fsharp">
    /// CreateProcess.fromRawCommandLine "cmd" "/C \"echo test\""
    ///     |&gt; Proc.run
    ///     |&gt; ignore
    /// </code>
    /// </example>
    /// 
    /// <remarks>
    /// Using BlackFox.CommandLine <br/>
    /// See <a href="https://github.com/vbfox/FoxSharp/tree/master/src/BlackFox.CommandLine">
    /// <c>BlackFox.CommandLine</c></a> for details
    /// <example>
    /// <code lang="fsharp">
    /// open BlackFox.CommandLine
    /// 
    ///     CmdLine.empty
    ///     |&gt; CmdLine.append "build"
    ///     |&gt; CmdLine.appendIf noRestore "--no-restore"
    ///     |&gt; CmdLine.appendPrefixIfSome "--framework" framework
    ///     |&gt; CmdLine.appendPrefixf "--configuration" "%A" configuration
    ///     |&gt; CmdLine.toString
    ///     |&gt; CreateProcess.fromRawCommandLine "dotnet.exe"
    ///     |&gt; Proc.run
    ///     |&gt; ignore
    /// </code>
    /// </example>
    /// </remarks>
    /// 
    let fromRawCommandLine command windowsCommandLine =
        fromCommand <| RawCommand(command, Arguments.OfWindowsCommandLine windowsCommandLine)

    /// <summary>
    /// Create a CreateProcess from the given file and arguments
    /// </summary>
    /// 
    /// <example>
    /// <code lang="fsharp">
    /// CreateProcess.fromRawCommand "cmd" [ "/C";  "echo test" ]
    ///     |&gt; Proc.run
    ///     |&gt; ignore
    /// </code>
    /// </example>
    let fromRawCommand command args =
        fromCommand <| RawCommand(command, Arguments.OfArgs args)

    /// <summary>
    /// Create a CreateProcess from the given <c>ProcessStartInfo</c>
    /// </summary>
    let ofStartInfo (p:ProcessStartInfo) =
        {   InternalCommand = if p.UseShellExecute then ShellCommand p.FileName else RawCommand(p.FileName, Arguments.OfStartInfo p.Arguments)
            TraceCommand = true
            InternalWorkingDirectory = if String.IsNullOrWhiteSpace p.WorkingDirectory then None else Some p.WorkingDirectory
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
    
    /// <summary>
    /// Intercept the given StreamSpecification and writes the intercepted data into target.
    /// Throws if the stream is not redirected (ie is Inherit).
    /// </summary>
    ///
    /// <param name="target">The target stream</param>
    /// <param name="s">The target stream specification</param>
    let interceptStream target (s:StreamSpecification) =
        interceptStreamFallback (fun _ -> failwithf "cannot intercept stream when it is not redirected. Please redirect the stream first!") target s
    
    /// <summary>
    /// Copies std-out and std-err into the corresponding <c>System.Console</c> streams (by using interceptStream).
    /// </summary>
    ///
    /// <param name="c">The process to copy output to standard output</param>
    let copyRedirectedProcessOutputsToStandardOutputs (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardOutput =
                        let stdOut = Console.OpenStandardOutput()
                        interceptStream stdOut c.Streams.StandardOutput
                    StandardError =
                        let stdErr = Console.OpenStandardError()
                        interceptStream stdErr c.Streams.StandardError } }
    
    /// <summary>
    /// Set the working directory of the new process.
    /// </summary>
    ///
    /// <param name="workDir">The working directory</param>
    /// <param name="c">The create process instance</param>
    let withWorkingDirectory workDir (c:CreateProcess<_>)=
        { c with
            InternalWorkingDirectory = Some workDir }

    /// <summary>
    /// Disable the default trace of started processes.
    /// </summary>
    ///
    /// <param name="c">The create process instance</param>
    let disableTraceCommand (c:CreateProcess<_>)=
        { c with
            TraceCommand = false }
    
    /// <summary>
    /// Set the command to the given one.
    /// </summary>
    ///
    /// <param name="command">The command to add to create process instance</param>
    /// <param name="c">The create process instance</param>
    let withCommand command (c:CreateProcess<_>)=
        { c with
            InternalCommand = command }

    /// <summary>
    /// Replace the file-path
    /// </summary>
    ///
    /// <param name="newFilePath">The new file path to use as a replacement</param>
    /// <param name="c">The create process instance</param>
    let replaceFilePath newFilePath (c:CreateProcess<_>)=
        { c with
            InternalCommand =
                match c.Command with
                | ShellCommand _s -> failwith "Expected RawCommand"
                | RawCommand (_, c) -> RawCommand(newFilePath, c) }

    /// <summary>
    /// Map the file-path according to the given function.
    /// </summary>
    ///
    /// <param name="f">Function to override file path</param>
    /// <param name="c">The create process instance</param>
    let mapFilePath f (c:CreateProcess<_>)=
        c
        |> replaceFilePath (f (match c.Command with ShellCommand _s -> failwith "Expected RawCommand" | RawCommand (file, _) -> f file))


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
            member _.PrepareState () =
                prepareState ()
            member _.PrepareStreams (state, streams) =
                prepareStreams state streams
            member _.ProcessStarted (state, p) = 
                onStart state p
            member _.RetrieveResult (state, exitCode) = 
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
            member _.PrepareState () =
                let state1 = c.PrepareState ()
                let state2 = prepareState ()
                { State1 = state1; State2 = state2 }
            member _.PrepareStreams (state, streams) =
                let newStreams = c.PrepareStreams(state.State1, streams)
                let finalStreams = prepareStreams state.State2 newStreams
                finalStreams
            member _.ProcessStarted (state, p) =
                c.ProcessStarted (state.State1, p)
                onStart state.State2 p
            member _.RetrieveResult (state, exitCode) = 
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

    /// <summary>
    /// Attaches the given functions to the current CreateProcess instance.
    /// </summary>
    ///
    /// <param name="prepareState">Function to override state</param>
    /// <param name="onStart">Function to override start</param>
    /// <param name="onResult">Function to override result</param>
    /// <param name="onDispose">Function to override dispose</param>
    /// <param name="c">The create process instance</param>
    let appendSimpleFuncs prepareState onStart onResult onDispose (c:CreateProcess<_>) =
        c
        |> appendFuncsDispose
            prepareState
            (fun _state streams -> streams)
            onStart
            onResult
            onDispose                     

    /// <summary>
    /// Execute the given function before the process is started
    /// </summary>
    ///
    /// <param name="f">Function to add on setup</param>
    /// <param name="c">The create process instance</param>
    let addOnSetup f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs
            (fun _ -> f())
            (fun _state _p -> ())
            (fun prev _state _exitCode -> prev)
            ignore

    /// <summary>
    /// Execute the given function when the process is cleaned up.
    /// </summary>
    ///
    /// <param name="f">Function to add as a finally clause</param>
    /// <param name="c">The create process instance</param>
    let addOnFinally f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            ignore
            (fun _state _p -> ())
            (fun prev _state _exitCode -> prev)
            (fun _ -> f ())
    
    /// <summary>
    /// Execute the given function right after the process is started.
    /// </summary> 
    ///
    /// <param name="f">Function to add on started event</param>
    /// <param name="c">The create process instance</param>
    let addOnStarted f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            ignore
            (fun _state _p -> f ())
            (fun prev _state _exitCode -> prev)
            ignore
    
    /// <summary>
    /// Execute the given function right after the process is started.
    /// PID for process can be obtained from p parameter (p.Process.Id).
    /// </summary>
    ///
    /// <param name="f">Function to add on started event</param>
    /// <param name="c">The create process instance</param>
    let addOnStartedEx (f:StartedProcessInfo -> _) (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            ignore
            (fun _state p -> f { InternalProcess = p })
            (fun prev _state _exitCode -> prev)
            ignore

    /// <summary>
    /// Sets the given environment variables
    /// </summary>
    ///
    /// <param name="env">The environment variables list to add</param>
    /// <param name="c">The create process instance</param>
    let withEnvironment (env: (string * string) list) (c:CreateProcess<_>)=
        { c with
            InternalEnvironment = Some (EnvMap.ofSeq env) }
            
    /// <summary>
    /// Sets the given environment map.
    /// </summary>
    ///
    /// <param name="env">The environment variables map to add</param>
    /// <param name="c">The create process instance</param>
    let withEnvironmentMap (env: EnvMap) (c:CreateProcess<_>)=
        { c with
            InternalEnvironment = Some env }

    /// <summary>
    /// Retrieve the current environment map.
    /// </summary>
    ///
    /// <param name="c">The create process instance</param>
    let getEnvironmentMap (c:CreateProcess<_>)=
        match c.Environment with
        | Some en -> en
        | None -> EnvMap.create()

    /// <summary>
    /// Set the given environment variable.
    /// </summary>
    ///
    /// <param name="envKey">The environment variable key</param>
    /// <param name="envVar">The environment variable value</param>
    /// <param name="c">The create process instance</param>
    let setEnvironmentVariable envKey (envVar:string) (c:CreateProcess<_>) =
        { c with
            InternalEnvironment =
                getEnvironmentMap c
                |> IMap.add envKey envVar
                |> Some }

    /// <summary>
    /// Set the standard output stream.
    /// </summary>
    ///
    /// <param name="stdOut">The standard output to use</param>
    /// <param name="c">The create process instance</param>
    let withStandardOutput stdOut (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardOutput = stdOut } }
    
    /// <summary>
    /// Set the standard error stream.
    /// </summary>
    ///
    /// <param name="stdOut">The standard error to use</param>
    /// <param name="c">The create process instance</param>
    let withStandardError stdErr (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardError = stdErr } }
    
    /// <summary>
    /// Set the standard input stream.
    /// </summary>
    ///
    /// <param name="stdIn">The standard input to use</param>
    /// <param name="c">The create process instance</param> 
    let withStandardInput stdIn (c:CreateProcess<_>)=
        { c with
            Streams =
                { c.Streams with
                    StandardInput = stdIn } }

    /// <summary>
    /// Map the current result to a new type.
    /// </summary>
    let map f c =
        c
        |> appendSimpleFuncs 
            ignore
            (fun _state _p -> ())
            (fun prev _state _exitCode -> 
                async {
                    let! old = prev
                    return f old
                })
            ignore
    
    /// <summary>
    /// Map only the result object and leave the exit code in the result type.
    /// </summary>
    ///
    /// <param name="f">Function to run result map through</param>
    let mapResult f (c:CreateProcess<ProcessResult<_>>) =
        c
        |> map (fun r ->
            { ExitCode = r.ExitCode; Result = f r.Result })

    /// <summary>
    /// Starts redirecting the output streams and collects all data at the end.
    /// </summary>
    ///
    /// <param name="c">The create process instance</param> 
    let redirectOutput (c:CreateProcess<_>) =
        c
        |> appendFuncsDispose 
            (fun _streams ->
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
            (fun (_outMem, _errMem) _p -> ())
            (fun prev (outMem, errMem) exitCode ->
                async {
                    let! _prevResult = prev
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
        
    /// <summary>
    /// Starts redirecting the output streams if they are not already redirected.
    /// Be careful when using this function. Using redirectOutput is the preferred variant
    /// </summary>
    ///
    /// <param name="c">The create process instance</param> 
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
            (fun _r streams -> streams)
            (fun () _p -> ())
            (fun prev () _exitCode ->
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
        
    /// <summary>
    /// Calls the given functions whenever a new output-line is received.
    /// </summary>
    ///
    /// <param name="onStdOut">Function to add as a standard output handler</param> 
    /// <param name="onStdErr">Function to add as a standard error handler</param> 
    /// <param name="c">The create process instance</param> 
    let withOutputEvents onStdOut onStdErr (c:CreateProcess<_>) =
        let watchStream onF (stream:Stream) =
            async {
                let reader = new StreamReader(stream)
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
            (fun (closeOut, outMem, closeErr, errMem, _tOut, _tErr) streams ->
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
            (fun _state _p -> ())
            (fun prev (closeOut, _outMem, closeErr, _errMem, tOut, tErr) _exitCode ->
                async {
                    let! prevResult = prev
                    do! closeOut()
                    do! closeErr()
                    do! tOut
                    do! tErr
                    return prevResult
                })
            (fun (_closeOut, outMem, _closeErr, errMem, _tOut, _tErr) ->
                
                outMem.Dispose()
                errMem.Dispose())
    
    /// <summary>
    /// Like <c>withOutputEvents</c> but skips <c>null</c> objects.
    /// </summary>
    ///
    /// <param name="onStdOut">Function to add as a standard output handler</param> 
    /// <param name="onStdErr">Function to add as a standard error handler</param> 
    /// <param name="c">The create process instance</param> 
    let withOutputEventsNotNull onStdOut onStdErr (c:CreateProcess<_>) =
        c
        |> withOutputEvents
            (fun m -> if isNull m |> not then onStdOut m)
            (fun m -> if isNull m |> not then onStdErr m)
            
    /// <summary>
    /// Execute the given function after the process has been exited and the previous result has been calculated.
    /// </summary>
    ///
    /// <param name="f">Function to add on exit event</param> 
    /// <param name="c">The create process instance</param> 
    let addOnExited f (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            (fun _ -> ())
            (fun _state _p -> ())
            (fun prev _state exitCode -> 
                async {
                    let! prevResult = prev
                    let! e = exitCode
                    let s = f prevResult e.RawExitCode
                    return s
                })
            (fun _ -> ())

    /// <summary>
    /// throws an exception with the given message if <c>exitCode &lt;&gt; 0</c>
    /// </summary>
    ///
    /// <param name="msg">The message to use</param> 
    /// <param name="c">The create process instance</param> 
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

    /// <summary>
    /// Makes sure the exit code is <c>0</c>, otherwise a detailed exception is thrown (showing the command line).
    /// </summary>
    ///
    /// <param name="c">The create process instance</param> 
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
    /// <summary>
    /// Like<c>ensureExitCode</c> but only triggers a warning instead of failing.
    /// </summary>
    ///
    /// <param name="c">The create process instance</param> 
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
            data)

    type internal TimeoutState =
        { Stopwatch : Stopwatch
          mutable HasExited : bool }
        
    /// <summary>
    /// Set the given timeout, kills the process after the specified timespan
    /// </summary>
    ///
    /// <param name="timeout">The timeout amount</param> 
    /// <param name="c">The create process instance</param> 
    let withTimeout (timeout:TimeSpan) (c:CreateProcess<_>) =
        c
        |> appendSimpleFuncs 
            (fun _ -> 
                { Stopwatch = Stopwatch.StartNew()
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
            (fun _state -> ())
