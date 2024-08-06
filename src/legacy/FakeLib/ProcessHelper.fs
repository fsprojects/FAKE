﻿[<AutoOpen>]
/// Contains functions which can be used to start other tools.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
module Fake.ProcessHelper

open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Threading
open System.Text
open System.Collections.Generic
open System.Collections.Concurrent
open System.ServiceProcess
open Fake.Core.GuardedAwaitObservable


/// If set to true the ProcessHelper will start all processes with a custom ProcessEncoding.
/// If set to false (default) only mono processes will be changed.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let mutable AlwaysSetProcessEncoding = false

/// The ProcessHelper will start all processes with this encoding if AlwaysSetProcessEncoding is set to true.
/// If AlwaysSetProcessEncoding is set to false (default) only mono processes will be changed.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let mutable ProcessEncoding = Encoding.UTF8

/// [omit]
type internal ConcurrentBag<'T> with

    member internal this.Clear() =
        while not (this.IsEmpty) do
            this.TryTake() |> ignore

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let startedProcesses = ConcurrentBag()

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let start (proc: Process) =
    if isMono || AlwaysSetProcessEncoding then
        try
            System.Console.OutputEncoding <- ProcessEncoding
        with exn ->
            logfn "Failed setting console encoding, ignoring error. Message: %s." exn.Message

    if isMono && proc.StartInfo.FileName.ToLowerInvariant().EndsWith(".exe") then
        proc.StartInfo.Arguments <- "--debug \"" + proc.StartInfo.FileName + "\" " + proc.StartInfo.Arguments
        proc.StartInfo.FileName <- monoPath

    proc.Start() |> ignore
    startedProcesses.Add(proc.Id, proc.StartTime) |> ignore

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let mutable redirectOutputToTrace = false

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let mutable enableProcessTracing = true

/// A record type which captures console messages
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
type ConsoleMessage =
    { IsError: bool
      Message: string
      Timestamp: DateTimeOffset }

/// A process result including error code, message log and errors.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
type ProcessResult =
    { ExitCode: int
      Messages: List<string>
      Errors: List<string> }

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
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let ExecProcessWithLambdas configProcessStartInfoF (timeOut: TimeSpan) silent errorF messageF =
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
    platformInfoAction proc.StartInfo

    if isNullOrEmpty proc.StartInfo.WorkingDirectory |> not then
        if Directory.Exists proc.StartInfo.WorkingDirectory |> not then
            failwithf
                "Start of process %s failed. WorkingDir %s does not exist."
                proc.StartInfo.FileName
                proc.StartInfo.WorkingDirectory

    if silent then
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.RedirectStandardError <- true

        if isMono || AlwaysSetProcessEncoding then
            proc.StartInfo.StandardOutputEncoding <- ProcessEncoding
            proc.StartInfo.StandardErrorEncoding <- ProcessEncoding

        proc.ErrorDataReceived.Add(fun d ->
            if d.Data <> null then
                errorF d.Data)

        proc.OutputDataReceived.Add(fun d ->
            if d.Data <> null then
                messageF d.Data)

    try
        if enableProcessTracing && (not <| proc.StartInfo.FileName.EndsWith "fsi.exe") then
            tracefn "%s %s" proc.StartInfo.FileName proc.StartInfo.Arguments

        start proc
    with exn ->
        failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message

    if silent then
        proc.BeginErrorReadLine()
        proc.BeginOutputReadLine()

    if timeOut = TimeSpan.MaxValue then
        proc.WaitForExit()
    else if not <| proc.WaitForExit(int timeOut.TotalMilliseconds) then
        try
            proc.Kill()
        with exn ->
            traceError
            <| sprintf "Could not kill process %s  %s after timeout." proc.StartInfo.FileName proc.StartInfo.Arguments

        failwithf "Process %s %s timed out." proc.StartInfo.FileName proc.StartInfo.Arguments
    // See http://stackoverflow.com/a/16095658/1149924 why WaitForExit must be called twice.
    proc.WaitForExit()
    proc.ExitCode

/// Runs the given process and returns the process result.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let ExecProcessAndReturnMessages configProcessStartInfoF timeOut =
    let errors = new List<_>()
    let messages = new List<_>()

    let exitCode =
        ExecProcessWithLambdas configProcessStartInfoF timeOut true (errors.Add) (messages.Add)

    ProcessResult.New exitCode messages errors

/// Runs the given process and returns the process result.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let ExecProcessRedirected configProcessStartInfoF timeOut =
    let messages = ref []

    let appendMessage isError msg =
        messages
        := { IsError = isError
             Message = msg
             Timestamp = DateTimeOffset.UtcNow }
           :: !messages

    let exitCode =
        ExecProcessWithLambdas configProcessStartInfoF timeOut true (appendMessage true) (appendMessage false)

    exitCode = 0, (!messages |> List.rev |> Seq.ofList)

