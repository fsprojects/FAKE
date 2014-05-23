module Fake.Services

open Fake
open System
open System.Diagnostics
open System.ServiceProcess
open System.Configuration

let ServiceName = "Fake Deploy Agent"

type FakeDeployService() as self = 
    inherit ServiceBase()
    let mutable nancyHost = null

    let logger = 
        if Environment.UserInteractive then TraceHelper.logToConsole
        else 
            self.EventLog.Log <- "Application"
            self.EventLog.Source <- ServiceName
            self.EventLog.WriteEntry
    
    do 
        self.AutoLog <- true
        self.ServiceName <- "Fake Deploy Agent"
    
    override x.OnStart args = 
        let serverName = 
            if args <> null && args.Length > 1 then args.[1]
            else ConfigurationManager.AppSettings.["ServerName"]
        
        let port = 
            let p =
                if args <> null && args.Length > 2 then args.[2]
                else ConfigurationManager.AppSettings.["Port"]
            let success, port' = Int32.TryParse(p)
            if success then port' else 8080
                    
        DeploymentAgent.workDir <-
            if args <> null && args.Length > 3 then args.[3]
            else ConfigurationManager.AppSettings.["WorkDirectory"]
        
        let uri = sprintf "http://%s:%i/" serverName port
        logger(sprintf "Listening on %s" uri, EventLogEntryType.Information)
        nancyHost <- DeploymentAgent.createNancyHost ([| Uri uri |])
        nancyHost.Start()
    
    override x.OnStop() =
        nancyHost.Stop()
    
    member x.Start(args) = 
        if Environment.UserInteractive then x.OnStart args
        else ServiceBase.Run x
    
    member x.Stop() = x.OnStop()

let getFakeAgentService() = 
    ServiceController.GetServices() 
    |> Array.find (fun (x : ServiceController) -> x.ServiceName = "Fake Deploy Agent")