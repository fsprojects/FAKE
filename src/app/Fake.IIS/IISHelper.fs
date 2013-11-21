[<AutoOpen>]
module Fake.IISHelper

open Microsoft.Web.Administration
open Fake.PermissionsHelper
open Fake.ProcessHelper

let private bindApplicationPool (appPool : ApplicationPool) (app : Application) =
    app.ApplicationPoolName <- appPool.Name

let Site (name : string) (protocol : string) (binding : string) (physicalPath : string) (appPool : string) (mgr : ServerManager) =
    let mutable site = mgr.Sites.[name] 
    match (site) with
    | null -> site <- mgr.Sites.Add(name, protocol, binding, physicalPath)
    | _ -> ()
    site.ApplicationDefaults.ApplicationPoolName <- appPool
    site

let ApplicationPool (name : string) (allow32on64:bool) (runtime:string) (mgr : ServerManager) = 
    let appPool = mgr.ApplicationPools.[name]
    match (appPool) with
    | null -> 
        let pool = mgr.ApplicationPools.Add(name)
        pool.Enable32BitAppOnWin64 <- allow32on64
        pool.ManagedRuntimeVersion <- runtime
        pool
    | _ ->
        appPool.Enable32BitAppOnWin64 <- allow32on64
        appPool.ManagedRuntimeVersion <- runtime
        appPool

let Application (virtualPath : string) (physicalPath : string) (site : Site) (mgr : ServerManager) =
    let app = site.Applications.[virtualPath]
    match (app) with
    | null -> site.Applications.Add(virtualPath, physicalPath)
    | _ -> app.VirtualDirectories.[0].PhysicalPath <- physicalPath; app

let commit (mgr : ServerManager) = mgr.CommitChanges()

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

let AppCmd (command : string) = 
    System.Console.WriteLine("Applying {0} via appcmd.exe", command)
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- @"c:\windows\system32\inetsrv\appcmd.exe"
        info.Arguments <- command) (System.TimeSpan.FromSeconds(30.))
    then failwithf "AppCmd.exe %s failed." command
    ()

let UnlockSection (configPath : string) =
    requiresAdmin (fun _ -> AppCmd (sprintf "unlock config -section:%s" configPath))

let deleteSite (name : string) = 
    use mgr = new ServerManager()
    let site = mgr.Sites.[name]
    if site <> null then
        site.Delete()
        commit mgr 

let deleteApp (name : string) (site : Site) = 
    use mgr = new ServerManager()
    let app = site.Applications.[name]
    if app <> null then
        app.Delete()
        commit mgr

let deleteApplicationPool (name : string) = 
    use mgr = new ServerManager()
    let appPool = mgr.ApplicationPools.[name]
    if appPool <> null then
        appPool.Delete()
        commit mgr
