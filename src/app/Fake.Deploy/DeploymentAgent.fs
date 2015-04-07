module Fake.DeploymentAgent

open System
open System.IO
open System.Diagnostics
open Fake
open Fake.DeploymentHelper
open Fake.DeployAgentModule
open Nancy
open Nancy.Hosting.Self
open Nancy.Security

let mutable private logger : string * EventLogEntryType -> unit = ignore

let getBodyFromNancyRequest (ctx : Nancy.Request) = 
    use ms = new MemoryStream()
    ctx.Body.CopyTo ms
    ms.ToArray()

let getScriptArgumentsFromNancyRequest (ctx : Nancy.Request) =
    ctx.Headers 
    |> Seq.choose (fun pair -> if pair.Key = FakeDeployAgentHelper.scriptArgumentsHeaderName then Some <| pair.Value else None)
    |> Seq.head
    |> Seq.head
    |> fromHeaderValue

let  runDeployment workDir (ctx : Nancy.Request) = 
    let packageBytes = getBodyFromNancyRequest ctx
    let package, scriptFile = unpack workDir false packageBytes
    let scriptArguments = getScriptArgumentsFromNancyRequest ctx
    let response = doDeployment scriptFile scriptArguments
    match response with
    | FakeDeployAgentHelper.Success _ -> 
        logger (sprintf "Successfully deployed %s %s" package.Id package.Version, EventLogEntryType.Information)
    | response -> 
        logger 
            (sprintf "Deployment failed of %s %s failed\r\nDetails:\r\n%A" package.Id package.Version response, 
             EventLogEntryType.Information)
    response |> Json.serialize


let createNancyHost uri =
    let config = HostConfiguration()
    config.UrlReservations.CreateAutomatically <- true
    new NancyHost(config, uri)


let mutable workDir = Path.GetDirectoryName(Uri(typedefof<FakeModule>.Assembly.CodeBase).LocalPath)


type DeployAgentModule() as http =
    inherit FakeModule("/fake")

    let createResponse x = 
        x
        |> FakeDeployAgentHelper.DeploymentResponse.QueryResult
        |> http.Response.AsJson

    do
        http.RequiresAuthentication()


        http.post "/" (fun _ -> 
            runDeployment workDir http.Request)

        http.get "/deployments/" (fun _ -> 
            let status = http.Request.Query ?> "status"
            match status with
                | "active" -> getActiveReleases workDir
                | _ -> getAllReleases workDir
            |> createResponse
        )

        http.get "/deployments/{app}/" (fun p ->
            let app = p ?> "app"
            let status = http.Request.Query ?> "status"
            match status with
                | "active" -> 
                    getActiveReleaseFor workDir app
                    |> Seq.singleton
                | _ -> getAllReleasesFor workDir app
            |> createResponse
        )

        http.put "/deployments/{app}" (fun p ->
            let version = p ?> "version"
            let app = p ?> "app"
            let result = rollbackTo workDir app version
            http.Response
                .AsJson result
        )

        http.get "/statistics/" (fun _ -> getStatistics() |> http.Response.AsJson)
