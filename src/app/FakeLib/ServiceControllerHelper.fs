namespace Fake

[<AutoOpen>]
module ServiceControllerHelpers = 
    
    open System
    open System.ServiceProcess

    let checkServiceExists name = 
        ServiceController.GetServices() |> Seq.exists (fun s -> s.DisplayName = name) 

    let startService name =
        ServiceController.GetServices() 
        |> Seq.filter (fun s -> s.DisplayName = name)
        |> Seq.iter (fun s -> if s.Status <> ServiceControllerStatus.Running then s.Start())

    let ensureServiceHasStarted name timeout =
        let startTime = DateTime.Now 
        let endTime = startTime.Add(timeout)
        let mutable result = false
        let mutable continueLooping = true
        tracefn "Waiting for %s to start (Timeout: %A secs)" name timeout.TotalSeconds
        while DateTime.Now < endTime && continueLooping do
            
            let service = ServiceController.GetServices() 
                          |> Seq.tryFind (fun s -> s.DisplayName = name)
            result <- match service with
                      | Some(sc) -> sc.Status = ServiceControllerStatus.Running
                      | None -> failwith "Could not find service %s" name
            continueLooping <- not(result) 
            System.Threading.Thread.Sleep(1000)
        if result then ()
        else failwithf "The service %s has not been started (check the logs for errors)" name


    let ensureServiceHasStopped name timeout =
        let startTime = DateTime.Now 
        let endTime = startTime.Add(timeout)
        let mutable result = false
        let mutable continueLooping = true
        tracefn "Waiting for %s to stop (Timeout: %A secs)" name timeout.TotalSeconds
        while DateTime.Now < endTime && continueLooping do
            
            let service = ServiceController.GetServices() 
                          |> Seq.tryFind (fun s -> s.DisplayName = name)
            result <- match service with
                      | Some(sc) -> sc.Status = ServiceControllerStatus.Stopped
                      | None -> true
            continueLooping <- not(result) 
            System.Threading.Thread.Sleep(1000)

        if result then ()
        else failwithf "The service %s has not stopped (check the logs for errors)" name
