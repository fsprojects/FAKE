module Fake.Deploy.Web.Data

    open System
    open System.IO
    open System.Reflection
    open System.Configuration
    open Fake.Deploy.Web
    open System.Web
    open System.Web.Configuration

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
        lazy 
            let configFile = WebConfigurationManager.OpenWebConfiguration("~")
            let connStr = configFile.ConnectionStrings.ConnectionStrings.["Fake.Deploy"]
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
    
    let private setupConnectionStrings(info : SetupInfo, config : Configuration) =
        let section = config.GetSection("connectionStrings") :?> ConnectionStringsSection
        section.ConnectionStrings.Add(new ConnectionStringSettings("Fake.Deploy", info.DataProviderConnectionString, info.DataProvider))

    let private setupAppSettings(info : SetupInfo, config : Configuration) =
        let appSettings = config.GetSection("appSettings") :?> AppSettingsSection
        appSettings.Settings.["ApplicationInitialized"].Value <- "true"

    let configure(info : SetupInfo) =
        let configFile = WebConfigurationManager.OpenWebConfiguration("~")
        setupConnectionStrings(info ,configFile)
        setupAppSettings(info, configFile)
        configFile.Save()

    let init(info : SetupInfo) =
        InitialData.Init(info.AdministratorUserName, info.AdministratorPassword, info.AdministratorEmail, provider.Value)

    let dispose() =
        if provider.IsValueCreated
        then provider.Value.Dispose()

    let getEnvironment (id : string) = 
        provider.Value.GetEnvironments([id]) |> Seq.head

    let saveEnvironment (env : Environment) = 
        provider.Value.SaveEnvironments [env]

    let deleteEnvironment (id : string) =
        provider.Value.DeleteEnvironment id

    let getEnvironments() = 
        provider.Value.GetEnvironments([])
    
    let saveAgent (environmentId : string) (agent : Agent) = 
        let env = getEnvironment environmentId
        env.AddAgents([agent])
        saveEnvironment(env)
        provider.Value.SaveAgents([agent])

    let getAgents() = 
        provider.Value.GetAgents([])

    let deleteAgent (id : string) =
        provider.Value.DeleteAgent(id)

    let getAgent (id : string) =
        provider.Value.GetAgents [id] |> Seq.head