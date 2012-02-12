
open Fake

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

    { Name = "install"
      Parameters = []
      Description = "installs the deployment agent as a service"
      Function = fun _ -> Installers.getInstaller().Install(new System.Collections.Hashtable()) }
        |> register

    { Name = "uninstall"
      Parameters = []
      Description = "uninstalls the deployment agent"
      Function = fun _ -> Installers.getInstaller().Uninstall(null) }
        |> register

    { Name = "start"
      Parameters = []
      Description = "starts the deployment agent"
      Function = fun _ -> Services.getFakeAgentService().Start() }
        |> register

    { Name = "stop"
      Parameters = []
      Description = "stops the deployment agent"
      Function = fun _ -> Services.getFakeAgentService().Stop() }
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
        else 
            use srv = new Services.FakeDeployService()
            srv.Start(args)
            System.Console.ReadLine() |> ignore

        0
