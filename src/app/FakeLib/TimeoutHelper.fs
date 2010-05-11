[<AutoOpen>]
module Fake.TimeoutHelper

/// Waits until the given function returns true or the timeout is reached
let waitFor f timeout (testMS:int) timeoutF = 
    let watch = new System.Diagnostics.Stopwatch()
    watch.Start()    

    while f() |> not do
        if watch.Elapsed > timeout then timeoutF()
        System.Threading.Thread.Sleep testMS

    watch.Elapsed