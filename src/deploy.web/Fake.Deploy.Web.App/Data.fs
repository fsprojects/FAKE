module Fake.Deploy.Web.Data

    open System
    open System.IO
    open System.Configuration
    open System.Reflection
    open Fake.Deploy.Web
    open System.Web

    let providerPath = AppDomain.CurrentDomain.BaseDirectory + @"\bin"
//        let basePath = AppDomain.CurrentDomain.BaseDirectory
//        if HttpContext.Current = null
//        then 
//            Path.GetFullPath(basePath)
//        else 
//            HttpContext.Current.Server.MapPath(basePath)

    let getProviderPath(name : string) =
        Path.Combine(providerPath, name.Trim() + ".dll")

    let provider = 
        let connStr = ConfigurationManager.ConnectionStrings.["Fake.Deploy"]
        let providerAss, providerType = 
            match connStr.ProviderName.Split([|','|], StringSplitOptions.RemoveEmptyEntries) with
            | [|ty; ass|] -> ass, ty
            | _ -> failwith "Fake.Deploy connection string has an invalid providerName. Should be of form:  Type, AssemblyName"
        let assemblyPath = getProviderPath(providerAss)
        let loadedAssembly = Assembly.LoadFile(assemblyPath)
        let providerType = loadedAssembly.GetType(providerType, true)
        let instance = Activator.CreateInstance(providerType) :?> IDataProvider
        instance.Initialize("Fake.Deploy")
        instance

    let init() =
        InitialData.Init(provider)

    let dispose() =
        provider.Dispose()

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