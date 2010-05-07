[<AutoOpen>]
module Fake.MessageHelper

open System
open System.Diagnostics
open System.IO
open System.Threading

/// Waits for other applications to create a output files
/// if the timeout is reached an exception will be raised
let WaitForMessageFiles files timeOut =
    tracefn "Waiting for message files %A (Timeout: %A)" files timeOut

    let watch = new Stopwatch()
    watch.Start()
    let fileInfos = files |> Seq.map (fun file -> new FileInfo(file)) |> Seq.toList
    let allFilesExists = Seq.forall (fun (fi:FileInfo) -> fi.Refresh(); fi.Exists)      
      
    // wait for file    
    while watch.Elapsed < timeOut && (not (allFilesExists fileInfos)) do
        Thread.Sleep 100

    let time = watch.Elapsed  
    if time > timeOut then failwith "MessageFile timeout"    
    Thread.Sleep 100   
    time
  
/// Waits for another application to create a output file
///   - if the timeout is reached an exception will be raised
let WaitForMessageFile file timeOut = WaitForMessageFiles [file] timeOut