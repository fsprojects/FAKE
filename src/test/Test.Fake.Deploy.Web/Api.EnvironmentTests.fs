module Test.Fake.Deploy.Web.Module.ApiEnvironment.Tests

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
    dataProvider.SaveEnvironments [
                                    { Id = "e1"; Name = "env 1"; Description="d"; Agents = [] }
                                    { Id = "e2"; Name = "env 2"; Description="d"; Agents = [] }
                                    ]

let rootUrl = "/api/v1/environment"

let createBrowser = createBrowser<Module.ApiEnvironment>
let get<'T> = get<Module.ApiEnvironment, 'T>
let post = post<Environment, Environment>

[<Fact>]
let ``should get environments`` () =
    init()
    let envs = get<Environment[]> rootUrl
    let envId = envs |> Array.map(fun e -> e.Id)
    Assert.Contains<string>("e1", envId)
    Assert.Contains<string>("e2", envId)

[<Fact>]
let ``should get one environment`` () =
    init()
    let env = get<Environment> (rootUrl + "/e2")
    Assert.Equal<string>("e2", env.Id)

[<Fact>]
let ``should save one environment`` () =
    init()
    let newEnv = { Id = "whatevar, server sets it anyway"; Name = "new environment"; Description = "we should fetch this env"; Agents = [] }
    let browser = createBrowser()
    let envCreated = post browser rootUrl newEnv
    let env = get<Environment> (rootUrl + "/" + envCreated.Id)
    Assert.Equal<string>(envCreated.Id, env.Id)
        
[<Fact>]
let ``should remove saved environment`` () =
    init()
    let newEnv = { Id = "whatevar, server sets it anyway"; Name = "new environment"; Description = "we should fetch this env"; Agents = [] }
    let browser = createBrowser()
    let envCreated = post browser rootUrl newEnv

    let response = browser.Delete (rootUrl + "/" + envCreated.Id)
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode)
    let envFound = get<Environment[]> rootUrl |> Seq.tryFind(fun e -> e.Id = envCreated.Id)
    Assert.True envFound.IsNone
