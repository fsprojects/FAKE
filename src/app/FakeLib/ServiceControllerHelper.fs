[<AutoOpen>]
module Fake.ServiceControllerHelpers

open System
open System.ServiceProcess
open System.Threading

let getServices name = ServiceController.GetServices() |> Seq.filter (fun s -> s.DisplayName = name)
let getService name = ServiceController.GetServices() |> Seq.tryFind (fun s -> s.DisplayName = name)     
let checkServiceExists name = ServiceController.GetServices() |> Seq.exists (fun s -> s.DisplayName = name) 

let startService name =
    getServices name
    |> Seq.iter (fun s -> if s.Status <> ServiceControllerStatus.Running then s.Start())

let stopService name =
    getServices name
    |> Seq.iter (fun s -> if s.Status <> ServiceControllerStatus.Stopped then s.Stop())

let ensureServiceHasStarted name timeout =
    let endTime = DateTime.Now.Add timeout
    let mutable continueLooping = true
    tracefn "Waiting for %s to start (Timeout: %A secs)" name timeout.TotalSeconds

    let checkStatus() = 
        match getService name with
        | Some sc -> sc.Status = ServiceControllerStatus.Running
        | None -> failwith "Could not find service %s" name

    while DateTime.Now < endTime && continueLooping do
        continueLooping <- not (checkStatus())
        Thread.Sleep 1000

    if continueLooping then 
        failwithf "The service %s has not been started (check the logs for errors)" name


let ensureServiceHasStopped name timeout =
    let endTime = DateTime.Now.Add timeout
    let mutable continueLooping = true
    tracefn "Waiting for %s to stop (Timeout: %A secs)" name timeout.TotalSeconds

    let checkStatus() = 
        match getService name with
        | Some sc -> sc.Status = ServiceControllerStatus.Stopped
        | None -> true

    while DateTime.Now < endTime && continueLooping do
        continueLooping <- not (checkStatus())
        Thread.Sleep 1000

    if continueLooping then 
        failwithf "The service %s has not stopped (check the logs for errors)" name