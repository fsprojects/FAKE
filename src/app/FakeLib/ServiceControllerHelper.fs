[<AutoOpen>]
/// Contains tasks which allow to control NT services.
module Fake.ServiceControllerHelpers

open System
open System.ServiceProcess
open System.Threading

/// Returns whether the given service has the given name as display or service name.
/// ## Parameters
///  - `name` - The name to check for.
///  - `s` - The service in question.
let isService name (s : ServiceController) = s.DisplayName = name || s.ServiceName = name

/// Returns sequence of services with given name.
/// ## Parameters
///  - `name` - The name of the services in question.
let getServices name = ServiceController.GetServices() |> Seq.filter (isService name)

/// Returns the first service with given name or None.
/// ## Parameters
///  - `name` - The name of the service in question.
let getService name = ServiceController.GetServices() |> Seq.tryFind (isService name)

/// Returns whether a service with the given name exists.
/// ## Parameters
///  - `name` - The name of the service in question.
let checkServiceExists name = ServiceController.GetServices() |> Seq.exists (isService name)

/// Returns status of the service with given name or fails when service is not found.
/// ## Parameters
///  - `name` - The name of the service in question.
let getServiceStatus name = 
    match getService name with
    | Some sc -> sc.Status
    | None -> 
        ServiceController.GetServices()
        |> Seq.map (fun s -> s.ServiceName)
        |> separated "\r\n"
        |> failwithf "Could not find service %s. The following services are available:\r\n%s" name

/// Starts all services with given name.
/// ## Parameters
///  - `name` - The name of the services in question.
let startService name = 
    getServices name |> Seq.iter (fun s -> 
                            if s.Status <> ServiceControllerStatus.Running then 
                                tracefn "Starting Service %s" name
                                s.Start())

/// Stops all services with given name.
/// ## Parameters
///  - `name` - The name of the services in question.
let stopService name = 
    getServices name |> Seq.iter (fun s -> 
                            if s.Status <> ServiceControllerStatus.Stopped then 
                                tracefn "Stopping Service %s" name
                                s.Stop())

/// Waits until the service with the given name has been started or fails after given timeout
/// ## Parameters
///  - `name` - The name of the service in question.
///  - `timeout` - The timespan to time out after.
let ensureServiceHasStarted name timeout = 
    let endTime = DateTime.Now.Add timeout
    while DateTime.Now <= endTime && (getServiceStatus name <> ServiceControllerStatus.Running) do
        tracefn "Waiting for %s to start (Timeout: %A)" name endTime
        Thread.Sleep 1000
    if getServiceStatus name <> ServiceControllerStatus.Running then 
        failwithf "The service %s has not been started (check the logs for errors)" name

/// Waits until the service with the given name has been stopped or fails after given timeout
/// ## Parameters
///  - `name` - The name of the service in question.
///  - `timeout` - The timespan to time out after.
let ensureServiceHasStopped name timeout = 
    let endTime = DateTime.Now.Add timeout
    let getServiceStatus name =
        try 
            getServiceStatus name
        with
        | exn -> 
            tracefn "Service %s was not found." name
            ServiceControllerStatus.Stopped

    while DateTime.Now <= endTime && (getServiceStatus name <> ServiceControllerStatus.Stopped) do
        tracefn "Waiting for %s to stop (Timeout: %A)" name endTime
        Thread.Sleep 1000
    if getServiceStatus name <> ServiceControllerStatus.Stopped then 
        failwithf "The service %s has not stopped (check the logs for errors)" name
