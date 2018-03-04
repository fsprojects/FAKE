namespace Fake.Deploy.Web.RavenDb

open Fake.Deploy.Web

type RavenDbDataProvider() =     
    interface IDataProvider with
        member x.Id with get() = "RavenDB"

        member x.Initialize(settings) =
              Provider.init settings.["url"]

        member x.ParameterDescriptions 
            with get() = [ 
                            { ParameterName = "url";
                              Description = "url to RavenDB. ex: http://localhost:8081" }
                         ] |> Seq.ofList
                
        member x.GetEnvironments(ids) =
            match ids |> Seq.toList with
            | [] -> Provider.getEnvironments()
            | a -> Provider.getEnvironment ids 

        member x.SaveEnvironments(envs) = 
            Provider.save envs

        member x.DeleteEnvironment(id) = 
            Provider.deleteEnvironment id

        member x.GetAgents(ids) =
            match ids |> Seq.toList with
            | [] -> Provider.getAgents()
            | a -> Provider.getAgent ids

        member x.SaveAgents(agents) = 
            Provider.save agents

        member x.DeleteAgent(id) = 
            Provider.deleteAgent id

        member x.Dispose() = 
            Provider.dispose()