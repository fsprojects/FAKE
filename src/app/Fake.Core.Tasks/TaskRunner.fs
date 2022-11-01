namespace Fake.Core

open Fake.Core

/// <summary>
/// Contains a helper which can be used to implement timeouts and retries.
/// </summary>
module TaskRunner =

    /// <summary>
    /// Waits until the given function returns true or the timeout is reached.
    /// </summary>
    ///
    /// <param name="f">This function will be started.</param>
    /// <param name="timeout">A System.TimeSpan representing the timeout.</param>
    /// <param name="testMS">An interval at which FAKE checks if the function has succeeded.</param>
    /// <param name="timeoutF">This function will be run if the timeout has been reached.</param>
    let waitFor f timeout (testMS: int) timeoutF =
        let watch = System.Diagnostics.Stopwatch()
        watch.Start()

        while f () |> not do
            if watch.Elapsed > timeout then
                timeoutF ()

            System.Threading.Thread.Sleep testMS

        watch.Elapsed

    /// <summary>
    /// Retries the given function until a retry limit is reached or the function succeeds without exception.
    /// </summary>
    ///
    /// <param name="f">This function will be started.</param>
    /// <param name="retries">A retry limit.</param>
    let rec runWithRetries f retries =
        if retries <= 0 then
            f ()
        else
            try
                f ()
            with exn ->
                Trace.trace (sprintf "Task failed with %s" exn.Message)
                Trace.trace "Retry."
                runWithRetries f (retries - 1)
