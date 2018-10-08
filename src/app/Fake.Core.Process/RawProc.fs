namespace Fake.Core

open System.Reflection
open Fake.Core.ProcessHelpers

/// The type of command to execute
type Command =
    | ShellCommand of string
    /// Windows: https://msdn.microsoft.com/en-us/library/windows/desktop/bb776391(v=vs.85).aspx
    /// Linux(mono): https://github.com/mono/mono/blob/0bcbe39b148bb498742fc68416f8293ccd350fb6/eglib/src/gshell.c#L32-L104 (because we need to create a commandline string internally which need to go through that code)
    /// Linux(netcore): See https://github.com/fsharp/FAKE/pull/1281/commits/285e585ec459ac7b89ca4897d1323c5a5b7e4558 and https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L443-L522
    | RawCommand of executable:FilePath * arguments:Arguments

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

/// The output of the process. If ordering between stdout and stderr is important you need to use streams.
type ProcessOutput = { Output : string; Error : string }

/// A raw (untyped) way to start a process
type RawCreateProcess =
    internal {
        Command : Command
        WorkingDirectory : string option
        Environment : (string * string) list option
        StandardInput : StreamSpecification 
        StandardOutput : StreamSpecification 
        StandardError : StreamSpecification
        GetRawOutput : (unit -> ProcessOutput) option
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
                
        let setEnv key var =
            p.Environment.[key] <- var
        x.Environment
            |> Option.iter (fun env ->
                p.Environment.Clear()
                env |> Seq.iter (fun (key, value) -> setEnv key value))
#if FX_WINDOWSTLE    
        p.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden
#endif
        p

    member x.OutputRedirected = x.GetRawOutput.IsSome
    member x.CommandLine =
        match x.Command with
        | ShellCommand s -> s
        | RawCommand (f, arg) -> sprintf "%s %s" f arg.ToWindowsCommandLine

type IProcessStarter =
    abstract Start : RawCreateProcess -> Async<int * ProcessOutput option>

module RawProc =
    // mono sets echo off for some reason, therefore interactive mode doesn't work as expected
    // this enables this tty feature which makes the interactive mode work as expected
    let private setEcho (b:bool) =
        // See https://github.com/mono/mono/blob/master/mcs/class/corlib/System/ConsoleDriver.cs#L289
        let t = System.Type.GetType("System.ConsoleDriver").GetTypeInfo()
        if Environment.isMono then
            let flags = System.Reflection.BindingFlags.Static ||| System.Reflection.BindingFlags.NonPublic
            if isNull t then
                Trace.traceFAKE "Expected to find System.ConsoleDriver.SetEcho"
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
    let mutable processStarter = 
        { new IProcessStarter with
            member __.Start c = async {
                let p = c.ToStartInfo
                let commandLine = 
                    sprintf "%s> \"%s\" %s" p.WorkingDirectory p.FileName p.Arguments

                Trace.tracefn "%s... RedirectInput: %b, RedirectOutput: %b, RedirectError: %b" commandLine p.RedirectStandardInput p.RedirectStandardOutput p.RedirectStandardError
                
                use toolProcess = new Process(StartInfo = p)
                
                let isStarted = ref false
                let mutable readOutputTask = System.Threading.Tasks.Task.FromResult Stream.Null
                let mutable readErrorTask = System.Threading.Tasks.Task.FromResult Stream.Null
                let mutable redirectStdInTask = System.Threading.Tasks.Task.FromResult Stream.Null
                let tok = new System.Threading.CancellationTokenSource()
                let start() =
                    if not <| !isStarted then
                        toolProcess.EnableRaisingEvents <- true
                        setEcho true |> ignore
                        Process.rawStartProcess toolProcess
                        isStarted := true
                        
                        let handleStream parameter processStream isInputStream =
                            async {
                                match parameter with
                                | Inherit ->
                                    return failwithf "Unexpected value"
                                | UseStream (shouldClose, stream) ->
                                    if isInputStream then
                                        do! stream.CopyToAsync(processStream, 81920, tok.Token)
                                            |> Async.AwaitTask
                                    else
                                        do! processStream.CopyToAsync(stream, 81920, tok.Token)
                                            |> Async.AwaitTask
                                    return
                                        if shouldClose then stream else Stream.Null
                                | CreatePipe (r) ->
                                    r.retrieveRaw := fun _ -> processStream
                                    return Stream.Null
                            }
        
                        if p.RedirectStandardInput then
                            redirectStdInTask <-
                              handleStream c.StandardInput toolProcess.StandardInput.BaseStream true
                              // Immediate makes sure we set the ref cell before we return...
                              |> fun a -> Async.StartImmediateAsTask(a, cancellationToken = tok.Token)
                              
                        if p.RedirectStandardOutput then
                            readOutputTask <-
                              handleStream c.StandardOutput toolProcess.StandardOutput.BaseStream false
                              // Immediate makes sure we set the ref cell before we return...
                              |> fun a -> Async.StartImmediateAsTask(a, cancellationToken = tok.Token)
        
                        if p.RedirectStandardError then
                            readErrorTask <-
                              handleStream c.StandardError toolProcess.StandardError.BaseStream false
                              // Immediate makes sure we set the ref cell before we return...
                              |> fun a -> Async.StartImmediateAsTask(a, cancellationToken = tok.Token)
            
                // Wait for the process to finish
                let! exitEvent = 
                    toolProcess.Exited
                        // This way the handler gets added before actually calling start or "EnableRaisingEvents"
                        |> Event.guard start
                        |> Async.AwaitEvent
                        |> Async.StartImmediateAsTask
                // Waiting for the process to exit (buffers)
                toolProcess.WaitForExit()
        
                let delay = System.Threading.Tasks.Task.Delay 500
                let all =  System.Threading.Tasks.Task.WhenAll([readErrorTask; readOutputTask; redirectStdInTask])
                let! t = System.Threading.Tasks.Task.WhenAny(all, delay)
                         |> Async.AwaitTask
                if t = delay then
                    Trace.traceFAKE "At least one redirection task did not finish: \nReadErrorTask: %O, ReadOutputTask: %O, RedirectStdInTask: %O" readErrorTask.Status readOutputTask.Status redirectStdInTask.Status
                tok.Cancel()
                
                // wait for finish -> AwaitTask has a bug which makes it unusable for chanceled tasks.
                // workaround with continuewith
                let! streams = all.ContinueWith (new System.Func<System.Threading.Tasks.Task<Stream[]>, Stream[]> (fun t -> t.GetAwaiter().GetResult())) |> Async.AwaitTask
                for s in streams do s.Dispose()
                setEcho false |> ignore
                
                let output =
                    match c.GetRawOutput with
                    | Some f -> Some (f())
                    | None -> None
                
                return toolProcess.ExitCode, output }
        }