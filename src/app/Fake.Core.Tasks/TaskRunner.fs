/// Contains a helper which can be used to implement timeouts and retries.
module Fake.Core.TaskRunner

open Fake.Core

/// Waits until the given function returns true or the timeout is reached.
/// ## Parameters
///
///  - `f` - This function will be started.
///  - `timeout` - A System.TimeSpan representing the timeout.
///  - `testMS` - An interval at which FAKE checks if the function has succeeded.
///  - `timeoutF` - This function will be run if the timeout has been reached.
let waitFor f timeout (testMS : int) timeoutF =
    let watch = new System.Diagnostics.Stopwatch()
    watch.Start()
    while f() |> not do
        if watch.Elapsed > timeout then timeoutF()
        System.Threading.Thread.Sleep testMS
    watch.Elapsed

/// Retries the given function until a retry limit is reached or the function succeeds without exception.
/// ## Parameters
///
///  - `f` - This function will be started.
///  - `retries` - A retry limit.
let rec runWithRetries f retries =
    if retries <= 0 then f()
    else
        try
            f()
        with exn ->
            Trace.trace (sprintf "Task failed with %s" exn.Message)
            Trace.trace ("Retry.")
            runWithRetries f (retries - 1)
