namespace Fake.Deploy.Web.RavenDb

open Fake.Deploy.Web

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type RavenDbDataProvider() =     
    interface IDataProvider with
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Id with get() = "RavenDB"

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Initialize(settings) =
              Provider.init settings.["url"]

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.ParameterDescriptions 
            with get() = [ 
                            { ParameterName = "url";
                              Description = "url to RavenDB. ex: http://localhost:8081" }
                         ] |> Seq.ofList
                
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]            
        member x.GetEnvironments(ids) =
            match ids |> Seq.toList with
            | [] -> Provider.getEnvironments()
            | a -> Provider.getEnvironment ids 

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.SaveEnvironments(envs) = 
            Provider.save envs


        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.DeleteEnvironment(id) = 
            Provider.deleteEnvironment id

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.GetAgents(ids) =
            match ids |> Seq.toList with
            | [] -> Provider.getAgents()
            | a -> Provider.getAgent ids

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.SaveAgents(agents) = 
            Provider.save agents

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.DeleteAgent(id) = 
            Provider.deleteAgent id

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Dispose() = 
            Provider.dispose()
