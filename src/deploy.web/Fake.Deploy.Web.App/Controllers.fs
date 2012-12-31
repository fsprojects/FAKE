namespace Fake.Deploy.Web.Controllers

open System.Web.Mvc
open System.Web.Http
open Fake.Deploy.Web
open Fake.Deploy.Web.Model
open Fake.Deploy.Web.ViewModels

type HomeController() = 
    inherit Controller()

    member this.Index() = this.View() :> ActionResult

    member this.RegisterAgent() = this.View({ Environments = Model.getEnvironments() }) :> ActionResult

    member this.CreateEnvironment() = this.View() :> ActionResult

    [<HttpPost>]
    member this.SaveAgent(environmentId : string, agentName : string, agentUrl : string) =
        let env = (Model.getEnvironment environmentId).AddAgents [Model.Agent.Create(agentUrl, agentName)]
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
