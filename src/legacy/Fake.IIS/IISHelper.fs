[<AutoOpen>]
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.IISHelper

open Microsoft.Web.Administration
open Fake.PermissionsHelper
open Fake.ProcessHelper

let private bindApplicationPool (appPool : ApplicationPool) (app : Application) =
    app.ApplicationPoolName <- appPool.Name

let private doWithManager (f : ServerManager->unit) (mgr : ServerManager option) =
    match mgr with
    | Some m -> f m
    | None ->
        let m = new ServerManager()
        f m
        m.CommitChanges()

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let SetPhysicalPath (virtualPath : string) physicalPath (siteName : string) (manager : ServerManager option) =
    doWithManager (fun m ->
        let site = m.Sites.[siteName]
        let app = site.Applications.[virtualPath]
        let virtDir = app.VirtualDirectories.[virtualPath]
        virtDir.PhysicalPath <- physicalPath
    ) manager

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let RemoveBindingFromSite bindingInformation bindingProtocol (siteName : string) (manager : ServerManager option) =
    doWithManager (fun m ->
        let site = m.Sites.[siteName]
        match site.Bindings |> Seq.tryFind( fun b -> b.Protocol = bindingProtocol && b.BindingInformation = bindingInformation) with    
        | Some b -> site.Bindings.Remove b
        | None -> ()
    ) manager

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let  AddBindingToSite (bindingInformation : string) (bindingProtocol : string) (siteName : string) (manager : ServerManager option) =
    doWithManager (fun m ->
        let site = m.Sites.[siteName]
        match site.Bindings |> Seq.exists( fun b -> b.Protocol = bindingProtocol && b.BindingInformation = bindingInformation) with
        | false -> site.Bindings.Add(bindingInformation, bindingProtocol) |> ignore
        | true -> ()
    ) manager

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let commit (mgr : ServerManager) = mgr.CommitChanges()

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type ISiteConfig = interface
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    abstract name : string
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    abstract binding : string
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    abstract physicalPath : string
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    abstract appPool : string
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    abstract id : int64 option
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    abstract protocol : string
end

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type SiteConfig(name : string, binding:string, physicalPath:string, appPool:string, ?id: int64, ?protocol:string) = class
    interface ISiteConfig with 
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member this.name = name
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member this.binding = binding
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member this.physicalPath = physicalPath
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member this.appPool = appPool
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member this.id = id
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member this.protocol = defaultArg protocol "http"
end

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type ApplicationPoolConfig(name : string, ?runtime:string, ?allow32on64:bool, ?identity : ProcessModelIdentityType, ?credentials: string * string) = class
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.name = name
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.runtime = defaultArg runtime "v4.0"
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.allow32on64 = defaultArg allow32on64 false
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.identity = defaultArg identity ProcessModelIdentityType.ApplicationPoolIdentity
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.credentials = defaultArg credentials ("","")
end

let private MergeAppPoolProperties (appPool:ApplicationPool)(config:ApplicationPoolConfig) = 
    appPool.Enable32BitAppOnWin64 <- config.allow32on64
    appPool.ManagedRuntimeVersion <- config.runtime
    appPool.ProcessModel.IdentityType <- config.identity

    let (userName, password) = config.credentials
    appPool.ProcessModel.UserName <- userName
    appPool.ProcessModel.Password <- password
    appPool

let private MergeSiteProperties(site:Site)(config:ISiteConfig) = 
    site.ApplicationDefaults.ApplicationPoolName <- config.appPool
    match (config.id) with
    | Some id -> site.Id <- id
    | None -> ()
    site

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Site (config:ISiteConfig) (mgr : ServerManager) =
    let mutable site = mgr.Sites.[config.name]
    match (site) with
    | null -> site <- mgr.Sites.Add(config.name, config.protocol, config.binding, config.physicalPath)
    | _ -> 
        SetPhysicalPath "/" config.physicalPath config.name (Some mgr)
        AddBindingToSite config.binding config.protocol config.name (Some mgr)
    MergeSiteProperties site config

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let ApplicationPool (config: ApplicationPoolConfig) (mgr : ServerManager) = 
    let appPool = mgr.ApplicationPools.[config.name]
    match (appPool) with
    | null -> 
        let pool = mgr.ApplicationPools.Add(config.name)
        MergeAppPoolProperties pool config
    | _ ->
        MergeAppPoolProperties appPool config

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Application (virtualPath : string) (physicalPath : string) (site : Site) (mgr : ServerManager) =
    let app = site.Applications.[virtualPath]
    match (app) with
    | null -> site.Applications.Add(virtualPath, physicalPath)
    | _ -> app.VirtualDirectories.[0].PhysicalPath <- physicalPath; app

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let IIS (site : ServerManager -> Site) 
        (appPool : ServerManager -> ApplicationPool) 
        (app : (Site -> ServerManager -> Application) option) =
    use mgr = new ServerManager()
    requiresAdmin (fun _ -> 
        match app with
        | Some(app) -> bindApplicationPool (appPool mgr) (app (site mgr) mgr)
        | None -> bindApplicationPool (appPool mgr) (site mgr).Applications.[0]
        commit mgr
    )

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let AppCmd (command : string) = 
    System.Console.WriteLine("Applying {0} via appcmd.exe", command)
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- @"c:\windows\system32\inetsrv\appcmd.exe"
        info.Arguments <- command) (System.TimeSpan.FromSeconds(30.))
    then failwithf "AppCmd.exe %s failed." command
    ()

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let UnlockSection (configPath : string) =
    requiresAdmin (fun _ -> AppCmd (sprintf "unlock config -section:%s" configPath))

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let deleteSite (name : string) = 
    use mgr = new ServerManager()
    let site = mgr.Sites.[name]
    if site <> null then
        site.Delete()
        commit mgr 

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let deleteApp (name : string) (site : Site) = 
    use mgr = new ServerManager()
    let app = site.Applications.[name]
    if app <> null then
        app.Delete()
        commit mgr

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let deleteApplicationPool (name : string) = 
    use mgr = new ServerManager()
    let appPool = mgr.ApplicationPools.[name]
    if appPool <> null then
        appPool.Delete()
        commit mgr
