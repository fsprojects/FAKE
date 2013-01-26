module Fake.Deploy.Web.Data

    open System
    open System.IO
    open System.Reflection
    open System.Configuration
    open Fake.Deploy.Web
    open System.Web
    open System.Web.Configuration
    open System.ComponentModel.Composition
    
    do
        if not <| Directory.Exists("bin")
        then Directory.CreateDirectory("bin") |> ignore
    
    type AppInfo = {
        DataProvider : string
        DataProviderParameters : string
        MembershipProvider : string
        MembershipProviderParameters : string
    }

    type Configuration internal() =
        
        let mutable membership : IMembershipProvider = Unchecked.defaultof<_> 
        let mutable data : IDataProvider = Unchecked.defaultof<_> 

        [<ImportMany>]
        member val DataProviders : seq<IDataProvider> = Seq.empty with get, set 
        [<ImportMany>]
        member val MembershipProviders : seq<IMembershipProvider> = Seq.empty with get, set

        member x.Membership with get() = membership
        member x.Data with get() = data

        member x.SetMembershipProvider(id : string) =
            x.MembershipProviders 
            |> Seq.tryFind (fun x -> x.Id.ToLower() = id.ToLower()) 
            |> function
               | Some(provider) -> membership <- provider
               | None -> failwithf "Could not find membership provider %s" id

        member x.SetDataProvider(id : string) =
            x.DataProviders 
            |> Seq.tryFind (fun x -> x.Id.ToLower() = id.ToLower()) 
            |> function
               | Some(provider) -> data <- provider
               | None -> failwithf "Could not find data provider %s" id

        interface IDisposable with
            member x.Dispose() = 
                if box(x.Data) <> null then x.Data.Dispose()
                if box(x.Membership) <> null then x.Membership.Dispose()
               
    let private config = new Configuration()
    let private started = ref false

    let private container = 
       
        let catalog = new Hosting.AggregateCatalog()
        catalog.Catalogs.Add(new Hosting.DirectoryCatalog("bin"))

        new Hosting.CompositionContainer(catalog)
    
    let private parametersToMap (str : string) = 
        str.Split([|';'|], StringSplitOptions.RemoveEmptyEntries)
        |> Seq.map (fun s -> 
                        match s.Split([|'='|], StringSplitOptions.None) with
                        | [|key;value|] -> key.ToLower(), value
                        | _ -> failwithf "Unable to parse parameter %s parameters should be seperated with a ; and key value pairs seperated with =" s
                   )
        |> dict

    let setupInfoPath = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data"), "SetupInfo.json")

    let isInitialized() = 
        File.Exists(setupInfoPath)

    let saveSetupInfo(info : SetupInfo) =
        let info = 
            { 
                DataProvider = info.DataProvider;
                DataProviderParameters = info.DataProviderParameters
                MembershipProvider = info.MembershipProvider
                MembershipProviderParameters = info.MembershipProviderParameters
            }
        File.WriteAllText(setupInfoPath, Newtonsoft.Json.JsonConvert.SerializeObject(info))

    let private doInit(info : AppInfo) = 
        container.SatisfyImportsOnce(config) |> ignore

        config.SetDataProvider(info.DataProvider)
        config.Data.Initialize(parametersToMap info.DataProviderParameters)
        config.SetMembershipProvider(info.MembershipProvider)
        config.Membership.Initialize(parametersToMap info.MembershipProviderParameters)
        started := true

    let init(info : SetupInfo) =
        let appInfo = 
            { 
                DataProvider = info.DataProvider;
                DataProviderParameters = info.DataProviderParameters
                MembershipProvider = info.MembershipProvider
                MembershipProviderParameters = info.MembershipProviderParameters
            }

        doInit appInfo
        InitialData.Init(info.AdministratorUserName, info.AdministratorPassword, info.AdministratorEmail, config.Data, config.Membership)
    
    let start() = 
        if (not <| !started) && isInitialized()
        then 
            let si = Newtonsoft.Json.JsonConvert.DeserializeObject<AppInfo>( File.ReadAllText(setupInfoPath))
            doInit si

    let dispose() =
        (config :> IDisposable).Dispose()

    let getEnvironment (id : string) = 
        config.Data.GetEnvironments([id]) |> Seq.head

    let saveEnvironment (env : Environment) = 
        config.Data.SaveEnvironments [env]

    let deleteEnvironment (id : string) =
        config.Data.DeleteEnvironment id

    let getEnvironments() = 
        config.Data.GetEnvironments([])
    
    let saveAgent (environmentId : string) (agent : Agent) = 
        let env = getEnvironment environmentId
        env.AddAgents([agent])
        saveEnvironment(env)
        config.Data.SaveAgents([agent])

    let getAgents() = 
        config.Data.GetAgents([])

    let deleteAgent (id : string) =
        config.Data.DeleteAgent(id)

    let getAgent (id : string) =
        config.Data.GetAgents [id] |> Seq.head

    let logon username password rememberMe = 
        config.Membership.Login(username, password, rememberMe)

    let logoff() = 
        config.Membership.Logout()

    let registerUser username password email = 
        config.Membership.CreateUser(username, password, email)

    let deleteUser id =
        config.Membership.DeleteUser id

    let getUser id = 
        config.Membership.GetUser id

    let addUserToRole user role = 
        config.Membership.AddUserToRoles(user,role)

    let removeUserFromRole user role = 
        config.Membership.RemoveUserFromRoles(user, role)