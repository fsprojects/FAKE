module Test.Fake.Deploy.Web.Module.ApiUser.Tests

open System
open System.IO
open Nancy
open Nancy.Testing
open Newtonsoft.Json
open Xunit
open Fake.Deploy.Web
open Fake.Deploy.Web.Data
open Test.Fake.Deploy.Web.NancyTestHelpers


let init() =
    membershipProvider.CreateUser("user1", "pass1", "user1@fsharp.org") |> ignore
    membershipProvider.CreateUser("user2", "pass2", "user2@fsharp.org") |> ignore

let rootUrl = "/api/v1/user"

let createBrowser = createBrowser<Module.ApiUser>
let get<'T> = get<Module.ApiUser, 'T>

[<Fact>]
let ``should get users`` () =
    init()
    let users = get<User[]> rootUrl
    let userId = users |> Array.map(fun e -> e.Id)
    Assert.Contains<string>("user1", userId)
    Assert.Contains<string>("user2", userId)

[<Fact>]
let ``should get user`` () =
    init()
    let user = get<User> (rootUrl + "/user1")
    Assert.Equal<string>("user1", user.Username)


[<Fact>]
let ``should save new user`` () =
    init()
    let newUser = { UserName = "newUser"; Password = "pass"; ConfirmPassword = "pass";  Email = "newUser@fake.org" }
    let browser = createBrowser()
    let userCreated = post<User, Registration> browser rootUrl newUser
    let user = get<User> (rootUrl + "/" + userCreated.Id)
    Assert.Equal<string>(userCreated.Id, user.Id)
        
[<Fact>]
let ``should remove saved user`` () =
    init()
    let newUser = { UserName = "newUser"; Password = "pass"; ConfirmPassword = "pass";  Email = "newUser@fake.org" }
    let browser = createBrowser()
    let userCreated = post<User, Registration> browser rootUrl newUser

    let response = browser.Delete (rootUrl + "/" + userCreated.Id)
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode)
    let userFound = get<User[]> rootUrl |> Seq.tryFind(fun e -> e.Id = userCreated.Id)
    Assert.True userFound.IsNone