/// Runs the given process and returns the exit code.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
///  - `silent` - If this flag is set then the process output is redicted to the trace.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let execProcess2 configProcessStartInfoF timeOut silent =
    ExecProcessWithLambdas configProcessStartInfoF timeOut silent traceError trace

/// Runs the given process and returns the exit code.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let execProcessAndReturnExitCode configProcessStartInfoF timeOut =
    ExecProcessWithLambdas configProcessStartInfoF timeOut true traceError trace

/// Runs the given process and returns if the exit code was 0.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let execProcess3 configProcessStartInfoF timeOut =
    ExecProcessWithLambdas configProcessStartInfoF timeOut true traceError trace = 0

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
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let ExecProcess configProcessStartInfoF timeOut =
    ExecProcessWithLambdas configProcessStartInfoF timeOut redirectOutputToTrace traceError trace

/// Runs the given process in an elevated context and returns the exit code.
/// ## Parameters
///
///  - `cmd` - The command which should be run in elavated context.
///  - `args` - The process arguments.
///  - `timeOut` - The timeout for the process.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let ExecProcessElevated cmd args timeOut =
    ExecProcess
        (fun si ->
            si.Verb <- "runas"
            si.Arguments <- args
            si.FileName <- cmd
            si.UseShellExecute <- true)
        timeOut

/// Gets the list of valid directories included in the PATH environment variable.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let pathDirectories =
    splitEnvironVar "PATH"
    |> Seq.map (fun value -> value.Trim())
    |> Seq.filter (fun value -> not <| isNullOrEmpty value)
    |> Seq.filter isValidPath

/// Sets the environment Settings for the given startInfo.
/// Existing values will be overriden.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let setEnvironmentVariables (startInfo: ProcessStartInfo) environmentSettings =
    for key, value in environmentSettings do
        if startInfo.EnvironmentVariables.ContainsKey key then
            startInfo.EnvironmentVariables.[key] <- value
        else
            startInfo.EnvironmentVariables.Add(key, value)

/// Runs the given process and returns true if the exit code was 0.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let execProcess configProcessStartInfoF timeOut =
    ExecProcess configProcessStartInfoF timeOut = 0

/// Starts the given process and returns immediatly.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let fireAndForget configProcessStartInfoF =
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo

    try
        start proc
    with exn ->
        failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message

/// Runs the given process, waits for its completion and returns if it succeeded.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let directExec configProcessStartInfoF =
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo

    try
        start proc
    with exn ->
        failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message

    proc.WaitForExit()
    proc.ExitCode = 0

/// Starts the given process and forgets about it.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let StartProcess configProcessStartInfoF =
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
    start proc

/// Sends a command to a remote windows service.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let RunRemoteService command host serviceName =
    let host, address =
        match host with
        | "." -> Environment.MachineName, ""
        | _ -> host, @"\\" + host

    tracefn "%s %s on %s" command serviceName host

    if
        not
        <| directExec (fun p ->
            p.FileName <- "sc"
            p.Arguments <- sprintf @"%s %s %s" address command serviceName
            p.RedirectStandardOutput <- true)
    then
        failwith "Failed to send command to service."

/// Sends a command to a local windows service.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let RunService command serviceName =
    RunRemoteService command "." serviceName

/// Stops a local windows service. Waits up to two minutes for a response.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let StopService serviceName =
    stopService serviceName
    ensureServiceHasStopped serviceName (TimeSpan.FromMinutes 2.)

/// Stops a remote windows service. Waits up to two minutes for a response.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let StopRemoteService host serviceName =
    stopRemoteService host serviceName
    ensureRemoteServiceHasStopped host serviceName (TimeSpan.FromMinutes 2.)

/// Starts a local windows service. Waits up to two minutes for a response.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let StartService serviceName =
    startService serviceName
    ensureServiceHasStarted serviceName (TimeSpan.FromMinutes 2.)

/// Starts a remote windows service. Waits up to two minutes for a response.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let StartRemoteService host serviceName =
    startRemoteService host serviceName
    ensureRemoteServiceHasStarted host serviceName (TimeSpan.FromMinutes 2.)

/// Adds quotes around the string
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let quote (str: string) = "\"" + str.Replace("\"", "\\\"") + "\""

/// Adds quotes around the string if needed
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let quoteIfNeeded str =
    if isNullOrEmpty str then ""
    elif str.Contains " " then quote str
    else str

/// Adds quotes and a blank around the string´.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let toParam x = " " + quoteIfNeeded x

