namespace Fake.Deploy.Web

open System
open System.Web
open System.Web.Configuration
open System.Web.Mvc
open System.Web.Routing
open System.Web.Http
open System.Security.Principal
open Newtonsoft.Json
open log4net.Config

type MapHttpActionRouteSettings = {  controller : string; action : string;  id : RouteParameter }
type MapHttpRouteSettings = { id : RouteParameter }
type FaviconRoute = { favicon : string }
type Route = { controller : string; action : string; oid : UrlParameter }


type FakeDeployControllerFactory() = 
    inherit DefaultControllerFactory()

    override this.CreateController(requestContext : RequestContext,  controllerName) =
        if not <| Data.isInitialized()
        then 
            requestContext.RouteData.Values.["controller"] <- "Setup"
            if controllerName.ToLower() <> "setup"
            then requestContext.RouteData.Values.["action"] <- "Index"
            base.CreateController(requestContext, "Setup")
        else base.CreateController(requestContext, controllerName)

and Global() =
    inherit System.Web.HttpApplication() 

    static let v1ApiRoutes (routes:RouteCollection) = 
        routes.MapHttpRoute("PackageApi", 
                "api/v1/package/{action}/{id}", 
                { controller = "Package"; action="Rollback"; id = RouteParameter.Optional }) |> ignore 
        routes.MapHttpRoute("DefaultApi", 
                    "api/v1/{controller}/{id}", 
                    { id = RouteParameter.Optional }) |> ignore
        routes
    
    static let uiRoutes (routes:RouteCollection) =
        routes.MapRoute("Default", 
                        "{controller}/{action}/{oid}", 
                        { controller = "Home"; action = "Index"; oid = UrlParameter.Optional }) |> ignore
        routes           

    static member Version = 
        typeof<Global>.Assembly.GetName().Version.ToString()

    static member RegisterGlobalFilters(filters:GlobalFilterCollection) =
        filters.Add(new HandleErrorAttribute())
        filters.Add(new System.Web.Mvc.AuthorizeAttribute())
        

    static member RegisterRoutes(routes:RouteCollection) =
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}")
        routes.IgnoreRoute("{*favicon}", { favicon = @"(.*/)?favicon.ico(/.*)?" })

        routes
        |> v1ApiRoutes
        |> uiRoutes
    
    member this.AuthenticateRequest() = 
        if this.Request.IsAuthenticated
        then 
            Data.getUser this.User.Identity.Name
            |> Option.iter 
                (fun user -> 
                    HttpContext.Current.User <- new GenericPrincipal(this.User.Identity, user.Roles.ToArray())
                )


    member this.Start() =
        XmlConfigurator.Configure() |> ignore
        
        AreaRegistration.RegisterAllAreas()
        GlobalConfiguration.Configuration.Formatters.Clear()
        GlobalConfiguration.Configuration.Formatters.Add(new JsonNetFormatter(jsonSettings))
        Global.RegisterGlobalFilters(GlobalFilters.Filters)
        Global.RegisterRoutes(RouteTable.Routes) |> ignore
        ControllerBuilder.Current.SetControllerFactory(new FakeDeployControllerFactory())
        Data.start()

    member this.End() = 
        Fake.Deploy.Web.Data.dispose()