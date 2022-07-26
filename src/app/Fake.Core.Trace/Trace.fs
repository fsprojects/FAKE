namespace Fake.Core

/// This module contains function which allow to trace build output
[<RequireQualifiedAccess>]
module Trace =

    open Fake.Core

    open System
    open System.Reflection
    open System.Threading

    /// Define a FAKE exception type
    type FAKEException(msg) =
        inherit Exception(msg)

    let private openTags =
        new ThreadLocal<list<System.Diagnostics.Stopwatch * KnownTags>>(fun _ -> [])

    let mutable private verbose =
        Environment.environVarAsBoolOrDefault "FAKE_COREFX_VERBOSE" false

    let mutable private hintPrinted = false
    let setVerbose () = verbose <- true

    /// Logs the specified string
    ///
    /// ## Parameters
    /// 
    ///  - `message` - The message to log
    let log message =
        TraceData.LogMessage(message, true) |> CoreTracing.postMessage

    /// Checks if FAKE is running in verbose mode
    ///
    /// ## Parameters
    /// 
    ///  - `printHint` - Flag to mark if a hint will be written to log for verbosity
    let isVerbose printHint =
        if printHint && not hintPrinted then
            hintPrinted <- true

            if not verbose then
                let hint =
                    "Note: To further diagnose the following problem you can increase verbosity of the fake modules by setting the 'FAKE_COREFX_VERBOSE' environment variable to 'true' or by using 'Trace.setVerbose()' in your script."
                log hint

        verbose

    /// Logs the specified message
    ///
    /// ## Parameters
    /// 
    ///  - `fmt` - The formatted message to log
    let logfn fmt = Printf.ksprintf log fmt

    /// Logs the specified message (without line break)
    ///
    /// ## Parameters
    /// 
    ///  - `fmt` - The formatted message to log
    let logf fmt =
        Printf.ksprintf (fun text -> CoreTracing.postMessage (TraceData.LogMessage(text, false))) fmt

    /// Logs the specified string if the verbose mode is activated.
    ///
    /// ## Parameters
    /// 
    ///  - `fmt` - The formatted message to log
    let logVerbosefn fmt =
        Printf.ksprintf (if isVerbose false then log else ignore) fmt

    /// Writes a trace to the command line (in green)
    ///
    /// ## Parameters
    /// 
    ///  - `message` - The message to log
    let trace message =
        CoreTracing.postMessage (TraceData.TraceMessage(message, true))

    /// Writes a message to the command line (in green)
    ///
    /// ## Parameters
    /// 
    ///  - `fmt` - The formatted message to log
    let tracefn fmt = Printf.ksprintf trace fmt

    /// Writes a message to the command line (in green) and without a line break
    ///
    /// ## Parameters
    /// 
    ///  - `fmt` - The formatted message to log
    let tracef fmt =
        Printf.ksprintf (fun text -> CoreTracing.postMessage (TraceData.TraceMessage(text, false))) fmt

    /// Writes a trace to the command line (in green) if the verbose mode is activated.
    ///
    /// ## Parameters
    /// 
    ///  - `s` - The message to trace
    let traceVerbose s =
        if isVerbose false then
            trace s

    /// Writes a trace to stderr (in yellow)
    ///
    /// ## Parameters
    /// 
    ///  - `text` - The text to trace
    let traceImportant text =
        CoreTracing.postMessage (TraceData.ImportantMessage text)

    /// Writes a message to stderr (in yellow)
    ///
    /// ## Parameters
    /// 
    ///  - `fmt` - The formatted message to trace
    let traceImportantfn fmt = Printf.ksprintf traceImportant fmt

    /// Writes a trace to the command line (in yellow)
    ///
    /// ## Parameters
    /// 
    ///  - `fmt` - The formatted message to trace
    let traceFAKE fmt =
        Printf.ksprintf (TraceData.ImportantMessage >> CoreTracing.postMessage) fmt

    /// Traces an error (in red)
    ///
    /// ## Parameters
    /// 
    ///  - `error` - The error message to trace
    let traceError error =
        CoreTracing.postMessage (TraceData.ErrorMessage error)

    /// Writes an error message to stderr (in red)
    ///
    /// ## Parameters
    /// 
    ///  - `fmt` - The formatted error message to trace
    let traceErrorfn fmt = Printf.ksprintf traceError fmt

    open Microsoft.FSharp.Core.Printf

    /// Converts an exception and its inner exceptions to a nice string.
    ///
    /// ## Parameters
    /// 
    ///  - `ex` - The exception to convert
    let exceptionAndInnersToString (ex: Exception) =
        let sb = Text.StringBuilder()
        let delimiter = String.replicate 50 "*"
        let nl = Environment.NewLine

        let rec printException (e: Exception) count =
            if (e :? TargetException && not (isNull e.InnerException)) then
                printException e.InnerException count
            else
                if (count = 1) then
                    bprintf sb "Exception Message:%s%s%s" e.Message nl delimiter
                else
                    bprintf sb "%s%s%d)Exception Message:%s%s%s" nl nl count e.Message nl delimiter

                bprintf sb "%sType: %s" nl (e.GetType().FullName)
                // Loop through the public properties of the exception object
                // and record their values.
                e.GetType().GetTypeInfo().GetProperties()
                |> Array.iter (fun p ->
                    // Do not log information for the InnerException or StackTrace.
                    // This information is captured later in the process.
                    if
                        (p.Name <> "InnerException"
                         && p.Name <> "StackTrace"
                         && p.Name <> "Message"
                         && p.Name <> "Data")
                    then
                        try
                            let value = p.GetValue(e, null)

                            if (not (isNull value)) then
                                bprintf sb "%s%s: %s" nl p.Name (value.ToString())
                        with e2 ->
                            bprintf sb "%s%s: %s" nl p.Name e2.Message)

                if not (isNull e.StackTrace) then
                    bprintf sb "%s%sStackTrace%s%s%s" nl nl nl delimiter nl
                    bprintf sb "%s%s" nl e.StackTrace

                if not (isNull e.InnerException) then
                    printException e.InnerException (count + 1)

        printException ex 1
        sb.ToString()

    /// Traces an exception details (in red)
    ///
    /// ## Parameters
    /// 
    ///  - `ex` - The exception to trace
    let traceException (ex: Exception) =
        exceptionAndInnersToString ex |> traceError

    /// Traces the EnvironmentVariables
    let traceEnvironmentVariables () =
