module Fake.DeploymentAgent

open System.IO
open Fake
open Fake.DeploymentHelper
open Fake.DeployAgentModule
open Nancy
open Nancy.Hosting.Self
open Nancy.Security

let getBodyFromNancyRequest (ctx : Nancy.Request) = 
    use ms = new MemoryStream()
    ctx.Body.CopyTo ms
    ms.ToArray()

let getScriptArgumentsFromNancyRequest (ctx : Nancy.Request) : string [] =
    let args = 
        ctx.Headers 
        |> Seq.choose (fun pair -> 
            if pair.Key = FakeDeployAgentHelper.scriptArgumentsHeaderName then 
                Some pair.Value 
            else None
        ) 
        |> List.ofSeq

    match args with 
    | [] -> [||]
    | _ -> args |> Seq.collect id |> Seq.map fromHeaderValue |> Seq.collect id |> Array.ofSeq
    

let  runDeployment workDir (ctx : Nancy.Request) = 
    let packageBytes = getBodyFromNancyRequest ctx
    let package, scriptFile = unpack workDir false packageBytes
    let scriptArguments = getScriptArgumentsFromNancyRequest ctx
    let response = doDeployment scriptFile scriptArguments
    match response with
    | FakeDeployAgentHelper.Success _ -> 
        Logger.info "Successfully deployed %s %s" package.Id package.Version
    | FakeDeployAgentHelper.Failure failure ->
        Logger.error "Deployment failed of %s %s failed\r\nDetails:\r\n%A" package.Id package.Version response
        Logger.info "Failure: %A" failure.Exception
        Logger.info "Messages for failed deploy:"
        failure.Messages |> Seq.iter(fun m -> Logger.info "Deploy message: %A IsError:%b %s" m.Timestamp m.IsError m.Message )
    | response -> 
        Logger.error "Deployment failed of %s %s failed\r\nDetails:\r\n%A" package.Id package.Version response

    response |> Json.serialize


let createNancyHost uri =
    let config = HostConfiguration()
    config.UrlReservations.CreateAutomatically <- true
    new NancyHost(config, uri)

let mutable workDir = AppConfig.WorkDirectory

type DeployAgentModule() as http =
    inherit FakeModule("/fake")

    let createResponse x = 
        x
        |> FakeDeployAgentHelper.DeploymentResponse.QueryResult
        |> http.Response.AsJson

    let logged f =
        try
            f()
        with
        | ex ->
            Logger.errorEx ex "%s" ex.Message
            reraise()

    do
        http.RequiresAuthentication()

        http.post "/" (fun _ -> 
            logged (fun () -> runDeployment workDir http.Request))

        http.get "/deployments/" (fun _ ->
            logged (fun () -> 
                let status = http.Request.Query ?> "status"
                match status with
                    | "active" -> getActiveReleases workDir
                    | _ -> getAllReleases workDir
                |> createResponse
            )
        )

        http.get "/deployments/{app}/" (fun p ->
            logged (fun () -> 
                let app = p ?> "app"
                let status = http.Request.Query ?> "status"
                match status with
                    | "active" -> 
                        getActiveReleaseFor workDir app
                        |> Seq.singleton
                    | _ -> getAllReleasesFor workDir app
                |> createResponse
            )
        )

        http.put "/deployments/{app}" (fun p ->
            logged (fun () -> 
                let version = p ?> "version"
                let app = p ?> "app"
                let result = rollbackTo workDir app version
                http.Response
                    .AsJson result
            )
        )

        http.get "/statistics/" (fun _ -> getStatistics() |> http.Response.AsJson)
