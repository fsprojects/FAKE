/// Contains functions which can be used to start other tools.
module Fake.Core.Process

open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Threading
open System.Text
open System.Collections.Generic
open Fake.IO.FileSystem
open Fake.IO.FileSystem.Operators
open Fake.Core.GuardedAwaitObservable

/// Kills the given process
let kill (proc : Process) = 
    Trace.tracefn "Trying to kill process %s (Id = %d)" proc.ProcessName proc.Id
    try 
        proc.Kill()
    with exn -> ()

let private killCreatedProcessesVar = "Fake.Core.Process.killCreatedProcesses"
let private getKillCreatedProcesses, _, public setKillCreatedProcesses = 
    Fake.Core.Context.fakeVarAllowNoContext killCreatedProcessesVar
let shouldKillCreatedProcesses () =
    match getKillCreatedProcesses() with
    | Some v -> v
    | None ->
      let shouldEnable = Fake.Core.Context.isFakeContext()
      setKillCreatedProcesses shouldEnable
      shouldEnable

type ProcessList() =
    let startedProcesses = HashSet()
    let killProcesses () = 
        let traced = ref false
        
        for pid, startTime in startedProcesses do
            try
                let proc = Process.GetProcessById pid
                
                // process IDs may be reused by the operating system so we need
                // to make sure the process is indeed the one we started
                if proc.StartTime = startTime && not proc.HasExited then
                    try 
                        if not !traced then
                          Trace.tracefn "Killing all processes that are created by FAKE and are still running."
                          traced := true

                        Trace.logfn "Trying to kill %s" proc.ProcessName
                        kill proc
                    with exn -> Trace.logfn "Killing %s failed with %s" proc.ProcessName exn.Message                              
            with exn -> ()
        startedProcesses.Clear()
    member x.KillAll() = killProcesses()
    member x.Add (pid, startTime) = startedProcesses.Add(pid, startTime)
    
    interface IDisposable with
        member x.Dispose() =
            if shouldKillCreatedProcesses() then killProcesses()

/// [omit]
//let startedProcesses = HashSet()
let private startedProcessesVar = "Fake.Core.Process.startedProcesses"
let private getStartedProcesses, _, private setStartedProcesses = 
    Fake.Core.Context.fakeVar startedProcessesVar
let private addStartedProcess (id:int, startTime:System.DateTime) =
    match getStartedProcesses () with
    | Some (h:ProcessList) -> h.Add(id, startTime)
    | None -> 
        let h = new ProcessList()
        setStartedProcesses (h)
        h.Add(id, startTime)

let private monoArgumentsVar = "Fake.Core.Process.monoArguments"
let private tryGetMonoArguments, _, public setMonoArguments = 
    Fake.Core.Context.fakeVar monoArgumentsVar
let getMonoArguments () =
    match tryGetMonoArguments () with
    | Some (args) -> args
    | None -> ""

/// Modifies the ProcessStartInfo according to the platform semantics
let platformInfoAction (psi : ProcessStartInfo) = 
    if Environment.isMono && psi.FileName.EndsWith ".exe" then 
        psi.Arguments <- getMonoArguments() + " \"" + psi.FileName + "\" " + psi.Arguments
        psi.FileName <- Environment.monoPath

/// [omit]
let start (proc : Process) = 
    platformInfoAction proc.StartInfo
    proc.Start() |> ignore
    addStartedProcess(proc.Id, proc.StartTime) |> ignore

/// [omit]
//let mutable redirectOutputToTrace = false
let private redirectOutputToTraceVar = "Fake.Core.Process.redirectOutputToTrace"
let private tryGetRedirectOutputToTrace, _, public setRedirectOutputToTrace = 
    Fake.Core.Context.fakeVarAllowNoContext redirectOutputToTraceVar
let getRedirectOutputToTrace () =
    match tryGetRedirectOutputToTrace() with
    | Some v -> v
    | None ->
      let shouldEnable = false
      setRedirectOutputToTrace shouldEnable
      shouldEnable

