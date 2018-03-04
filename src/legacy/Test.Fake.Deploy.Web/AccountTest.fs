module Test.Fake.Deploy.Web.Module.Account.Tests
open Nancy
open Nancy.Testing
open Newtonsoft.Json
open Xunit
open Fake.Deploy.Web
open Fake.Deploy.Web.Data
open Test.Fake.Deploy.Web.NancyTestHelpers


let rootUrl = "/account"
let createBrowser = createBrowser<Module.Account>
let get<'T> = get<Module.ApiAgent, 'T>


[<Fact>]
let ``should get login form`` () =
    let browser = createBrowser()
    let response = browser.Get (rootUrl + "/login")
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)

// Nancy throws an error
//[<Fact>]
//let ``should login`` () =
//    membershipProvider.CreateUser("user", "password", "user@fsharp.org") |> ignore
//    let browser = createBrowser()
//    let formData (bc : BrowserContext) =
//        bc.FormValue("UserName", "user")
//        bc.FormValue("Password", "password")
//        bc.FormValue("ReturnUrl", "/")
//    let response = browser.Post(rootUrl + "/login", formData)
//    Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode)
//    response.ShouldHaveRedirectedTo "/"
