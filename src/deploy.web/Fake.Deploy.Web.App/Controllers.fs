namespace Fake.Deploy.Web.Controllers

open System
open System.IO
open System.Web
open System.Web.Mvc
open System.Web.Http
open System.Dynamic
open System.Security.Principal
open System.Web.Routing
open System.Web.Security
open System.Collections.Generic
open System.Net.Mail
open Fake.Deploy.Web
open Fake.HttpClientHelper



[<HandleError>]
type SetupController() =
    inherit Controller()

    [<System.Web.Mvc.AllowAnonymous>]
    member this.Index() = 
        if this.Request.IsAuthenticated
        then
            FormsAuthentication.SignOut() //Kick the user out
            this.RedirectToAction("Index") :> ActionResult //Remake the request now the user has been signed out
        else this.View() :> ActionResult

    [<HttpPost>]
    [<System.Web.Mvc.AllowAnonymous>]
    member this.SaveSetupInformation(info : SetupInfo) =
        Data.init(info)
        Data.saveSetupInfo info

        this.RedirectToAction("Index", "Home");
        

[<HandleError>]
[<System.Web.Mvc.Authorize(Roles="Administrator")>]
type AdminController() = 
    inherit Controller()
    
    member this.Agent() = this.View() :> ActionResult

    member this.Environment() = this.View() :> ActionResult

    member this.Users() = this.View() :> ActionResult

[<HandleError>]
type HomeController() = 
    inherit Controller()

    member this.Index() = this.View() :> ActionResult

    member this.Agent(agentId : string) = this.View("Agent", agentId |> box) :> ActionResult


type ResetPasswordQuestionAndAnswerRouteValues = {
    username : string
}

[<CLIMutable>]
type LogOnModel = {
   UserName : string
   Password : string
   RememberMe : bool
}

[<HandleError>]
type AccountController() =
    inherit Controller()
  
    [<System.Web.Mvc.AllowAnonymous>]
    member this.Login() = this.View() :> ActionResult

    [<HttpPost>]
    [<System.Web.Mvc.AllowAnonymous>]
    [<ValidateAntiForgeryToken>]
    member this.DoLogOn(model : LogOnModel, returnUrl : string) =
        if this.ModelState.IsValid
        then 
            if Data.logon model.UserName model.Password model.RememberMe
            then
                if this.Url.IsLocalUrl(returnUrl) 
                then this.Redirect(returnUrl) :> ActionResult
                else this.RedirectToAction("Index", "Home") :> ActionResult
            else 
                this.ModelState.AddModelError("", "The user name or password provided is incorrect.");
                this.View("Login", model) :> ActionResult
        else this.View("Login", model) :> ActionResult

    member this.LogOff() =
        Data.logoff()
        this.RedirectToAction("Login", "Account");