/// [omit]
//let mutable enableProcessTracing = true
let private enableProcessTracingVar = "Fake.Core.Process.enableProcessTracing"
let private getEnableProcessTracing, private removeEnableProcessTracing, public setEnableProcessTracing = 
    Fake.Core.Context.fakeVarAllowNoContext enableProcessTracingVar
let shouldEnableProcessTracing () =
    match getEnableProcessTracing() with
    | Some v -> v
    | None ->
      let shouldEnable = Fake.Core.Context.isFakeContext()
      setEnableProcessTracing shouldEnable
      shouldEnable

/// A record type which captures console messages
type ConsoleMessage = 
    { IsError : bool
      Message : string
      Timestamp : DateTimeOffset }

/// A process result including error code, message log and errors.
type ProcessResult = 
    { ExitCode : int
      Messages : List<string>
      Errors : List<string> }
    member x.OK = x.ExitCode = 0
    static member New exitCode messages errors = 
        { ExitCode = exitCode
          Messages = messages
          Errors = errors }


/// Runs the given process and returns the exit code.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
///  - `silent` - If this flag is set then the process output is redirected to the given output functions `errorF` and `messageF`.
///  - `errorF` - A function which will be called with the error log.
///  - `messageF` - A function which will be called with the message log.
let ExecProcessWithLambdas configProcessStartInfoF (timeOut : TimeSpan) silent errorF messageF = 
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
    platformInfoAction proc.StartInfo
    if String.isNullOrEmpty proc.StartInfo.WorkingDirectory |> not then 
        if Directory.Exists proc.StartInfo.WorkingDirectory |> not then 
            failwithf "Start of process %s failed. WorkingDir %s does not exist." proc.StartInfo.FileName 
                proc.StartInfo.WorkingDirectory
    if silent then 
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.RedirectStandardError <- true
        if Environment.isMono then
            proc.StartInfo.StandardOutputEncoding <- Encoding.UTF8
            proc.StartInfo.StandardErrorEncoding  <- Encoding.UTF8
        proc.ErrorDataReceived.Add(fun d -> 
            if isNull d.Data |> not then errorF d.Data)
        proc.OutputDataReceived.Add(fun d -> 
            if isNull d.Data |> not then messageF d.Data)
    try 
        if shouldEnableProcessTracing() && (not <| proc.StartInfo.FileName.EndsWith "fsi.exe") then 
            Trace.tracefn "%s %s" proc.StartInfo.FileName proc.StartInfo.Arguments
        start proc
    with exn -> failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message
    if silent then 
        proc.BeginErrorReadLine()
        proc.BeginOutputReadLine()
    if timeOut = TimeSpan.MaxValue then proc.WaitForExit()
    else 
        if not <| proc.WaitForExit(int timeOut.TotalMilliseconds) then 
            try 
                proc.Kill()
            with exn -> 
                Trace.traceError 
                <| sprintf "Could not kill process %s  %s after timeout." proc.StartInfo.FileName 
                       proc.StartInfo.Arguments
            failwithf "Process %s %s timed out." proc.StartInfo.FileName proc.StartInfo.Arguments
    // See http://stackoverflow.com/a/16095658/1149924 why WaitForExit must be called twice.
    proc.WaitForExit()
    proc.ExitCode

/// Runs the given process and returns the process result.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
let ExecProcessAndReturnMessages configProcessStartInfoF timeOut = 
    let errors = new List<_>()
    let messages = new List<_>()
    let exitCode = ExecProcessWithLambdas configProcessStartInfoF timeOut true (errors.Add) (messages.Add)
    ProcessResult.New exitCode messages errors

