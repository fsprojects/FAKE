namespace Fake.Deploy.Web.Controllers

open System.IO
open System.Web
open System.Web.Mvc
open System.Web.Http
open Fake.Deploy.Web
open Fake.Deploy.Web.Model
open Fake.HttpClientHelper

[<HandleError>]
type AdminController() = 
    inherit Controller()

    member this.Agent() = this.View() :> ActionResult

    member this.Environment() = this.View() :> ActionResult

[<HandleError>]
type HomeController() = 
    inherit Controller()

    member this.Index() = this.View() :> ActionResult

    member this.Agent(agentId : string) = this.View("Agent", agentId |> box) :> ActionResult