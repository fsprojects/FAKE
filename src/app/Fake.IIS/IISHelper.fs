[<AutoOpen>]
module Fake.IISHelper

    open Microsoft.Web.Administration
    open Fake.PermissionsHelper

    let private bindApplicationPool (appPool : ApplicationPool) (app : Application) =
        app.ApplicationPoolName <- appPool.Name

    let Site (name : string) (protocol : string) (binding : string) (physicalPath : string) (mgr : ServerManager) = 
        mgr.Sites.Add(name, protocol, binding, physicalPath)

    let ApplicationPool (name : string) (mgr : ServerManager) = 
        mgr.ApplicationPools.Add(name)

    let Application (virtualPath : string) (physicalPath : string) (site : Site) (mgr : ServerManager) =
        site.Applications.Add(virtualPath, physicalPath)

    let commit (mgr : ServerManager) = mgr.CommitChanges();

    let IIS (site : ServerManager -> Site) 
            (appPool : ServerManager -> ApplicationPool) 
            (app : (Site -> ServerManager -> Application) option) =
        use mgr = new ServerManager()
        requiresAdmin (fun _ -> 
                            match app with
                            | Some(app) -> bindApplicationPool (appPool mgr) (app (site mgr) mgr); 
                            | None -> (site mgr).Applications.[0].ApplicationPoolName <- (appPool mgr).Name
                            commit mgr
                      )
        
        