/// Runs the given process and returns the process result.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
let ExecProcessRedirected configProcessStartInfoF timeOut = 
    let messages = ref []
    
    let appendMessage isError msg = 
        messages := { IsError = isError
                      Message = msg
                      Timestamp = DateTimeOffset.UtcNow } :: !messages
    
    let exitCode = 
        ExecProcessWithLambdas configProcessStartInfoF timeOut true (appendMessage true) (appendMessage false)
    exitCode = 0, 
    (!messages
     |> List.rev
     |> Seq.ofList)

/// Runs the given process and returns the exit code.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
/// ## Sample
///
///     let result = ExecProcess (fun info ->  
///                       info.FileName <- "c:/MyProc.exe"
///                       info.WorkingDirectory <- "c:/workingDirectory"
///                       info.Arguments <- "-v") (TimeSpan.FromMinutes 5.0)
///     
///     if result <> 0 then failwithf "MyProc.exe returned with a non-zero exit code"
let ExecProcess configProcessStartInfoF timeOut = 
    ExecProcessWithLambdas configProcessStartInfoF timeOut (getRedirectOutputToTrace()) Trace.traceError Trace.trace

/// Runs the given process in an elevated context and returns the exit code.
/// ## Parameters
///
///  - `cmd` - The command which should be run in elavated context.
///  - `args` - The process arguments.
///  - `timeOut` - The timeout for the process.
[<Obsolete("This is currently no possible in dotnetcore")>]
let ExecProcessElevated cmd args timeOut = 
    ExecProcess (fun si ->
#if !NETSTANDARD
        si.Verb <- "runas"
#endif
        si.Arguments <- args
        si.FileName <- cmd
        si.UseShellExecute <- true) timeOut

/// Sets the environment Settings for the given startInfo.
/// Existing values will be overriden.
/// [omit]
let setEnvironmentVariables (startInfo : ProcessStartInfo) environmentSettings =
#if NETSTANDARD
    let envDict = startInfo.Environment
#else
    let envDict = startInfo.EnvironmentVariables
#endif
    for key, value in environmentSettings do
        if envDict.ContainsKey key then envDict.[key] <- value
        else envDict.Add(key, value)

/// Runs the given process and returns true if the exit code was 0.
/// [omit]
let execProcess configProcessStartInfoF timeOut = ExecProcess configProcessStartInfoF timeOut = 0

/// Starts the given process and returns immediatly.
let fireAndForget configProcessStartInfoF = 
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
    try 
        start proc
    with exn -> failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message

/// Runs the given process, waits for its completion and returns if it succeeded.
let directExec configProcessStartInfoF = 
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
    try 
        start proc
    with exn -> failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message
    proc.WaitForExit()
    proc.ExitCode = 0

/// Starts the given process and forgets about it.
let StartProcess configProcessStartInfoF = 
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
    start proc

/// Adds quotes around the string
/// [omit]
let quote (str:string) = "\"" + str.Replace("\"","\\\"") + "\""

/// Adds quotes around the string if needed
/// [omit]
let quoteIfNeeded str = 
    if String.isNullOrEmpty str then ""
    elif str.Contains " " then quote str
    else str

/// Adds quotes and a blank around the string´.
/// [omit]
let toParam x = " " + quoteIfNeeded x

/// Use default Parameters
/// [omit]
let UseDefaults = id

/// [omit]
let stringParam (paramName, paramValue) = 
    if String.isNullOrEmpty paramValue then None
    else Some(paramName, quote paramValue)

/// [omit]
let multipleStringParams paramName = Seq.map (fun x -> stringParam (paramName, x)) >> Seq.toList

/// [omit]
let optionParam (paramName, paramValue) = 
    match paramValue with
    | Some x -> Some(paramName, x.ToString())
    | None -> None

/// [omit]
let boolParam (paramName, paramValue) = 
    if paramValue then Some(paramName, null)
    else None

/// [omit]
let parametersToString flagPrefix delimiter parameters = 
    parameters
    |> Seq.choose id
    |> Seq.map (fun (paramName, paramValue) -> 
           flagPrefix + paramName + if String.isNullOrEmpty paramValue then ""
                                    else delimiter + paramValue)
    |> String.separated " "

