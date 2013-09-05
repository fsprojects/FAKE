namespace Fake.Deploy.Web.File

open Fake.Deploy.Web

type FileProvider() =

    interface IDataProvider with
        member x.Id with get() = "File"

        member x.ParameterDescriptions 
            with get() = [ 
                            { ParameterName = "datafolder";
                              Description = "Path to where you want data to be stored. Ex: C:\\Data" }
                         ] |> Seq.ofList

        member x.Initialize(settings) =
              Provider.dataFolder <- settings.["datafolder"]
                
        member x.GetEnvironments(ids) =
            let ids = ids |> Seq.toList
            let envs = Provider.getEnvironments()
            match ids with
            | [] -> envs
            | ag -> envs |> Array.filter(fun a -> ids |> List.exists(fun e -> e = a.Id)) |> Array.ofSeq

        member x.SaveEnvironments(envs) = 
            Provider.saveEnvironments envs

        member x.DeleteEnvironment(id) = 
            Provider.deleteEnvironment id

        member x.GetAgents(ids) =
            let ids = ids |> Seq.toList
            let agents = Provider.getAgents()
            match ids with
            | [] -> agents
            | ag -> agents |> Array.filter(fun a -> ids |> List.exists(fun e -> e = a.Id)) |> Array.ofSeq

        member x.SaveAgents(agents) = 
            Provider.saveAgents agents

        member x.DeleteAgent(id) = 
            Provider.deleteAgent id

        member x.Dispose () = ()
