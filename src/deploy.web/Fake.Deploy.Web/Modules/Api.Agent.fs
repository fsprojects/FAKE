namespace Fake.Deploy.Web.Module

open System
open System.IO
open System.Net
open System.Web
open Nancy
open Nancy.ModelBinding
open Nancy.Security
open log4net
open Newtonsoft.Json
open Fake.EnvironmentHelper
open Fake.NuGetHelper
open Fake.Deploy.Web
open Fake.Deploy.Web.Module.NancyOp

type AgentResponse<'t> = 
    { case : string
      fields : 't [] }

type ApiAgent(dataProvider : IDataProvider, agentProxy : AgentProxy) as http = 
    inherit FakeModule("/api/v1/agent")

    let logger = LogManager.GetLogger(http.GetType().Name)
    
    let getAgent agentId = dataProvider.GetAgents [ agentId ] |> Seq.tryFind (fun i -> true)

    let callAgent agentId urlPart f logError = 
        http.returnAsJson (fun () -> agentProxy.CallAgent agentId urlPart f) logError
    
    do 
        http.get "/" 
            (fun p -> 
            http.returnAsJson (fun () -> dataProvider.GetAgents([])) 
                (fun e -> logger.Error("An error occured retrieving agents", e)))
    
        http.get "/{agentId}" (fun p -> 
            let id = (p ?> "agentId")
            try 
                match getAgent id with
                | None -> http.Response.AsText("Not found").WithStatusCode HttpStatusCode.NotFound |> box
                | Some e -> http.Response.AsJson e |> box
            with e -> 
                logger.Error(sprintf "An error occured retrieving agent %s" id, e)
                http.InternalServerError e |> box)
    
        http.post "/" (fun p -> 
            try 
                let tmp = http.Bind<Agent>()
                let agent = Agent.Create tmp.Address.AbsoluteUri tmp.EnvironmentId tmp.Name
                let env = dataProvider.GetEnvironments([ agent.EnvironmentId ]) |> Seq.head
                dataProvider.SaveEnvironments [ env.AddAgents [ agent ] ]
                dataProvider.SaveAgents [ agent ]
                http.Response.AsJson(agent).WithStatusCode HttpStatusCode.Created
            with e -> 
                logger.Error("An error occured saving agent", e)
                http.InternalServerError e)
    
        http.delete "/{agentId}" (fun p -> 
            let agentId = p ?> "agentId"
            try 
                let agent = dataProvider.GetAgents([ agentId ]) |> Seq.head
                dataProvider.DeleteAgent agentId
                let env = dataProvider.GetEnvironments([ agent.EnvironmentId ]) |> Seq.head
                let env' = env.RemoveAgents([ agent ])
                dataProvider.SaveEnvironments([ env' ])
                HttpStatusCode.NoContent |> box
            with e -> 
                logger.Error(sprintf "An error occured retrieving agent %s" agentId, e)
                http.InternalServerError e |> box)
    
        http.get "/details/{agentId}" 
            (fun p -> 
            let agentId = p ?> "agentId"
            callAgent agentId "statistics" fromJSON<MachineDetails> 
                (fun e -> logger.Error(sprintf "An error occured retrieving details for agent %s" agentId, e)))
    
        http.get "/deployments/{agentId}" 
            (fun p -> 
            let agentId = p ?> "agentId"
            callAgent agentId "deployments?status=active" (fun x -> 
                let result = fromJSON<AgentResponse<NuSpecPackage []>> x
                result) 
                (fun e -> logger.Error(sprintf "An error occured retrieving deployments for agent %s" agentId, e)))
    
        http.get "/details/{agentId}/{status}" 
            (fun p -> 
            let agentId = p ?> "agentId"
            let status = p ?> "status"
            callAgent agentId (sprintf "deployments?status=%s" status) fromJSON<AgentResponse<NuSpecPackage []>> 
                (fun e -> 
                logger.Error(sprintf "An error occured retrieving details for agent %s (status = %s)" agentId status, e)))