/// Searches the given directories for all occurrences of the given file name
/// [omit]
let tryFindFile dirs file = 
    let files = 
        dirs
        |> Seq.map (fun (path : string) -> 
               let dir = 
                   path
                   |> String.replace "[ProgramFiles]" Environment.ProgramFiles
                   |> String.replace "[ProgramFilesX86]" Environment.ProgramFilesX86
                   |> String.replace "[SystemRoot]" Environment.SystemRoot
                   |> DirectoryInfo.ofPath
               if not dir.Exists then ""
               else 
                   let fi = dir.FullName @@ file
                            |> FileInfo.ofPath
                   if fi.Exists then fi.FullName
                   else "")
        |> Seq.filter ((<>) "")
        |> Seq.cache
    if not (Seq.isEmpty files) then Some(Seq.head files)
    else None

/// Searches the given directories for the given file, failing if not found.
/// [omit]
let findFile dirs file = 
    match tryFindFile dirs file with
    | Some found -> found
    | None -> failwithf "%s not found in %A." file dirs

/// Searches the current directory and the directories within the PATH
/// environment variable for the given file. If successful returns the full
/// path to the file.
/// ## Parameters
///  - `file` - The file to locate
let tryFindFileOnPath (file : string) : string option =
    Environment.pathDirectories
    |> Seq.filter Path.isValidPath
    |> Seq.append [ "." ]
    |> fun path -> tryFindFile path file

/// Returns the AppSettings for the key - Splitted on ;
/// [omit]
[<Obsolete("This is no longer supported on dotnetcore.")>]
let appSettings (key : string) (fallbackValue : string) = 
    let value = 
        let setting =
#if NETSTANDARD
            null
#else
            try 
                System.Configuration.ConfigurationManager.AppSettings.[key]
            with exn -> ""
#endif
        if not (String.isNullOrWhiteSpace setting) then setting
        else fallbackValue
    value.Split([| ';' |], StringSplitOptions.RemoveEmptyEntries)

/// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable.
/// [omit]
let tryFindPath settingsName fallbackValue tool = 
    let paths = appSettings settingsName fallbackValue
    match tryFindFile paths tool with
    | Some path -> Some path
    | None -> tryFindFileOnPath tool

/// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable.
/// [omit]
let findPath settingsName fallbackValue tool = 
    match tryFindPath settingsName fallbackValue tool with
    | Some file -> file
    | None -> tool

/// Parameter type for process execution.
type ExecParams = 
    { /// The path to the executable, without arguments. 
      Program : string
      /// The working directory for the program. Defaults to "".
      WorkingDirectory : string
      /// Command-line parameters in a string.
      CommandLine : string
      /// Command-line argument pairs. The value will be quoted if it contains
      /// a string, and the result will be appended to the CommandLine property.
      /// If the key ends in a letter or number, a space will be inserted between
      /// the key and the value.
      Args : (string * string) list }

/// Default parameters for process execution.
let defaultParams = 
    { Program = ""
      WorkingDirectory = ""
      CommandLine = ""
      Args = [] }

let private formatArgs args = 
    let delimit (str : string) = 
        if String.isLetterOrDigit (str.Chars(str.Length - 1)) then str + " "
        else str
    args
    |> Seq.map (fun (k, v) -> delimit k + quoteIfNeeded v)
    |> String.separated " "

