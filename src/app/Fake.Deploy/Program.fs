
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

type DeployCommand = {
    Name :  string;
    Parameters : string list
    Description : string
    Function: string array -> unit
}  

module Main = 
    let registeredCommands = System.Collections.Generic.Dictionary<_,_>()
    let register command = registeredCommands.Add(command.Name.ToLower(), command)

    let printUsage() =
        printfn "---- Usage -----"

        registeredCommands        
        |> Seq.map (fun p -> sprintf "/%s %s ->\r\n\t%s" p.Value.Name (separated " " p.Value.Parameters) p.Value.Description)
        |> Seq.iter (printfn "%s\r\n")
            
        printfn "Otherwise the service is just started as a command line process"

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

    { Name = "install"
      Parameters = []
      Description = "installs the deployment agent as a service"
      Function = fun _ -> installer (fun i -> i.Install(new System.Collections.Hashtable())) }
        |> register

    { Name = "uninstall"
      Parameters = []
      Description = "uninstalls the deployment agent"
      Function = fun _ -> installer (fun i -> i.Uninstall(null)) }
        |> register

    { Name = "start"
      Parameters = []
      Description = "starts the deployment agent"
      Function = fun _ -> (getService()).Start() }
        |> register

    { Name = "stop"
      Parameters = []
      Description = "stops the deployment agent"
      Function = fun _ -> (getService()).Stop() }
        |> register

    { Name = "createFromArchive"
      Parameters = ["name"; "version"; "scriptpath"; "archive"; "output"]
      Description = "creates a Fake deployment package from the given zip and\r\n\toutputs to the given directory"
      Function = fun args -> DeploymentHelper.createDeploymentPackageFromZip args.[1] args.[2] args.[3] args.[4] args.[5] }
        |> register

    { Name = "createFromDirectory"
      Parameters = ["name"; "version"; "scriptpath"; "dir"; "output"]
      Description = "creates a Fake deployment package from the given dir and\r\n\toutputs to the given directory"
      Function = fun args -> DeploymentHelper.createDeploymentPackageFromDirectory args.[1] args.[2] args.[3] args.[4] args.[5] }
        |> register

    { Name = "deployRemote"
      Parameters = ["url"; "package"]
      Description = "pushes the deployment package to the deployment agent\r\n\tlistening on the url"
      Function = 
        fun args ->
            match DeploymentHelper.postDeploymentPackage args.[1] args.[2] with
            | Some(Choice1Of2 p ) -> printfn "%A" p
            | Some(Choice2Of2(e)) -> printfn "Deployment of %A Failed\r\n%A" args.[1] e
            | _ -> printfn "Deployment of %A Failed\r\nCould not derive reason sorry!!!" args.[1] }
        |> register

    { Name = "deploy"
      Parameters = ["package"]
      Description = "runs the deployment on the local machine (for testing purposes)"
      Function =
        fun args -> 
            match DeploymentHelper.runDeploymentFromPackage args.[1] with
            | Choice1Of2(r, p) -> printfn "Deployment of %A %s" p (if r then "Sucessful" else "Failed")
            | Choice2Of2(e) -> printfn "Deployment of %A Failed\r\n%A" args.[1] e }
        |> register

    { Name = "help"
      Parameters = []
      Description = "prints this message"
      Function = fun _ -> printUsage() }
        |> register   
     

    [<EntryPoint>]
    let main(args) =
        if args <> null && args.Length > 0 then
            match args.[0].ToLower() |> registeredCommands.TryGetValue with
            | true,cmd -> cmd.Function args
            | false,_ -> printUsage()
            0
        else 
            use srv = new FakeDeployService()
            srv.Start(args)
            Console.ReadLine() |> ignore
            0

    

