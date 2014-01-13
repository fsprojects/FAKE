[<AutoOpen>]
/// Contains a helper which can be used to implement timeouts and retries.
module Fake.TaskRunnerHelper

/// Waits until the given function returns true or the timeout is reached.
/// ## Parameters
///
///  - `f` - This function will be started.
///  - `timeout` - A System.TimeSpan representing the timeout.
///  - `testMS` - An interval at which FAKE checks if the function has succeeded.
///  - `timeoutF` - This function will be run if the timeout has been reached.
let waitFor f timeout (testMS:int) timeoutF = 
    let watch = new System.Diagnostics.Stopwatch()
    watch.Start()    

    while f() |> not do
        if watch.Elapsed > timeout then timeoutF()
        System.Threading.Thread.Sleep testMS

    watch.Elapsed