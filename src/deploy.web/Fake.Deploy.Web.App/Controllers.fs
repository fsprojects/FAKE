namespace Fake.Deploy.Web.Controllers

open System.IO
open System.Web
open System.Web.Mvc
open System.Web.Http
open Fake.Deploy.Web
open Fake.Deploy.Web.Model
open Fake.Deploy.Web.ViewModels
open Fake.HttpClientHelper

type HomeController() = 
    inherit Controller()

    member this.Index() = this.View() :> ActionResult

    member this.Agent(agentId : string) = 
        let agent = Model.getAgent agentId
        match getAllActiveReleases (agent.Address.AbsoluteUri + "fake") with
        | QueryResult(packages) -> this.View({ Agent = agent; Packages = packages }) :> ActionResult
        | _ as a -> failwith "Unexpected result"

    member this.RegisterAgent() = this.View({ Environments = Model.getEnvironments() }) :> ActionResult

    member this.CreateEnvironment() = this.View() :> ActionResult

    [<HttpPost>]
    member this.DeployPackage(agentId : string, agentUrl : string, package : HttpPostedFileBase) =
        let routeValues = 
            let rvd = new Routing.RouteValueDictionary()
            rvd.Add("agentId", agentId)
            rvd
            
        if (package <> null && package.ContentLength > 0) 
        then
            let fileName = Path.GetFileName(package.FileName);
            let path = Path.Combine(this.Server.MapPath("~/App_Data/Package_Temp"), fileName);
            package.SaveAs(path);
            match postDeploymentPackage (agentUrl + "/fake/") path with
            | Failure(err) -> raise(err :?> System.Exception)
            | _ -> this.RedirectToAction("Agent", routeValues) :> ActionResult
        else this.RedirectToAction("Agent", routeValues) :> ActionResult

    [<HttpPost>]
    member this.SaveAgent(environmentId : string, agentName : string, agentUrl : string) =
        let agent = Model.Agent.Create(agentUrl, agentName)
        Model.Save [agent]
        let env = (Model.getEnvironment environmentId).AddAgents [agent]
        Model.Save [env]
        this.RedirectToAction("Index") :> ActionResult

    [<HttpPost>]
    member this.SaveEnvironment(environmentName : string, environmentDescription : string) =
        Model.Save [Environment.Create(environmentName, environmentDescription, [])] 
        this.RedirectToAction("Index") :> ActionResult

type EnvironmentController() =
    inherit ApiController()

    member this.Get() = Model.getEnvironments()

    member this.Post(models : seq<Model.Environment>) = Model.Save models

    member this.Delete(id : string) = Model.deleteEnvironment id     
