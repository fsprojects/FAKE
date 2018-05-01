namespace Fake.Deploy.Web.File

open Fake.Deploy.Web
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type FileProvider() =

    interface IDataProvider with
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Id with get() = "File"

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.ParameterDescriptions 
            with get() = [ 
                            { ParameterName = "datafolder";
                              Description = "Path to where you want data to be stored. Ex: C:\\Data" }
                         ] |> Seq.ofList

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Initialize(settings) =
              Provider.dataFolder <- settings.["datafolder"]

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]            
        member x.GetEnvironments(ids) =
            let ids = ids |> Seq.toList
            let envs = Provider.getEnvironments()
            match ids with
            | [] -> envs
            | ag -> envs |> Array.filter(fun a -> ids |> List.exists(fun e -> e = a.Id)) |> Array.ofSeq

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.SaveEnvironments(envs) = 
            Provider.saveEnvironments envs

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.DeleteEnvironment(id) = 
            Provider.deleteEnvironment id

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.GetAgents(ids) =
            let ids = ids |> Seq.toList
            let agents = Provider.getAgents()
            match ids with
            | [] -> agents
            | ag -> agents |> Array.filter(fun a -> ids |> List.exists(fun e -> e = a.Id)) |> Array.ofSeq

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.SaveAgents(agents) = 
            Provider.saveAgents agents

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.DeleteAgent(id) = 
            Provider.deleteAgent id

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.Dispose () = ()
