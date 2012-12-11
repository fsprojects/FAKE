module Fake.DeploymentAgent
    
open System
open System.IO
open System.Net
open System.Threading
open System.Diagnostics
open Fake.DeploymentHelper
open Fake.HttpListenerHelper

let mutable private logger : (string * EventLogEntryType) -> unit = ignore 


let private runDeployment workDir args (ctx : HttpListenerContext) =
    let packageBytes = getBodyFromContext ctx

    let package,scriptFile = unpack workDir false packageBytes
    let response = doDeployment package.Name scriptFile
    
    match response with
    | HttpClientHelper.Success -> logger (sprintf "Successfully deployed %s %s" package.Id package.Version, EventLogEntryType.Information)
    | response -> logger (sprintf "Deployment failed of %s %s failed" package.Id package.Version, EventLogEntryType.Information)

    response
    |> Json.serialize

let private getActiveReleases workDir args (ctx : HttpListenerContext) =
    getActiveReleases workDir 
    |> HttpClientHelper.DeploymentResponse.QueryResult 
    |> Json.serialize

let private getAllReleases workDir args (ctx : HttpListenerContext) = 
    getAllReleases workDir 
    |> HttpClientHelper.DeploymentResponse.QueryResult 
    |> Json.serialize

let private getAllReleasesFor workDir (args:Map<_,_>) (ctx : HttpListenerContext) = 
    getAllReleasesFor workDir args.["app"]
    |> HttpClientHelper.DeploymentResponse.QueryResult 
    |> Json.serialize

let private getActiveReleaseFor workDir (args:Map<_,_>) (ctx : HttpListenerContext) =
    getActiveReleaseFor workDir args.["app"]
    |> Seq.singleton
    |> HttpClientHelper.DeploymentResponse.QueryResult 
    |> Json.serialize

let private runRollbackToVersion workDir (args:Map<_,_>) (ctx : HttpListenerContext) = 
    rollbackTo workDir args.["app"] args.["version"]
    |> Json.serialize

let routes workDir =
    defaultRoutes
        @ [ "POST", "", runDeployment workDir
            "GET", "/deployments/{app}/", getAllReleasesFor workDir
            "GET", "/deployments/{app}?status=active", getActiveReleaseFor workDir
            "PUT", "/deployments/{app}?version={version}", runRollbackToVersion workDir
            "GET", "/deployments?status=active", getActiveReleases workDir
            "GET", "/deployments/", getAllReleases workDir ]

let start log workDir serverName port = 
    logger <- log
    routes workDir |> Seq.iter (fun (v,r,_) -> tracefn "%s %s" v r)
    HttpListenerHelper.start log serverName port (routes workDir |> createRoutes)
