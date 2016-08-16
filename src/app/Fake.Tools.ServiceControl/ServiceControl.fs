/// Contains functions which can be used to start other tools.
module Fake.Windows.ServiceControl

open Fake.Core
open System
open System.ServiceProcess
open System.Threading

/// Host value used for querying local services.
let private localhost = "."

/// Get friendly service name for displaying in logs
let private friendlyName host name =
    if host = localhost then Environment.MachineName else host
    |> sprintf "%s on %s" name

/// Returns whether the given service has the given name as display or service name.
/// ## Parameters
///  - `name` - The name to check for.
///  - `service` - The service in question.
let isService name (service : ServiceController) = service.DisplayName = name || service.ServiceName = name

/// Returns sequence of remote services with given name.
/// ## Parameters
///  - `host` - The hostname of the remote machine.
///  - `name` - The name of the services in question.
let getRemoteServices host name = ServiceController.GetServices(host) |> Seq.filter (isService name)

/// Returns sequence of local services with given name.
/// ## Parameters
///  - `name` - The name of the services in question.
let getServices name = getRemoteServices localhost name

/// Returns the first remote service with given name or None.
/// ## Parameters
///  - `host` - The hostname of the remote machine.
///  - `name` - The name of the service in question.
let getRemoteService host name = getRemoteServices host name |> Seq.tryPick Some

/// Returns the first local service with given name or None.
/// ## Parameters
///  - `name` - The name of the service in question.
let getService name = getRemoteService localhost name

/// Returns whether a remote service with the given name exists.
/// ## Parameters
///  - `host` - The hostname of the remote machine.
///  - `name` - The name of the service in question.
let checkRemoteServiceExists host name = getRemoteService host name |> Option.isSome

/// Returns whether a local service with the given name exists.
/// ## Parameters
///  - `name` - The name of the service in question.
let checkServiceExists name = checkRemoteServiceExists localhost name

/// Returns status of the remote service with given name or fails when service is not found.
/// ## Parameters
///  - `host` - The hostname of the remote machine.
///  - `name` - The name of the service in question.
let getRemoteServiceStatus host name =
    match getRemoteService host name with
    | Some sc -> sc.Status
    | None -> 
        ServiceController.GetServices()
        |> Seq.map (fun s -> s.ServiceName)
        |> String.separated "\r\n"
        |> failwithf "Could not find service %s. The following services are available:\r\n%s" (friendlyName host name)

/// Returns status of the local service with given name or fails when service is not found.
/// ## Parameters
///  - `name` - The name of the service in question.
let getServiceStatus name = getRemoteServiceStatus localhost name

/// Starts all remote services with given name.
/// ## Parameters
///  - `host` - The hostname of the remote machine.
///  - `name` - The name of the services in question.
let startRemoteService host name =
    getRemoteServices host name |> Seq.iter (fun s ->
                                       if s.Status <> ServiceControllerStatus.Running then
                                           Trace.tracefn "Starting Service %s" (friendlyName host name)
                                           s.Start())

/// Starts all local services with given name.
/// ## Parameters
///  - `name` - The name of the services in question.
let startService name = startRemoteService localhost name

/// Stops all services with given name.
/// ## Parameters
///  - `host` - The hostname of the remote machine.
///  - `name` - The name of the services in question.
let stopRemoteService host name =
    getRemoteServices host name |> Seq.iter (fun s ->
                                            if s.Status <> ServiceControllerStatus.Stopped then
                                                Trace.tracefn "Stopping Service %s" (friendlyName host name)
                                                s.Stop())

/// Stops all local services with given name.
/// ## Parameters
///  - `name` - The name of the services in question.
let stopService name = stopRemoteService localhost name

/// Waits until the remote service with the given name has been started or fails after given timeout
/// ## Parameters
///  - `host` - The hostname of the remote machine.
///  - `name` - The name of the service in question.
///  - `timeout` - The timespan to time out after.
let ensureRemoteServiceHasStarted host name timeout =
    let endTime = DateTime.Now.Add timeout
    while DateTime.Now <= endTime && (getRemoteServiceStatus host name <> ServiceControllerStatus.Running) do
        Trace.tracefn "Waiting for %s to start (Timeout: %A)" name timeout
        Thread.Sleep 1000
    if getRemoteServiceStatus host name <> ServiceControllerStatus.Running then
        failwithf "The service %s has not been started (check the logs for errors)" name

/// Waits until the local service with the given name has been started or fails after given timeout
/// ## Parameters
///  - `name` - The name of the service in question.
///  - `timeout` - The timespan to time out after.
let ensureServiceHasStarted name timeout =
    ensureRemoteServiceHasStarted localhost name timeout

/// Waits until the remote service with the given name has been stopped or fails after given timeout
/// ## Parameters
///  - `host` - The hostname of the remote machine.
///  - `name` - The name of the service in question.
///  - `timeout` - The timespan to time out after.
let ensureRemoteServiceHasStopped host name timeout =
    let endTime = DateTime.Now.Add timeout
    let getRemoteServiceStatus host name =
        try 
            getRemoteServiceStatus host name
        with
        | exn -> 
            Trace.tracefn "Service %s was not found." name
            ServiceControllerStatus.Stopped

    while DateTime.Now <= endTime && (getRemoteServiceStatus host name <> ServiceControllerStatus.Stopped) do
        Trace.tracefn "Waiting for %s to stop (Timeout: %A)" name timeout
        Thread.Sleep 1000
    if getRemoteServiceStatus host name <> ServiceControllerStatus.Stopped then
        failwithf "The service %s has not stopped (check the logs for errors)" name

/// Waits until the local service with the given name has been stopped or fails after given timeout
/// ## Parameters
///  - `name` - The name of the service in question.
///  - `timeout` - The timespan to time out after.
let ensureServiceHasStopped name timeout =
    ensureRemoteServiceHasStopped localhost name timeout
    
/// Sends a command to a remote windows service.
let RunRemoteService command host serviceName =
    let host, address =
        match host with
        | "." -> Environment.MachineName, ""
        | _ -> host, @"\\" + host
    Trace.tracefn "%s %s on %s" command serviceName host
    if not <| Process.directExec (fun p ->
        p.FileName <- "sc"
        p.Arguments <- sprintf @"%s %s %s" address command serviceName
        p.RedirectStandardOutput <- true
    ) then failwith "Failed to send command to service."

/// Sends a command to a local windows service.
let RunService command serviceName =
    RunRemoteService command "." serviceName

/// Stops a local windows service. Waits up to two minutes for a response.
let StopService serviceName =
    stopService serviceName
    ensureServiceHasStopped serviceName (TimeSpan.FromMinutes 2.)

/// Stops a remote windows service. Waits up to two minutes for a response.
let StopRemoteService host serviceName =
    stopRemoteService host serviceName
    ensureRemoteServiceHasStopped host serviceName (TimeSpan.FromMinutes 2.)

/// Starts a local windows service. Waits up to two minutes for a response.
let StartService serviceName =
    startService serviceName
    ensureServiceHasStarted serviceName (TimeSpan.FromMinutes 2.)

/// Starts a remote windows service. Waits up to two minutes for a response.
let StartRemoteService host serviceName =
    startRemoteService host serviceName
    ensureRemoteServiceHasStarted host serviceName (TimeSpan.FromMinutes 2.)
