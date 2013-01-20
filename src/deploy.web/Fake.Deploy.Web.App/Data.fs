module Fake.Deploy.Web.Data

    open System
    open System.IO
    open System.Configuration
    open System.Reflection
    open Fake.Deploy.Web

    let getProviderPath(name) = 
        Path.Combine("DataProviders", name + ".dll")

    let provider = 
        let connStr = ConfigurationManager.ConnectionStrings.["Fake.Deploy"]
        let providerAss, providerType = 
            match connStr.ProviderName.Split([|','|], StringSplitOptions.RemoveEmptyEntries) with
            | [|ass; ty|] -> ass, ty
            | _ -> failwith "Fake.Deploy connection string has an invalid providerName. Should be of form: (Assmbly.Path, ProviderType)"
        let loadedAssembly = Assembly.LoadFile(getProviderPath(providerAss))
        let providerType = loadedAssembly.GetType(providerType, true)
        let instance = Activator.CreateInstance(providerType) :?> IDataProvider

    let init() =
        InitialData.Init(provider)

    let getEnvironment (id : string) = 
        provider.GetEnvironments([id]) |> Seq.head

    let saveEnvironment (env : Environment) = 
        provider.SaveEnvironments [env]

    let deleteEnvironment (id : string) =
        provider.DeleteEnvironment id

    let getEnvironments() = 
        provider.GetEnvironments([])
    
    let saveAgent (environmentId : string) (agent : Agent) = 
        let env = getEnvironment environmentId
        env.AddAgents([agent])
        saveEnvironment(env)
        provider.SaveAgents([agent])

    let getAgents() = 
        provider.GetAgents([])

    let deleteAgent (id : string) =
        provider.DeleteAgent(id)

    let getAgent (id : string) =
        provider.GetAgents [id] |> Seq.head


