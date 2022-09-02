namespace Fake.Core

/// <summary>
/// This module contains function which allow to trace build output
/// </summary>
[<RequireQualifiedAccess>]
module Trace =

    open Fake.Core

    open System
    open System.Reflection
    open System.Threading

    /// <summary>
    /// Define a FAKE exception type
    /// </summary>
    type FAKEException(msg) =
        inherit Exception(msg)

    let private openTags =
        new ThreadLocal<list<System.Diagnostics.Stopwatch * KnownTags>>(fun _ -> [])

    let mutable private verbose =
        Environment.environVarAsBoolOrDefault "FAKE_COREFX_VERBOSE" false

    let mutable private hintPrinted = false
    let setVerbose () = verbose <- true

    /// <summary>
    /// Logs the specified string
    /// </summary>
    ///
    /// <param name="message">The message to log</param>
    let log message =
        TraceData.LogMessage(message, true) |> CoreTracing.postMessage

    /// <summary>
    /// Checks if FAKE is running in verbose mode
    /// </summary>
    ///
    /// <param name="printHint">Flag to mark if a hint will be written to log for verbosity</param>
    let isVerbose printHint =
        if printHint && not hintPrinted then
            hintPrinted <- true

            if not verbose then
                let hint =
                    "Note: To further diagnose the following problem you can increase verbosity of the fake modules by setting the 'FAKE_COREFX_VERBOSE' environment variable to 'true' or by using 'Trace.setVerbose()' in your script."
                log hint

        verbose

    /// <summary>
    /// Logs the specified message
    /// </summary>
    ///
    /// <param name="fmt">The formatted message to log</param>
    let logfn fmt = Printf.ksprintf log fmt

    /// <summary>
    /// Logs the specified message (without line break)
    /// </summary>
    ///
    /// <param name="fmt">The formatted message to log</param>
    let logf fmt =
        Printf.ksprintf (fun text -> CoreTracing.postMessage (TraceData.LogMessage(text, false))) fmt

    /// <summary>
    /// Logs the specified string if the verbose mode is activated.
    /// </summary>
    ///
    /// <param name="fmt">The formatted message to log</param>
    let logVerbosefn fmt =
        Printf.ksprintf (if isVerbose false then log else ignore) fmt

    /// <summary>
    /// Writes a trace to the command line (in green)
    /// </summary>
    ///
    /// <param name="message">The message to log</param>
    let trace message =
        CoreTracing.postMessage (TraceData.TraceMessage(message, true))

    /// <summary>
    /// Writes a message to the command line (in green)
    /// </summary>
    ///
    /// <param name="fmt">The formatted message to log</param>
    let tracefn fmt = Printf.ksprintf trace fmt

    /// <summary>
    /// Writes a message to the command line (in green) and without a line break
    /// </summary>
    ///
    /// <param name="fmt">The formatted message to log</param>
    let tracef fmt =
        Printf.ksprintf (fun text -> CoreTracing.postMessage (TraceData.TraceMessage(text, false))) fmt

    /// <summary>
    /// Writes a trace to the command line (in green) if the verbose mode is activated.
    /// </summary>
    ///
    /// <param name="s">The message to trace</param>
    let traceVerbose s =
        if isVerbose false then
            trace s

    /// <summary>
    /// Writes a trace to stderr (in yellow)
    /// </summary>
    ///
    /// <param name="text">The text to trace</param>
    let traceImportant text =
        CoreTracing.postMessage (TraceData.ImportantMessage text)

    /// <summary>
    /// Writes a message to stderr (in yellow)
    /// </summary>
    ///
    /// <param name="fmt">The formatted message to trace</param>
    let traceImportantfn fmt = Printf.ksprintf traceImportant fmt

    /// <summary>
    /// Writes a trace to the command line (in yellow)
    /// </summary>
    ///
    /// <param name="fmt">The formatted message to trace</param>
    let traceFAKE fmt =
        Printf.ksprintf (TraceData.ImportantMessage >> CoreTracing.postMessage) fmt

    /// <summary>
    /// Traces an error (in red)
    /// </summary>
    ///
    /// <param name="error">The error message to trace</param>
    let traceError error =
        CoreTracing.postMessage (TraceData.ErrorMessage error)

    /// <summary>
    /// Writes an error message to stderr (in red)
    /// </summary>
    ///
    /// <param name="fmt">The formatted error message to trace</param>
    let traceErrorfn fmt = Printf.ksprintf traceError fmt

    open Microsoft.FSharp.Core.Printf

    /// <summary>
    /// Converts an exception and its inner exceptions to a nice string.
    /// </summary>
    ///
    /// <param name="ex">The exception to convert</param>
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

    /// <summary>
    /// Traces an exception details (in red)
    /// </summary>
    ///
    /// <param name="ex">The exception to trace</param>
    let traceException (ex: Exception) =
        exceptionAndInnersToString ex |> traceError

    /// <summary>
    /// Traces the EnvironmentVariables
    /// </summary>
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

    /// <summary>
    /// Traces a line
    /// </summary>
    let traceLine () =
        trace "---------------------------------------------------------------------"

    /// <summary>
    /// Traces a header
    /// </summary>
    ///
    /// <param name="name">The header value</param>
    let traceHeader name =
        trace ""
        traceLine ()
        trace name
        traceLine ()

    /// <summary>
    /// Puts an opening tag on the internal tag stack
    /// </summary>
    ///
    /// <param name="tag">The tag to insert</param>
    /// <param name="description">The tag description</param>
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

    /// <summary>
    /// A safe disposable type for tracing
    /// </summary>
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

    /// <summary>
    /// Removes an opening tag from the internal tag stack
    /// </summary>
    ///
    /// <param name="tag">The tag to close</param>
    let closeTagUnsafeEx status tag =
        let time =
            match openTags.Value with
            | (sw, x) :: rest when x = tag ->
                openTags.Value <- rest
                sw.Elapsed
            | _ -> failwithf "Invalid tag structure. Trying to close %A tag but stack is %A" tag openTags

        TraceData.CloseTag(tag, time, status) |> CoreTracing.postMessage

    /// <summary>
    /// Removes an opening tag from the internal tag stack
    /// </summary>
    /// 
    /// <param name="tag">The tag to insert</param>
    let closeTagUnsafe tag = closeTagUnsafeEx TagStatus.Success tag

    /// <summary>
    /// Traces a tag
    /// </summary>
    /// 
    /// <param name="tag">The tag to trace</param>
    /// <param name="description">the tag description</param>
    let traceTag tag description =
        openTagUnsafe tag description
        asSafeDisposable (fun state -> closeTagUnsafeEx state tag)

    /// <summary>
    /// Set build state with the given tag and message
    /// </summary>
    /// 
    /// <param name="tag">The tag to trace</param>
    /// <param name="message">the build message</param>
    let setBuildStateWithMessage tag message =
        TraceData.BuildState(tag, Some(message)) |> CoreTracing.postMessage

    /// <summary>
    /// Set build state with the given tag
    /// </summary>
    /// 
    /// <param name="tag">The tag to trace</param>
    let setBuildState tag =
        TraceData.BuildState(tag, None) |> CoreTracing.postMessage

    /// <summary>
    /// Set status for the given test
    /// </summary> 
    /// 
    /// <param name="testName">The test name</param>
    /// <param name="testStatus">The test status</param>
    let testStatus testName testStatus =
        // TODO: Check if the given test is opened in openTags-stack?
        TraceData.TestStatus(testName, testStatus) |> CoreTracing.postMessage

    /// <summary>
    /// Trace test output and errors
    /// </summary>
    /// 
    /// <param name="testName">The test name</param>
    /// <param name="out">The test output</param>
    /// <param name="err">The test error</param>
    let testOutput testName out err =
        // TODO: Check if the given test is opened in openTags-stack?
        TraceData.TestOutput(testName, out, err) |> CoreTracing.postMessage

    /// <summary>
    /// Publish given type in given path
    /// </summary>
    /// 
    /// <param name="typ">The type to publish</param>
    /// <param name="path">The path to publish type to</param>
    let publish typ path =
        TraceData.ImportData(typ, path) |> CoreTracing.postMessage

    /// <summary>
    /// Trace the given build number
    /// </summary>
    /// 
    /// <param name="number">The build number to trace</param>
    let setBuildNumber number =
        TraceData.BuildNumber number |> CoreTracing.postMessage

    /// <summary>
    /// Closes all opened tags
    /// </summary>
    let closeAllOpenTags () =
        Seq.iter (fun (_, tag) -> closeTagUnsafeEx TagStatus.Failed tag) openTags.Value

    /// <summary>
    /// Traces the begin of a target
    /// </summary>
    /// 
    /// <param name="name">The name of the target</param>
    /// <param name="description">The description of the target</param>
    /// <param name="dependencyString">The target dependency string</param>
    let traceStartTargetUnsafe name description (dependencyString: string) =
        openTagUnsafe (KnownTags.Target name) description

    /// <summary>
    /// Traces the begin of a final target
    /// </summary>
    /// 
    /// <param name="name">The name of the target</param>
    /// <param name="description">The description of the target</param>
    /// <param name="dependencyString">The target dependency string</param>
    let traceStartFinalTargetUnsafe name description (dependencyString: string) =
        openTagUnsafe (KnownTags.FinalTarget name) description

    /// <summary>
    /// Traces the begin of a failure target
    /// </summary>
    /// 
    /// <param name="name">The name of the target</param>
    /// <param name="description">The description of the target</param>
    /// <param name="dependencyString">The target dependency string</param>
    let traceStartFailureTargetUnsafe name description (dependencyString: string) =
        openTagUnsafe (KnownTags.FailureTarget name) description

    /// <summary>
    /// Traces the end of a target
    /// </summary>
    /// 
    /// <param name="state">The target state</param>
    /// <param name="name">The name of the target</param>
    let traceEndTargetUnsafeEx state name =
        closeTagUnsafeEx state (KnownTags.Target name)

    /// <summary>
    /// Traces the end of a final target
    /// </summary>
    /// 
    /// <param name="state">The target state</param>
    /// <param name="name">The name of the target</param>
    let traceEndFinalTargetUnsafeEx state name =
        closeTagUnsafeEx state (KnownTags.FinalTarget name)

    /// <summary>
    /// Traces the end of a failure target
    /// </summary>
    /// 
    /// <param name="state">The target state</param>
    /// <param name="name">The name of the target</param>
    let traceEndFailureTargetUnsafeEx state name =
        closeTagUnsafeEx state (KnownTags.FailureTarget name)

    /// <summary>
    /// Traces the end of a target
    /// </summary>
    /// 
    /// <param name="name">The name of the target</param>
    let traceEndTargetUnsafe name =
        traceEndTargetUnsafeEx TagStatus.Success name

    /// <summary>
    /// Traces a target
    /// </summary>
    /// 
    /// <param name="name">The name of the target</param>
    /// <param name="description">The description of the target</param>
    /// <param name="dependencyString">The target dependency string</param>
    let traceTarget name description dependencyString =
        traceStartTargetUnsafe name description dependencyString
        asSafeDisposable (fun state -> traceEndTargetUnsafeEx state name)

    /// <summary>
    /// Traces a final target
    /// </summary>
    /// 
    /// <param name="name">The name of the target</param>
    /// <param name="description">The description of the target</param>
    /// <param name="dependencyString">The target dependency string</param>
    let traceFinalTarget name description dependencyString =
        traceStartFinalTargetUnsafe name description dependencyString
        asSafeDisposable (fun state -> traceEndFinalTargetUnsafeEx state name)

    /// <summary>
    /// Traces a failed target
    /// </summary>
    /// 
    /// <param name="name">The name of the target</param>
    /// <param name="description">The description of the target</param>
    /// <param name="dependencyString">The target dependency string</param>
    let traceFailureTarget name description dependencyString =
        traceStartFailureTargetUnsafe name description dependencyString
        asSafeDisposable (fun state -> traceEndFailureTargetUnsafeEx state name)

    /// <summary>
    /// Traces the begin of a task
    /// </summary>
    /// 
    /// <param name="task">The name of the task</param>
    /// <param name="description">The description of the task</param>
    let traceStartTaskUnsafe task description =
        openTagUnsafe (KnownTags.Task task) description

    /// <summary>
    /// Traces the end of a task
    /// </summary>
    /// 
    /// <param name="state">The state of the task</param>
    /// <param name="task">The name of the task</param>
    let traceEndTaskUnsafeEx state task =
        closeTagUnsafeEx state (KnownTags.Task task)

    /// <summary>
    /// Traces the end of a task
    /// </summary>
    /// 
    /// <param name="task">The name of the task</param>
    let traceEndTaskUnsafe task =
        traceEndTaskUnsafeEx TagStatus.Success task

    /// <summary>
    /// Wrap functions in a 'use' of this function
    /// </summary>
    /// 
    /// <param name="name">The name of the task</param>
    /// <param name="description">The description of the task</param>
    let traceTask name description =
        traceStartTaskUnsafe name description
        asSafeDisposable (fun state -> traceEndTaskUnsafeEx state name)

    /// <summary>
    /// Allows automatic or manual tracing around a function being run
    /// If in automatic success mode and no exception is thrown then trace is marked as success
    /// Any exception thrown will result in a mark failed and exception re-thrown
    /// </summary>
    /// 
    /// <param name="automaticSuccess">Flag to mark trace task as success</param>
    /// <param name="func">Callback to call on result of task trace</param>
    /// <param name="trace">The trace instance</param>
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
    /// <summary>
    /// Traces the message to the console
    /// </summary>
    /// 
    /// <param name="msg">The message to log</param>
    /// <param name="eventLogEntry">The message log level</param>
    let logToConsole (msg, eventLogEntry: EventLogEntryType) =
        let safeMessage = TraceSecrets.guardMessage msg

        match eventLogEntry with
        | EventLogEntryType.Error -> TraceData.ErrorMessage safeMessage
        | EventLogEntryType.Information -> TraceData.TraceMessage(safeMessage, true)
        | EventLogEntryType.Warning -> TraceData.ImportantMessage safeMessage
        | _ -> TraceData.LogMessage(safeMessage, true)
        |> CoreTracing.defaultConsoleTraceListener.Write

    /// <summary>
    /// Logs the given files with the message.
    /// </summary>
    /// 
    /// <param name="message">The message to log</param>
    /// <param name="items">The files to log message to</param>
    let logItems message items =
        items |> Seq.iter (log << sprintf "%s%s" message)
