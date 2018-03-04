module Test.Fake.Deploy.Web.Module.ApiAgent.Tests
open System
open System.IO
open Nancy
open Nancy.Testing
open Newtonsoft.Json
open Xunit
open Fake.Deploy.Web
open Fake.Deploy.Web.Data
open Test.Fake.Deploy.Web.NancyTestHelpers
open Test.Fake.Deploy.Web


let init() =
    dataProvider.SaveAgents [
                                { Id = "a1"; Name = "orange"; Address=Uri("http://a.se"); EnvironmentId = "agentEnv1" }
                                { Id = "a2"; Name = "blue"; Address=Uri("http://b.se"); EnvironmentId = "agentEnv1" }
                            ]
    dataProvider.SaveEnvironments [ 
                                    { Id="agentEnv1"; Name = "env 1"; Description = ""; Agents = [] }
                                  ]

let rootUrl = "/api/v1/agent"
let createBrowser = createBrowser<Module.ApiAgent>
let get<'T> = get<Module.ApiAgent, 'T>
let post = post<Agent, Agent>


[<Fact>]
let ``should get agents`` () =
    init()
    let envs = get<Agent[]> rootUrl
    let envId = envs |> Array.map(fun e -> e.Id)
    Assert.Contains<string>("a1", envId)
    Assert.Contains<string>("a2", envId)

[<Fact>]
let ``should get one agent`` () =
    init()
    let env = get<Agent> (rootUrl + "/a2")
    Assert.Equal<string>("blue", env.Name)

[<Fact>]
let ``should save one agent`` () =
    init()
    let newAgent = { Id = "set by the server"; Name = "new environment"; Address = Uri("http://c.se"); EnvironmentId = "agentEnv1" }
    let browser = createBrowser()
    let agentCreated = post browser rootUrl newAgent
    let env = get<Environment> (rootUrl + "/" + agentCreated.Id)
    Assert.Equal<string>(agentCreated.Id, env.Id)
        
[<Fact>]
let ``should remove saved agent`` () =
    init()
    let newAgent = { Id = "set by the server"; Name = "new environment"; Address = Uri("http://c.se"); EnvironmentId = "agentEnv1" }
    let browser = createBrowser()
    let agentCreated = post browser rootUrl newAgent

    let response = browser.Delete (rootUrl + "/" + agentCreated.Id)
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode)
    let agentFound = get<Agent[]> rootUrl |> Seq.tryFind(fun e -> e.Id = agentCreated.Id)
    Assert.True agentFound.IsNone
