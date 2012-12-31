namespace Fake.Deploy.Web.Controllers

open System.Web.Mvc
open System.Web.Http
open Fake.Deploy.Web

type HomeController() = 
    inherit Controller()

    member this.Index() = this.View() :> ActionResult

type EnvironmentController() =
    inherit ApiController()

    member this.Get() = Model.Environments()

    member this.Post(models : seq<Model.Environment>) = Model.Save models

    member this.Delete(id : string) = Model.DeleteEnvironment id     
