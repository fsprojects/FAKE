namespace Fake.Deploy.Web.Controllers

open System.IO
open System.Web
open System.Web.Mvc
open System.Web.Http
open Fake.Deploy.Web
open Fake.Deploy.Web.Model
open Fake.HttpClientHelper

[<HandleError>]
type HomeController() = 
    inherit Controller()

    member this.Index() = this.View() :> ActionResult

    member this.Agent(agentId : string) = this.View("Agent", agentId |> box) :> ActionResult

    member this.RegisterAgent() = this.View() :> ActionResult

    member this.CreateEnvironment() = this.View() :> ActionResult