module Fake.Services

open Fake
open System
open System.ServiceProcess

let ServiceName = "Fake Deploy Agent"

type FakeDeployService() as self = 
    inherit ServiceBase()
    let mutable nancyHost = null

    do 
        self.AutoLog <- true
        self.ServiceName <- "Fake Deploy Agent"
        self.EventLog.Log <- "Application"
        self.EventLog.Source <- ServiceName
        if Environment.UserInteractive then 
            Logger.initLogAsConsole ()
        else 
            Logger.initLogAsService (fun s e -> self.EventLog.WriteEntry(s, e))
    
    override x.OnStart args = 
        let serverName = 
            if args <> null && args.Length > 1 then args.[1]
            else AppConfig.ServerName
        
        let port = 
            let p =
                if args <> null && args.Length > 2 then args.[2]
                else AppConfig.Port
            let success, port' = Int32.TryParse(p)
            if success then port' else 8080
                    
        DeploymentAgent.workDir <-
            let path =
                if args <> null && args.Length > 3 then args.[3]
                else AppConfig.WorkDirectory
            match Uri.TryCreate(path, UriKind.RelativeOrAbsolute) with
            | false, _ -> failwithf "Incorrect path '%s'" path
            | true, uri when uri.IsAbsoluteUri -> path
            | true, _ ->
                let exeLocation = typedefof<FakeDeployService>.Assembly.Location
                let directory = IO.Path.GetDirectoryName(exeLocation)
                directory @@ path
        
        let uri = sprintf "http://%s:%i/" serverName port
        Logger.info "Listening on %s" uri
        Logger.info "WorkDirectory is %s" DeploymentAgent.workDir
        Logger.info "LogDirectory is %s" AppConfig.LogDirectory
        Logger.info "Authorization is %A" AppConfig.Authorization
        try
            nancyHost <- DeploymentAgent.createNancyHost ([| Uri uri |])
            nancyHost.Start()
        with
        | ex -> 
            Logger.errorEx ex "Failed to start!"
            reraise()

    override x.OnStop() =
        Logger.info "Stopping..."
        nancyHost.Stop()
    
    member x.Start(args) = 
        Logger.info "Starting..."
        if Environment.UserInteractive then x.OnStart args
        else ServiceBase.Run x
    
    member x.Stop() = 
        x.OnStop()

let getFakeAgentService() = 
    ServiceController.GetServices() 
    |> Array.find (fun (x : ServiceController) -> x.ServiceName = "Fake Deploy Agent")