#if !DOTNETCORE
        // [ EnvironTarget.Machine; EnvironTarget.Process; EnvironTarget.User ]
        // |> Seq.iter (fun mode ->
        //        tracefn "Environment-Settings (%A):" mode
        //        environVars mode |> Seq.iter (tracefn "  %A"))
        tracefn "Environment-Settings :\n"
        Environment.environVars () |> Seq.iter (fun (a, b) -> tracefn "  %A - %A" a b)

#else
        tracefn "Environment-Settings (%A):" "Process"
        Environment.environVars () |> Seq.iter (tracefn "  %A")
#endif

    /// Traces a line
    let traceLine () =
        trace "---------------------------------------------------------------------"

    /// Traces a header
    ///
    /// ## Parameters
    /// 
    ///  - `name` - The header value
    let traceHeader name =
        trace ""
        traceLine ()
        trace name
        traceLine ()

    /// Puts an opening tag on the internal tag stack
    ///
    /// ## Parameters
    /// 
    ///  - `tag` - The tag to insert
    ///  - `description` - The tag description
    let openTagUnsafe tag description =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        openTags.Value <- (sw, tag) :: openTags.Value

        TraceData.OpenTag(
            tag,
            if String.IsNullOrEmpty description then
                None
            else
                Some description
        )
        |> CoreTracing.postMessage

    /// A safe disposable type for tracing
    type ISafeDisposable =
        inherit IDisposable
        abstract MarkSuccess: unit -> unit
        abstract MarkFailed: unit -> unit

    let private asSafeDisposable f =
        let mutable state = TagStatus.Failed
        let mutable isDisposed = false

        { new ISafeDisposable with
            member _.MarkSuccess() = state <- TagStatus.Success
            member _.MarkFailed() = state <- TagStatus.Failed

            member _.Dispose() =
                if not isDisposed then
                    isDisposed <- true
                    f state }

    /// Removes an opening tag from the internal tag stack
    ///
    /// ## Parameters
    /// 
    ///  - `tag` - The tag to close
    let closeTagUnsafeEx status tag =
        let time =
            match openTags.Value with
            | (sw, x) :: rest when x = tag ->
                openTags.Value <- rest
                sw.Elapsed
            | _ -> failwithf "Invalid tag structure. Trying to close %A tag but stack is %A" tag openTags

        TraceData.CloseTag(tag, time, status) |> CoreTracing.postMessage

    /// Removes an opening tag from the internal tag stack
    /// 
    /// ## Parameters
    /// 
    ///  - `tag` - The tag to insert
    let closeTagUnsafe tag = closeTagUnsafeEx TagStatus.Success tag

    /// Traces a tag
    /// 
    /// ## Parameters
    /// 
    ///  - `tag` - The tag to trace
    ///  - `description` - the tag description
    let traceTag tag description =
        openTagUnsafe tag description
        asSafeDisposable (fun state -> closeTagUnsafeEx state tag)

    /// Set build state with the given tag and message
    /// 
    /// ## Parameters
    /// 
    ///  - `tag` - The tag to trace
    ///  - `message` - the build message
    let setBuildStateWithMessage tag message =
        TraceData.BuildState(tag, Some(message)) |> CoreTracing.postMessage

    /// Set build state with the given tag
    /// 
    /// ## Parameters
    /// 
    ///  - `tag` - The tag to trace
    let setBuildState tag =
        TraceData.BuildState(tag, None) |> CoreTracing.postMessage

    /// Set status for the given test 
    /// 
    /// ## Parameters
    /// 
    ///  - `testName` - The test name
    ///  - `testStatus` - The test status
    let testStatus testName testStatus =
        // TODO: Check if the given test is opened in openTags-stack?
        TraceData.TestStatus(testName, testStatus) |> CoreTracing.postMessage

    /// Trace test output and errors
    /// 
    /// ## Parameters
    /// 
    ///  - `testName` - The test name
    ///  - `out` - The test output
    ///  - `err` - The test error
    let testOutput testName out err =
        // TODO: Check if the given test is opened in openTags-stack?
        TraceData.TestOutput(testName, out, err) |> CoreTracing.postMessage

    /// Publish given type in given path
    /// 
    /// ## Parameters
    /// 
    ///  - `typ` - The type to publish
    ///  - `path` - The path to publish type to
    let publish typ path =
        TraceData.ImportData(typ, path) |> CoreTracing.postMessage

    /// Trace the given build number
    /// 
    /// ## Parameters
    /// 
    ///  - `number` - The build number to trace
    let setBuildNumber number =
        TraceData.BuildNumber number |> CoreTracing.postMessage

    /// Closes all opened tags
    let closeAllOpenTags () =
        Seq.iter (fun (_, tag) -> closeTagUnsafeEx TagStatus.Failed tag) openTags.Value

    /// Traces the begin of a target
    /// 
    /// ## Parameters
    /// 
    ///  - `name` - The name of the target
    ///  - `description` - The description of the target
    ///  - `dependencyString` - The target dependency string
    let traceStartTargetUnsafe name description (dependencyString: string) =
        openTagUnsafe (KnownTags.Target name) description

    /// Traces the begin of a final target
    /// 
    /// ## Parameters
    /// 
    ///  - `name` - The name of the target
    ///  - `description` - The description of the target
    ///  - `dependencyString` - The target dependency string
    let traceStartFinalTargetUnsafe name description (dependencyString: string) =
        openTagUnsafe (KnownTags.FinalTarget name) description

    /// Traces the begin of a failure target
    /// 
    /// ## Parameters
    /// 
    ///  - `name` - The name of the target
    ///  - `description` - The description of the target
    ///  - `dependencyString` - The target dependency string
    let traceStartFailureTargetUnsafe name description (dependencyString: string) =
        openTagUnsafe (KnownTags.FailureTarget name) description

    /// Traces the end of a target
    /// 
    /// ## Parameters
    /// 
    ///  - `state` - The target state
    ///  - `name` - The name of the target
    let traceEndTargetUnsafeEx state name =
        closeTagUnsafeEx state (KnownTags.Target name)

    /// Traces the end of a final target
    /// 
    /// ## Parameters
    /// 
    ///  - `state` - The target state
    ///  - `name` - The name of the target
    let traceEndFinalTargetUnsafeEx state name =
        closeTagUnsafeEx state (KnownTags.FinalTarget name)

    /// Traces the end of a failure target
    /// 
    /// ## Parameters
    /// 
    ///  - `state` - The target state
    ///  - `name` - The name of the target
    let traceEndFailureTargetUnsafeEx state name =
        closeTagUnsafeEx state (KnownTags.FailureTarget name)

    /// Traces the end of a target
    /// 
    /// ## Parameters
    /// 
    ///  - `name` - The name of the target
    let traceEndTargetUnsafe name =
        traceEndTargetUnsafeEx TagStatus.Success name

    /// Traces a target
    /// 
    /// ## Parameters
    /// 
    ///  - `name` - The name of the target
    ///  - `description` - The description of the target
    ///  - `dependencyString` - The target dependency string
    let traceTarget name description dependencyString =
        traceStartTargetUnsafe name description dependencyString
        asSafeDisposable (fun state -> traceEndTargetUnsafeEx state name)

    /// Traces a final target
    /// 
    /// ## Parameters
    /// 
    ///  - `name` - The name of the target
    ///  - `description` - The description of the target
    ///  - `dependencyString` - The target dependency string
    let traceFinalTarget name description dependencyString =
        traceStartFinalTargetUnsafe name description dependencyString
        asSafeDisposable (fun state -> traceEndFinalTargetUnsafeEx state name)

    /// Traces a failed target
    /// 
    /// ## Parameters
    /// 
    ///  - `name` - The name of the target
    ///  - `description` - The description of the target
    ///  - `dependencyString` - The target dependency string
    let traceFailureTarget name description dependencyString =
        traceStartFailureTargetUnsafe name description dependencyString
        asSafeDisposable (fun state -> traceEndFailureTargetUnsafeEx state name)

    /// Traces the begin of a task
    /// 
    /// ## Parameters
    /// 
    ///  - `task` - The name of the task
    ///  - `description` - The description of the task
    let traceStartTaskUnsafe task description =
        openTagUnsafe (KnownTags.Task task) description

    /// Traces the end of a task
    /// 
    /// ## Parameters
    /// 
    ///  - `state` - The state of the task
    ///  - `task` - The name of the task
    let traceEndTaskUnsafeEx state task =
        closeTagUnsafeEx state (KnownTags.Task task)

    /// Traces the end of a task
    /// 
    /// ## Parameters
    /// 
    ///  - `task` - The name of the task
    let traceEndTaskUnsafe task =
        traceEndTaskUnsafeEx TagStatus.Success task

    /// Wrap functions in a 'use' of this function
    /// 
    /// ## Parameters
    /// 
    ///  - `name` - The name of the task
    ///  - `description` - The description of the task
    let traceTask name description =
        traceStartTaskUnsafe name description
        asSafeDisposable (fun state -> traceEndTaskUnsafeEx state name)

    /// Allows automatic or manual tracing around a function being run
    /// If in automatic success mode and no exception is thrown then trace is marked as success
    /// Any exception thrown will result in a mark failed and exception re-thrown
    /// 
    /// ## Parameters
    /// 
    ///  - `automaticSuccess` - Flag to mark trace task as success
    ///  - `func` - Callback to call on result of task trace
    ///  - `trace` - The trace instance
    let inline useWith automaticSuccess func (trace: ISafeDisposable) =
        try
            try
                let result = func trace

                if automaticSuccess then
                    trace.MarkSuccess()

                result
            with _ ->
                trace.MarkFailed()
                reraise ()
        finally
            trace.Dispose()

#if DOTNETCORE
    type EventLogEntryType =
        | Error
        | Information
        | Warning
        | Other
#endif
    /// Traces the message to the console
    /// 
    /// ## Parameters
    /// 
    ///  - `msg` - The message to log
    ///  - `eventLogEntry` - The message log level
    let logToConsole (msg, eventLogEntry: EventLogEntryType) =
        let safeMessage = TraceSecrets.guardMessage msg

        match eventLogEntry with
        | EventLogEntryType.Error -> TraceData.ErrorMessage safeMessage
        | EventLogEntryType.Information -> TraceData.TraceMessage(safeMessage, true)
        | EventLogEntryType.Warning -> TraceData.ImportantMessage safeMessage
        | _ -> TraceData.LogMessage(safeMessage, true)
        |> CoreTracing.defaultConsoleTraceListener.Write

    /// Logs the given files with the message.
    /// 
    /// ## Parameters
    /// 
    ///  - `message` - The message to log
    ///  - `items` - The files to log message to
    let logItems message items =
        items |> Seq.iter (log << sprintf "%s%s" message)
