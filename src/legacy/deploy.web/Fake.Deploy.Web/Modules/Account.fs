namespace Fake.Deploy.Web.Module
open System
open Fake.Deploy.Web
open Fake.Deploy.Web.Module.NancyOp
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms
open Nancy.Security
open System.Security.Cryptography
open System.Text

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<CLIMutable>]
type LoginModel = {
    UserName : string
    Password : string
    RememberMe : bool
    ReturnUrl : string 
    Result : string
}

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type LoginResult =
    | Success of User
    | Failure of string

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Account (userMapper : UserMapper, memberProvider : IMembershipProvider) as http =
    inherit FakeModule("/account")

    let daysToStayLoggedIn = 14.

    let Login (model : LoginModel) =
        let failMsg = "Login was unsuccessful. Please correct the errors and try again."
        match (memberProvider.Login (model.UserName, model.Password, model.RememberMe)) with
        | true -> 
            let user = (memberProvider.GetUser model.UserName).Value
            let userId = userMapper.AddUser user
            Success  user, userId
        | false -> Failure failMsg, Guid.Empty

    let Logoff p =
        memberProvider.Logout()
        userMapper.RemoveUser http.Context.CurrentUser
        http.LogoutAndRedirect("/account/login")

    do
        http.get "/login" (fun p ->
            let url = http.Request.Query ?> "returnUrl"
            let model = { UserName = ""; Password = ""; RememberMe = false; ReturnUrl = url; Result = "" }
            http.View.["login"].WithModel model)

        http.post "/login" (fun p ->
            let model = http.Bind<LoginModel>()
            match Login model with
            | Success user, id ->
                let url = if String.IsNullOrEmpty(model.ReturnUrl) then "/" else model.ReturnUrl
                let expire = if (model.RememberMe) then Nullable(DateTime.Today.AddDays daysToStayLoggedIn) else Nullable<DateTime>()
                http.LoginAndRedirect(id, expire) |> box
            | Failure s, _ -> http.View.["login"].WithModel { model with Result = s } |> box
        )

        http.get "/logoff" Logoff

        http.post "/logoff" Logoff
