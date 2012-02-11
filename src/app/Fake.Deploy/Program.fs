
open Fake
open System
open System.Diagnostics
open System.ServiceProcess
open System.ComponentModel
open System.Configuration
open System.Configuration.Install
open System.Reflection
open Fake.TraceListener

type FakeDeployService() as self =
    inherit ServiceBase()

    let console = new ConsoleTraceListener(false, colorMap) :> ITraceListener

    let logger (msg, eventLogEntry : EventLogEntryType) = 
        if Environment.UserInteractive
        then 
            match eventLogEntry with
            | EventLogEntryType.Error -> console.Write(ErrorMessage(msg))
            | EventLogEntryType.Information -> console.Write(TraceMessage(msg, true))
            | EventLogEntryType.Warning -> console.Write(ImportantMessage(msg))
            | _ -> console.Write(LogMessage(msg, true))
        else self.EventLog.WriteEntry(msg, eventLogEntry)

    do 
        self.AutoLog <- true
        self.ServiceName <- "Fake Deploy Agent"

    override x.OnStart(args) = 
        DeploymentAgent.start logger (ConfigurationManager.AppSettings.["Port"])

    override x.OnStop() = 
        DeploymentAgent.stop()

    member x.Start(args) =
        if Environment.UserInteractive
        then x.OnStart(args)
        else ServiceBase.Run(x)

    member x.Stop() = 
        x.OnStop()

[<RunInstaller(true)>]
type FakeDeployInstaller() as self = 
    inherit Installer()
     
    let processInstaller = new ServiceProcessInstaller(Account = ServiceAccount.LocalSystem)
    let serviceInstaller = 
        new ServiceInstaller(
                                DisplayName = "Fake Deploy Service Agent",
                                Description = "Allows FAKE scripts to run as a deployment",
                                ServiceName = "Fake Deploy Agent",
                                StartType = ServiceStartMode.Automatic
                            )

    do 
        self.Installers.Add(processInstaller) |> ignore
        self.Installers.Add(serviceInstaller) |> ignore

    override x.OnCommitted(savedState) = 
        base.OnCommitted(savedState)
        let sc = new ServiceController("Fake Deploy Agent")
        sc.Start()


module Main = 
    
    let printUsage() =
        [
            "---- Usage -----";
            "/install ->\r\n\tinstalls the deployment agent as a service";
            "/uninstall ->\r\n\tuninstalls the deployment agent";
            "/start ->\r\n\tstarts the deployment agent";
            "/stop ->\r\n\tstops the deployment agent";
            "/createFromArchive name version scriptpath archive output ->\r\n\tcreates a Fake deployment package from the given zip and\r\n\toutputs to the given directory";
            "/createFromDirectory name version scriptpath dir output ->\r\n\tcreates a Fake deployment package from the given dir and\r\n\toutputs to the given directory";
            "/deployRemote url package ->\r\n\tpushes the deployment package to the deployment agent\r\n\tlistening on the url";
            "/deploy package ->\r\n\truns the deployment on the local machine (for testing purposes)";
            "/help ->\r\n\tprints this message";
            "Otherwise the service is just started as a command line process" 
        ] |> List.iter (printfn "%s\r\n")
     
    let installer f = 
        let ti = new TransactedInstaller()
        let installer = new FakeDeployInstaller()
        ti.Installers.Add(installer) |> ignore
        let ctx = new InstallContext("", [|"/assemblypath=" + (Assembly.GetEntryAssembly()).Location|]) 
        ti.Context <- ctx
        f(ti)

    let getService() = 
        ServiceController.GetServices() 
        |> Array.find (fun (x : ServiceController) -> x.ServiceName = "Fake Deploy Agent") 

    [<EntryPoint>]
    let main(args) =
        if args <> null && args.Length > 0 then
            match args.[0].ToLower() with
            | "/install" ->
                installer (fun i -> i.Install(new System.Collections.Hashtable()))
            | "/uninstall" -> 
                installer (fun i -> i.Uninstall(null))
            | "/start" ->
               (getService()).Start()
            | "/stop" -> 
               (getService()).Stop()     
            | "/createfromarchive" ->
                let name, version, script, archive, output = args.[1], args.[2], args.[3], args.[4], args.[5]
                DeploymentHelper.createDeploymentPackageFromZip name version script archive output
            | "/deployremote" ->
                match DeploymentHelper.postDeploymentPackage args.[1] args.[2] with
                | Some(Choice1Of2(p)) -> 
                    Console.WriteLine(p.ToString())
                | Some(Choice2Of2(e)) ->  
                    Console.WriteLine(sprintf "Deployment of %A Failed\r\n%A" (args.[1]) e)
                | _ -> Console.WriteLine(sprintf "Deployment of %A Failed\r\nCould not derive reason sorry!!!" (args.[1]))
            | "/deploy" -> 
                match DeploymentHelper.runDeploymentFromPackage args.[1] with
                | Choice1Of2(r, p) -> 
                    Console.WriteLine(sprintf "Deployment of %s %s" (p.ToString()) (if r then "Sucessful" else "Failed"))
                | Choice2Of2(e) ->  
                    Console.WriteLine(sprintf "Deployment of %A Failed\r\n%A" (args.[1]) e)
            | "/createfromdirectory" ->
                let name, version, script, archive, output = args.[1], args.[2], args.[3], args.[4], args.[5]
                DeploymentHelper.createDeploymentPackageFromDirectory name version script archive output
            | "/help" | _ -> printUsage()
            0
        else 
            use srv = new FakeDeployService()
            srv.Start(args)
            Console.ReadLine() |> ignore
            0

    

