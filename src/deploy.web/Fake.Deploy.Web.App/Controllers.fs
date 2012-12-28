namespace Fake.Deploy.Web.App

open System.Web.Mvc

type HomeController() = 
    inherit Controller()

    member this.Index() = this.View() :> ActionResult

type EnvironmentController() =
    inherit Controller()

    member this.Index() = 
        this.View(Model.Environments()) :> ActionResult

    member this.Edit(id : string) =
        this.View(Model.Environment id) :> ActionResult

    member this.Create() = this.View(Model.Environment null) :> ActionResult

    [<HttpPost>]
    member this.Edit(env : Model.Environment) = 
        Model.save env
        this.RedirectToAction("Index") :> ActionResult

    [<HttpPost>]
    member this.Create(env : Model.Environment) = 
        Model.save env
        this.RedirectToAction("Index") :> ActionResult

    member this.Delete(id : string) = 
        Model.DeleteEnvironment id
        this.RedirectToAction("Index") :> ActionResult

type AgentController() = 
    inherit Controller()

    member this.Index() = 
        this.View(Model.Agents()) :> ActionResult
        
    member this.Edit(id : string) =
        this.View(Model.Agent id) :> ActionResult

    member this.Create() = this.View(Model.Agent null) :> ActionResult

    [<HttpPost>]
    member this.Edit(env : Model.Agent) = 
        Model.save env
        this.RedirectToAction("Index") :> ActionResult

    [<HttpPost>]
    member this.Create(env : Model.Agent) = 
        Model.save env
        this.RedirectToAction("Index") :> ActionResult

    member this.Delete(id : string) = 
        Model.DeleteAgent id
        this.RedirectToAction("Index") :> ActionResult       
