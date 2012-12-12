module Fake.Services

open Fake
open System
open System.Diagnostics
open System.ServiceProcess
open System.Configuration

let ServiceName = "Fake Deploy Agent"

type FakeDeployService() as self =
    inherit ServiceBase()

    let mutable listener = HttpListenerHelper.emptyListener

    let logger = 
        if Environment.UserInteractive then 
            TraceHelper.logToConsole
        else
            self.EventLog.Log <- "Application"
            self.EventLog.Source <- ServiceName
            self.EventLog.WriteEntry


    do 
        self.AutoLog <- true
        self.ServiceName <- "Fake Deploy Agent"

    override x.OnStart args =
        let serverName =
            if args <> null && args.Length > 1 then args.[1] else 
            ConfigurationManager.AppSettings.["ServerName"]

        let port = 
            if args <> null && args.Length > 2 then args.[2] else 
            ConfigurationManager.AppSettings.["Port"]

        let workDir = 
            if args <> null && args.Length > 3 then args.[3] else
            ConfigurationManager.AppSettings.["WorkDirectory"]


        listener <- DeploymentAgent.start logger workDir serverName port

    override x.OnStop() = listener.Cancel()

    member x.Start(args) =
        if Environment.UserInteractive then 
            x.OnStart args
        else 
            ServiceBase.Run x

    member x.Stop() = 
        x.OnStop()

let getFakeAgentService() =
    ServiceController.GetServices() 
    |> Array.find (fun (x : ServiceController) -> x.ServiceName = "Fake Deploy Agent")