/// Use default Parameters
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let UseDefaults = id

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let stringParam (paramName, paramValue) =
    if isNullOrEmpty paramValue then
        None
    else
        Some(paramName, quote paramValue)

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let multipleStringParams paramName =
    Seq.map (fun x -> stringParam (paramName, x)) >> Seq.toList

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let optionParam (paramName, paramValue) =
    match paramValue with
    | Some x -> Some(paramName, x.ToString())
    | None -> None

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let boolParam (paramName, paramValue) =
    if paramValue then Some(paramName, null) else None

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let parametersToString flagPrefix delimiter parameters =
    parameters
    |> Seq.choose id
    |> Seq.map (fun (paramName, paramValue) ->
        flagPrefix
        + paramName
        + if isNullOrEmpty paramValue then
              ""
          else
              delimiter + paramValue)
    |> separated " "

/// Searches the given directories for all occurrences of the given file name
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let tryFindFile dirs file =
    let files =
        dirs
        |> Seq.map (fun (path: string) ->
            let dir =
                path
                |> replace "[ProgramFiles]" ProgramFiles
                |> replace "[ProgramFilesX86]" ProgramFilesX86
                |> replace "[SystemRoot]" SystemRoot
                |> directoryInfo

            if not dir.Exists then
                ""
            else
                let fi = dir.FullName @@ file |> fileInfo
                if fi.Exists then fi.FullName else "")
        |> Seq.filter ((<>) "")
        |> Seq.cache

    if not (Seq.isEmpty files) then
        Some(Seq.head files)
    else
        None

/// Searches the given directories for the given file, failing if not found.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let findFile dirs file =
    match tryFindFile dirs file with
    | Some found -> found
    | None -> failwithf "%s not found in %A." file dirs

/// Searches the current directory and the directories within the PATH
/// environment variable for the given file. If successful returns the full
/// path to the file.
/// ## Parameters
///  - `file` - The file to locate
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let tryFindFileOnPath (file: string) : string option =
    pathDirectories |> Seq.append [ "." ] |> (fun path -> tryFindFile path file)

/// Returns the AppSettings for the key - Splitted on ;
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let appSettings (key: string) (fallbackValue: string) =
    let value =
        let setting =
            try
                System.Configuration.ConfigurationManager.AppSettings.[key]
            with exn ->
                ""

        if not (isNullOrWhiteSpace setting) then
            setting
        else
            fallbackValue

    value.Split([| ';' |], StringSplitOptions.RemoveEmptyEntries)

/// tries to find the tool on the paths given. If not found then try on the system PATH.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let tryFindFileInDirsThenPath paths tool =
    match tryFindFile paths tool with
    | Some path -> Some path
    | None -> tryFindFileOnPath tool

/// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let tryFindPath settingsName fallbackValue tool =
    let paths = appSettings settingsName fallbackValue
    tryFindFileInDirsThenPath paths tool

/// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let findPath settingsName fallbackValue tool =
    match tryFindPath settingsName fallbackValue tool with
    | Some file -> file
    | None -> tool

/// Parameter type for process execution.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
[<CLIMutable>]
type ExecParams =
    {
        /// The path to the executable, without arguments.
        Program: string
        /// The working directory for the program. Defaults to "".
        WorkingDirectory: string
        /// Command-line parameters in a string.
        CommandLine: string
        /// Command-line argument pairs. The value will be quoted if it contains
        /// a string, and the result will be appended to the CommandLine property.
        /// If the key ends in a letter or number, a space will be inserted between
        /// the key and the value.
        Args: (string * string) list
    }

/// Default parameters for process execution.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let defaultParams =
    { Program = ""
      WorkingDirectory = ""
      CommandLine = ""
      Args = [] }

let private formatArgs args =
    let delimit (str: string) =
        if isLetterOrDigit (str.Chars(str.Length - 1)) then
            str + " "
        else
            str

    args |> Seq.map (fun (k, v) -> delimit k + quoteIfNeeded v) |> separated " "

