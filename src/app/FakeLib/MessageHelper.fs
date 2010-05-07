[<AutoOpen>]
module Fake.MessageHelper

/// Waits for other applications to create a output files
/// if the timeout is reached an exception will be raised
let WaitForMessageFiles files timeOut =
    let files = Seq.cache files
    tracefn "Waiting for message files %A (Timeout: %A)" files timeOut

    let watch = new System.Diagnostics.Stopwatch()
    watch.Start()    

    while allFilesExist files |> not do
        if watch.Elapsed > timeOut then failwith "MessageFile timeout" 
        System.Threading.Thread.Sleep 100

    watch.Elapsed    
  
/// Waits for another application to create a output file
///   - if the timeout is reached an exception will be raised
let WaitForMessageFile file timeOut = WaitForMessageFiles [file] timeOut