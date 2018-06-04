namespace Fake.Deploy.Web.RavenDb

open Fake.Deploy.Web

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type RavenDbDataProvider() =     
    interface IDataProvider with
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Id with get() = "RavenDB"

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Initialize(settings) =
              Provider.init settings.["url"]

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.ParameterDescriptions 
            with get() = [ 
                            { ParameterName = "url";
                              Description = "url to RavenDB. ex: http://localhost:8081" }
                         ] |> Seq.ofList
                
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]            
        member x.GetEnvironments(ids) =
            match ids |> Seq.toList with
            | [] -> Provider.getEnvironments()
            | a -> Provider.getEnvironment ids 

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.SaveEnvironments(envs) = 
            Provider.save envs


        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.DeleteEnvironment(id) = 
            Provider.deleteEnvironment id

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.GetAgents(ids) =
            match ids |> Seq.toList with
            | [] -> Provider.getAgents()
            | a -> Provider.getAgent ids

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.SaveAgents(agents) = 
            Provider.save agents

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.DeleteAgent(id) = 
            Provider.deleteAgent id

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Dispose() = 
            Provider.dispose()