/// Execute an external program asynchronously and return the exit code,
/// logging output and error messages to FAKE output. You can compose the result
/// with Async.Parallel to run multiple external programs at once, but be
/// sure that none of them depend on the output of another.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let asyncShellExec (args: ExecParams) =
    async {
        if isNullOrEmpty args.Program then
            invalidArg "args" "You must specify a program to run!"

        let commandLine = args.CommandLine + " " + formatArgs args.Args

        let info =
            ProcessStartInfo(
                args.Program,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = args.WorkingDirectory,
                Arguments = commandLine
            )

        let proc = new Process(StartInfo = info)

        try
            proc.ErrorDataReceived.Add(fun e ->
                if e.Data <> null then
                    traceError e.Data)

            proc.OutputDataReceived.Add(fun e ->
                if e.Data <> null then
                    log e.Data)

            start proc
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()
            proc.StandardInput.Close()
            // attaches handler to Exited event, enables raising events, then awaits event
            // the event gets triggered even if process has already finished
            let! _ = Async.GuardedAwaitObservable proc.Exited (fun _ -> proc.EnableRaisingEvents <- true)
            return proc.ExitCode
        finally
            // add a delay because we were seeing ObjectDisposedException when running shell commands on
            // osx. Github issue #1424.
            Async.Sleep(10) |> Async.RunSynchronously
            proc.Dispose()
    }

/// Kills the given process
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let kill (proc: Process) =
    tracefn "Trying to kill process %s (Id = %d)" proc.ProcessName proc.Id

    try
        proc.Kill()
    with exn ->
        tracefn
            "Could not kill process %s (Id = %d).%sMessage: %s"
            proc.ProcessName
            proc.Id
            Environment.NewLine
            exn.Message

/// Kills all processes with the given id
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let killProcessById id = Process.GetProcessById id |> kill

/// Returns all processes with the given name
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let getProcessesByName (name: string) =
    Process.GetProcesses()
    |> Seq.filter (fun p ->
        try
            not p.HasExited
        with exn ->
            false)
    |> Seq.filter (fun p ->
        try
            p.ProcessName.ToLower().StartsWith(name.ToLower())
        with exn ->
            false)

/// Kills all processes with the given name
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let killProcess name =
    tracefn "Searching for processes with name = %s" name

    Process.GetProcesses()
    |> Seq.filter (fun p ->
        try
            p.ProcessName.ToLower().StartsWith(name.ToLower())
        with exn ->
            false)
    |> Seq.iter kill

/// Kills the F# Interactive (FSI) process.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let killFSI () = killProcess "fsi.exe"

/// Kills the MSBuild process.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let killMSBuild () = killProcess "msbuild"

/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let mutable killCreatedProcesses = true

/// Kills all processes that are created by the FAKE build script unless "donotkill" flag was set.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let killAllCreatedProcesses () =
    if not killCreatedProcesses then
        ()
    else
        let traced = ref false

        for pid, startTime in startedProcesses do
            try
                let proc = Process.GetProcessById pid

                // process IDs may be reused by the operating system so we need
                // to make sure the process is indeed the one we started
                if proc.StartTime = startTime && not proc.HasExited then
                    try
                        if not !traced then
                            tracefn "Killing all processes that are created by FAKE and are still running."
                            traced := true

                        logfn "Trying to kill %s" proc.ProcessName
                        kill proc
                    with exn ->
                        logfn "Killing %s failed with %s" proc.ProcessName exn.Message
            with exn ->
                ()

        startedProcesses.Clear()

/// Waits until the processes with the given name have stopped or fails after given timeout.
/// ## Parameters
///  - `name` - The name of the processes in question.
///  - `timeout` - The timespan to time out after.
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let ensureProcessesHaveStopped name timeout =
    let endTime = DateTime.Now.Add timeout

    while DateTime.Now <= endTime && not (getProcessesByName name |> Seq.isEmpty) do
        tracefn "Waiting for %s to stop (Timeout: %A)" name endTime
        Thread.Sleep 1000

    if not (getProcessesByName name |> Seq.isEmpty) then
        failwithf "The process %s has not stopped (check the logs for errors)" name

/// Execute an external program and return the exit code.
/// [omit]
[<System.Obsolete("FAKE0001 Use the Fake.Core.Process module instead")>]
let shellExec args =
    args |> asyncShellExec |> Async.RunSynchronously

/// Allows to exec shell operations synchronously and asynchronously.
[<System.Obsolete("FAKE0001 Use `open Fake.Core` and `Process.Shell`")>]
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
    [<System.Obsolete("FAKE0001 Use `open Fake.Core` and `Process.Shell.Exec`")>]
    static member Exec(cmd, ?args, ?dir) =
        shellExec (Shell.GetParams(cmd, ?args = args, ?dir = dir))

    /// Runs the given process asynchronously.
    /// ## Parameters
    ///
    ///  - `cmd` - The command which should be run in elavated context.
    ///  - `args` - The process arguments (optional).
    ///  - `directory` - The working directory (optional).
    [<System.Obsolete("FAKE0001 Use `open Fake.Core` and `Process.Shell.AsyncExec`")>]
    static member AsyncExec(cmd, ?args, ?dir) =
        asyncShellExec (Shell.GetParams(cmd, ?args = args, ?dir = dir))
