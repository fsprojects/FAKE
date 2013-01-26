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
open RavenDBMembership
open RavenDBMembership.Provider
open RavenDBMembership.MVC.Models
open System.Net.Mail
open Fake.Deploy.Web
open Fake.Deploy.Web.Model
open Fake.HttpClientHelper

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

[<HandleError>]
type AccountController() =
    inherit Controller()
   
    member val FormsService = new FormsAuthenticationService() :> IFormsAuthenticationService with get, set
    member val MembershipService = new AccountMembershipService() :> IMembershipService with get, set

    [<System.Web.Mvc.AllowAnonymous>]
    member this.LogOn() = this.View() :> ActionResult

    [<HttpPost>]
    [<System.Web.Mvc.AllowAnonymous>]
    member this.DoLogOn(model : LogOnModel, returnUrl : string) =
        if this.ModelState.IsValid
        then 
            if this.MembershipService.ValidateUser(model.UserName, model.Password)
            then
                this.FormsService.SignIn(model.UserName, model.RememberMe);
                if this.Url.IsLocalUrl(returnUrl) 
                then this.Redirect(returnUrl) :> ActionResult
                else this.RedirectToAction("Index", "Home") :> ActionResult
            else 
                this.ModelState.AddModelError("", "The user name or password provided is incorrect.");
                this.View("LogOn", model) :> ActionResult
        else this.View("LogOn", model) :> ActionResult

    member this.LogOff() =
        this.FormsService.SignOut();
        this.RedirectToAction("LogOn", "Account");