/// Execute an external program asynchronously and return the exit code,
/// logging output and error messages to FAKE output. You can compose the result
/// with Async.Parallel to run multiple external programs at once, but be
/// sure that none of them depend on the output of another.
let asyncShellExec (args : ExecParams) = 
    async { 
        if String.isNullOrEmpty args.Program then invalidArg "args" "You must specify a program to run!"
        let commandLine = args.CommandLine + " " + formatArgs args.Args
        let info = 
            ProcessStartInfo
                (args.Program, UseShellExecute = false, 
                 RedirectStandardError = true, RedirectStandardOutput = true, RedirectStandardInput = true,
#if NETSTANDARD
                 CreateNoWindow = true,
#else
                 WindowStyle = ProcessWindowStyle.Hidden,
#endif 
                 WorkingDirectory = args.WorkingDirectory, 
                 Arguments = commandLine)
        use proc = new Process(StartInfo = info)
        proc.ErrorDataReceived.Add(fun e -> 
            if not (isNull e.Data) then Trace.traceError e.Data)
        proc.OutputDataReceived.Add(fun e -> 
            if not (isNull e.Data) then Trace.log e.Data)
        start proc
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()
        proc.StandardInput.Dispose()
        // attaches handler to Exited event, enables raising events, then awaits event
        // the event gets triggered even if process has already finished
        let! _ = Async.GuardedAwaitObservable proc.Exited (fun _ -> proc.EnableRaisingEvents <- true)
        return proc.ExitCode
    }


/// Kills all processes with the given id
let killProcessById id = Process.GetProcessById id |> kill

/// Returns all processes with the given name
let getProcessesByName (name : string) = 
    Process.GetProcesses()
    |> Seq.filter (fun p -> 
           try 
               not p.HasExited
           with exn -> false)
    |> Seq.filter (fun p -> 
           try 
               p.ProcessName.ToLower().StartsWith(name.ToLower())
           with exn -> false)

/// Kills all processes with the given name
let killProcess name = 
    Trace.tracefn "Searching for process with name = %s" name
    getProcessesByName name |> Seq.iter kill

/// Kills the F# Interactive (FSI) process.
let killFSI() = killProcess "fsi.exe"

/// Kills the MSBuild process.
let killMSBuild() = killProcess "msbuild"

/// Kills all processes that are created by the FAKE build script unless "donotkill" flag was set.
let killAllCreatedProcesses() =
    match getStartedProcesses() with
    | Some startedProcesses when shouldKillCreatedProcesses() ->
        startedProcesses.KillAll()
    | _ -> ()

/// Waits until the processes with the given name have stopped or fails after given timeout.
/// ## Parameters
///  - `name` - The name of the processes in question.
///  - `timeout` - The timespan to time out after.
let ensureProcessesHaveStopped name timeout =
    let endTime = DateTime.Now.Add timeout
    while DateTime.Now <= endTime && not (getProcessesByName name |> Seq.isEmpty) do
        Trace.tracefn "Waiting for %s to stop (Timeout: %A)" name endTime
        Thread.Sleep 1000
    if not (getProcessesByName name |> Seq.isEmpty) then
        failwithf "The process %s has not stopped (check the logs for errors)" name

/// Execute an external program and return the exit code.
/// [omit]
let shellExec args = args |> asyncShellExec |> Async.RunSynchronously

/// Allows to exec shell operations synchronously and asynchronously.
type Shell() = 
    
    static member private GetParams(cmd, ?args, ?dir) = 
        let args = defaultArg args ""
        let dir = defaultArg dir (Directory.GetCurrentDirectory())
        { WorkingDirectory = dir
          Program = cmd
          CommandLine = args
          Args = [] }
    
    /// Runs the given process, waits for it's completion and returns the exit code.
    /// ## Parameters
    ///
    ///  - `cmd` - The command which should be run in elavated context.
    ///  - `args` - The process arguments (optional).
    ///  - `directory` - The working directory (optional).
    static member Exec(cmd, ?args, ?dir) = shellExec (Shell.GetParams(cmd, ?args = args, ?dir = dir))
    
    /// Runs the given process asynchronously.
    /// ## Parameters
    ///
    ///  - `cmd` - The command which should be run in elavated context.
    ///  - `args` - The process arguments (optional).
    ///  - `directory` - The working directory (optional).
    static member AsyncExec(cmd, ?args, ?dir) = asyncShellExec (Shell.GetParams(cmd, ?args = args, ?dir = dir))
