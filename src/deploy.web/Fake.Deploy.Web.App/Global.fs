namespace Fake.Deploy.Web

open System
open System.Web
open System.Web.Mvc
open System.Web.Routing
open System.Web.Http
open Newtonsoft.Json
open log4net.Config

type MapHttpActionRouteSettings = {  controller : string; action : string;  id : RouteParameter }
type MapHttpRouteSettings = { id : RouteParameter }
type FaviconRoute = { favicon : string }
type Route = { controller : string; action : string; oid : UrlParameter }

type Global() =
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
                        { controller = "Home"; action = "Index"
                          oid = UrlParameter.Optional }) |> ignore
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

    member this.Start() =
        XmlConfigurator.Configure() |> ignore
        AreaRegistration.RegisterAllAreas()
        GlobalConfiguration.Configuration.Formatters.Clear()
        GlobalConfiguration.Configuration.Formatters.Add(new JsonNetFormatter(jsonSettings))
        Global.RegisterGlobalFilters(GlobalFilters.Filters)
        Global.RegisterRoutes(RouteTable.Routes) |> ignore
        InitialData.Init()

    member this.End() = 
        Fake.Deploy.Web.Model.documentStore.Dispose();