[<AutoOpen>]
module Fake.ServiceControllerHelpers

open System
open System.ServiceProcess
open System.Threading

let getServices name = ServiceController.GetServices() |> Seq.filter (fun s -> s.DisplayName = name)
let getService name = ServiceController.GetServices() |> Seq.tryFind (fun s -> s.DisplayName = name)     
let checkServiceExists name = ServiceController.GetServices() |> Seq.exists (fun s -> s.DisplayName = name)
let getServiceStatus name =
    match getService name with
    | Some sc -> sc.Refresh(); sc.Status
    | None -> failwithf "Could not find service %s" name


let startService name =
    getServices name
    |> Seq.iter (fun s -> if s.Status <> ServiceControllerStatus.Running then s.Start())

let stopService name =
    getServices name
    |> Seq.iter (fun s -> if s.Status <> ServiceControllerStatus.Stopped then s.Stop())

let ensureServiceHasStarted name timeout =
    let endTime = DateTime.Now.Add timeout
    tracefn "Waiting for %s to start (Timeout: %A secs)" name timeout.TotalSeconds

    while DateTime.Now < endTime && (getServiceStatus name <> ServiceControllerStatus.Running) do
        Thread.Sleep 1000

    if getServiceStatus name <> ServiceControllerStatus.Running then 
        failwithf "The service %s has not been started (check the logs for errors)" name


let ensureServiceHasStopped name timeout =
    let endTime = DateTime.Now.Add timeout
    tracefn "Waiting for %s to stop (Timeout: %A secs)" name timeout.TotalSeconds

    while DateTime.Now < endTime && (getServiceStatus name <> ServiceControllerStatus.Stopped) do
        Thread.Sleep 1000

    if getServiceStatus name <> ServiceControllerStatus.Stopped then 
        failwithf "The service %s has not stopped (check the logs for errors)" name