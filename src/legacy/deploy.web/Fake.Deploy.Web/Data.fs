module Fake.Deploy.Web.Data

    open System
    open System.IO
    open System.Reflection
    open System.Configuration
    open Fake.Deploy.Web
    open System.Web
    open System.Web.Configuration
    open System.ComponentModel.Composition
    open Newtonsoft.Json

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let appdata =
        let dir =
            match HttpContext.Current <> null with
            | true -> DirectoryInfo(HttpContext.Current.Server.MapPath("~/App_Data"))
            | false  -> 
                Reflection.Assembly.GetExecutingAssembly()
                |> fun asm -> Uri(asm.CodeBase).AbsolutePath
                |> Path.GetDirectoryName
                |> fun p -> Path.Combine(p, "App_Data")
                |> fun p -> DirectoryInfo p
        if not <| dir.Exists then dir.Create()
        dir
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let providerPath = Path.Combine(appdata.FullName, "Providers\\")

    do
        Directory.CreateDirectory(providerPath) |> ignore
    
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    type AppInfo = {
        DataProvider : string
        DataProviderParameters : string
        MembershipProvider : string
        MembershipProviderParameters : string
        UseFileUpload : bool
        UseNuGetFeedUpload : bool
        NuGetFeeds : seq<Uri>
    }
    
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    type Configuration () =
        
        let mutable membership : IMembershipProvider = Unchecked.defaultof<_>
        let mutable data : IDataProvider = Unchecked.defaultof<_>

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        [<ImportMany>]
        member val DataProviders : seq<IDataProvider> = Seq.empty with get, set 
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        [<ImportMany>]
        member val MembershipProviders : seq<IMembershipProvider> = Seq.empty with get, set

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Membership with get() = membership
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.Data with get() = data
        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.IsConfigured with get() = data <> Unchecked.defaultof<_> && membership <> Unchecked.defaultof<_>

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.SetMembershipProvider(id : string) =
            x.MembershipProviders 
            |> Seq.tryFind (fun x -> x.Id.ToLower() = id.ToLower()) 
            |> function
               | Some(provider) -> membership <- provider
               | None -> failwithf "Could not find membership provider %s" id

        [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
        member x.SetDataProvider(id : string) =
            x.DataProviders 
            |> Seq.tryFind (fun x -> x.Id.ToLower() = id.ToLower()) 
            |> function
               | Some(provider) -> data <- provider
               | None -> failwithf "Could not find data provider %s" id

        interface IDisposable with
            [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
            member x.Dispose() = 
                if box(x.Data) <> null then x.Data.Dispose()
                if box(x.Membership) <> null then x.Membership.Dispose()
               
    let private started = ref false

    let private container = 
        let catalog = new Hosting.AggregateCatalog()
        catalog.Catalogs.Add(new Hosting.DirectoryCatalog(providerPath))
        new Hosting.CompositionContainer(catalog)
    
    let private parametersToMap (str : string) = 
        str.Split([|';'|], StringSplitOptions.RemoveEmptyEntries)
        |> Seq.map (fun s -> 
                        match s.Split([|'='|], StringSplitOptions.None) with
                        | [|key;value|] -> key.ToLower(), value
                        | _ -> failwithf "Unable to parse parameter %s parameters should be seperated with a ; and key value pairs seperated with =" s
                   )
        |> dict

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let setupInfoPath = Path.Combine(appdata.FullName, "SetupInfo.json")

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let isInitialized() = 
        File.Exists(setupInfoPath)

    let private CreateAppInfo (info : SetupInfo) =
            { 
                DataProvider = info.DataProvider;
                DataProviderParameters = info.DataProviderParameters
                MembershipProvider = info.MembershipProvider
                MembershipProviderParameters = info.MembershipProviderParameters
                UseFileUpload = info.UseFileUpload
                UseNuGetFeedUpload = info.UseNuGetFeedUpload
                NuGetFeeds = if info.UseNuGetFeedUpload then info.NuGetFeeds |> List.ofSeq else []
            }

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let saveSetupInfo(info : SetupInfo) =
        let info = CreateAppInfo info
        File.WriteAllText(setupInfoPath, JsonConvert.SerializeObject(info, Formatting.Indented))

    let private doInit(config : Configuration) (info : AppInfo) = 
        config.SetDataProvider(info.DataProvider)
        config.Data.Initialize(parametersToMap info.DataProviderParameters)
        config.SetMembershipProvider(info.MembershipProvider)
        config.Membership.Initialize(parametersToMap info.MembershipProviderParameters)
        started := true

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let init(config : Configuration) (info : SetupInfo) =
        let appInfo = CreateAppInfo info
        doInit config appInfo
        InitialData.Init(info.AdministratorUserName, info.AdministratorPassword, info.AdministratorEmail, config.Data, config.Membership)
    
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let start(config : Configuration) = 
        if (not <| !started) && isInitialized()
        then 
            let si = Newtonsoft.Json.JsonConvert.DeserializeObject<AppInfo>(File.ReadAllText(setupInfoPath))
            container.SatisfyImportsOnce(config) |> ignore
            doInit config si
        else 
            //Unzip bundles and copy to Providers
//            let bundles = HttpContext.Current.Server.MapPath("Bundles")
//            for bundle in Directory.GetDirectories(bundles) do
//                Fake.ZipHelper.Unzip providerPath bundle
            
            container.SatisfyImportsOnce(config) |> ignore
