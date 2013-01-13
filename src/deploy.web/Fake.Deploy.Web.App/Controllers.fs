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
[<Authorize>]
type AdminController() = 
    inherit Controller()

    [<Authorize(Roles="Administrator")>]
    member this.Agent() = this.View() :> ActionResult

    [<Authorize(Roles="Administrator")>]
    member this.Environment() = this.View() :> ActionResult

[<HandleError>]
[<Authorize>]
type HomeController() = 
    inherit Controller()

    [<Authorize>]
    member this.Index() = this.View() :> ActionResult

    [<Authorize>]
    member this.Agent(agentId : string) = this.View("Agent", agentId |> box) :> ActionResult


type ResetPasswordQuestionAndAnswerRouteValues = {
    username : string
}

[<HandleError>]
type AccountController() =
    inherit Controller()
   
    member val FormsService = new FormsAuthenticationService() :> IFormsAuthenticationService with get, set
    member val MembershipService = new AccountMembershipService() :> IMembershipService with get, set

    [<HttpGet>]
    member this.LogOn() = this.View() :> ActionResult

    [<HttpPost>]
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
            this.RedirectToAction("Index", "Home");

        member this.Register() =
            this.View() :> ActionResult


        [<HttpPost>]
        member this.DoRegister(model : RegisterModel) =
            if this.ModelState.IsValid then
                let createStatus = this.MembershipService.CreateUser(model.UserName, model.Password, model.Email, model.PasswordQuestion, model.PasswordQuestionAnswer)
                
                if (createStatus = MembershipCreateStatus.Success)
                then
                    this.FormsService.SignIn(model.UserName, false);
                    this.RedirectToAction("Index", "Home") :> ActionResult
                else
                    this.ModelState.AddModelError("", AccountValidation.ErrorCodeToString(createStatus));
                    this.View("Register", model) :> ActionResult
            else
                this.View("Register", model) :> ActionResult

        [<Authorize>]
        member this.ChangePassword() =
            this.View() :> ActionResult
        

        [<Authorize>]
        [<HttpPost>]
        member this.DoChangePassword(model : ChangePasswordModel) =
            if (this.ModelState.IsValid)
            then
                if (this.MembershipService.ChangePassword(this.User.Identity.Name, model.OldPassword, model.NewPassword))
                then 
                    this.RedirectToAction("ChangePasswordSuccess") :> ActionResult
                else 
                    this.ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.")
                    this.View("ChangePassword", model) :> ActionResult
            else
                this.View("ChangePassword", model) :> ActionResult

        [<Authorize>]
        member this.ChangePasswordQuestionAndAnswer() =
            let user = this.MembershipService.GetUser(this.User.Identity.Name);
            let model = new ChangePasswordQuestionAndAnswerModel(user.UserName, user.PasswordQuestion)
            this.View(model) :> ActionResult
        

        [<Authorize>]
        [<HttpPost>]
        member this.DoChangePasswordQuestionAndAnswer(model : ChangePasswordQuestionAndAnswerModel) =
            if (this.ModelState.IsValid)
            then
                if (this.MembershipService.ChangePasswordQuestionAndAnswer(this.User.Identity.Name, model.Password, model.PasswordQuestion, model.PasswordQuestionAnswer))
                then this.RedirectToAction("ChangePasswordSuccess") :> ActionResult
                else
                    this.ModelState.AddModelError("", "The password is incorrect or the new question and answer are invalid.");
                    this.View("ChangePasswordQuestionAndAnswer", model) :> ActionResult
            else
                this.View("ChangePasswordQuestionAndAnswer", model) :> ActionResult

        member this.ChangePasswordSuccess() = this.View() :> ActionResult

        member this.ManageUsers() = 
            this.View(this.MembershipService.GetAllUsers()) :> ActionResult
       
        member this.EditUser(username : string) =
            let user = this.MembershipService.GetUser(username)
            let roles = this.MembershipService.GetAllRoles()
            let userRoles = this.MembershipService.GetRolesForUser(user.UserName)           
            this.View(new EditUserModel(user.UserName, user.Email, roles, userRoles)) :> ActionResult

        [<HttpPost>]
        member this.DoEditUser(model : EditUserModel) = 
            let user = this.MembershipService.GetUser(model.Username);
            user.Email <- model.Email;            
            this.MembershipService.UpdateUser(user, model.UserRoles);            
            this.RedirectToAction("ManageUsers") :> ActionResult
            
        //enablePasswordReset must be set to true in the web.config for this action
        member this.ResetPassword() = this.View() :> ActionResult

        [<HttpPost>]
        member this.DoResetPassword(model : ResetPasswordModel) =
            if this.ModelState.IsValid
            then
                let user = Membership.GetUser(model.UserName);
                if (user <> null)
                then  this.RedirectToAction("ResetPasswordQuestionAndAnswer", { username=model.UserName}) :> ActionResult
                else
                    this.ModelState.AddModelError("UserName", "Bad username.");
                    this.View("ResetPasssword", model) :> ActionResult
            else this.View("ResetPasssword", model) :> ActionResult

        member this.ResetPasswordQuestionAndAnswer(username : string) =
            let user = Membership.GetUser(username)
            this.View(new ResetPasswordQuesitonAndAnswerModel(PasswordQuestion = user.PasswordQuestion, UserName=username)) :> ActionResult
        
        [<HttpPost>]
        member this.DoResetPasswordQuestionAndAnswer(model : ResetPasswordQuesitonAndAnswerModel) = 
            try
                let newPass = this.MembershipService.ResetPassword(model.UserName, model.PasswordQuestionAnswer);             
                this.RedirectToAction("ResetPasswordSuccess") :> ActionResult
            with
                | :? MembershipPasswordException as e ->
                    this.ModelState.AddModelError("PasswordQuestionAnswer", "The answer is incorrect");
                    this.View("ResetPasswordQuestionAndAnswer", model) :> ActionResult

        member this.ResetPasswordSuccess() = this.View() :> ActionResult
