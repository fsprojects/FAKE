module Fake.DeploymentAgent
    
open System
open System.IO
open System.Net
open System.Threading
open System.Diagnostics
open Fake.DeploymentHelper
open Fake.HttpListenerHelper

let mutable private logger : (string * EventLogEntryType) -> unit = ignore 

let private runDeployment args (ctx : HttpListenerContext) =
    let packageBytes = getBodyFromContext ctx

    let package,scriptFile = unpack false packageBytes
    let response = doDeployment package.Name scriptFile
    
    match response with
    | Success -> logger (sprintf "Successfully deployed %s %s" package.Id package.Version, EventLogEntryType.Information)
    | response -> logger (sprintf "Deployment failed of %s %s failed" package.Id package.Version, EventLogEntryType.Information)

    response
    |> Json.serialize

let private getActiveReleases args (ctx : HttpListenerContext) =
    getActiveReleases workDir |> Json.serialize

let private getAllReleases args (ctx : HttpListenerContext) = 
    getAllReleases workDir |> Json.serialize

let private getAllReleasesFor (args: string list) (ctx : HttpListenerContext) = 
    getAllReleasesFor workDir args.[0] |> Json.serialize

let private getActiveReleaseFor (args: string list) (ctx : HttpListenerContext) =
    getActiveReleaseFor workDir args.[0] |> Json.serialize

let private runRollback (args: string list)  (ctx : HttpListenerContext) = 
    rollback workDir args.[0] args.[1] |> Json.serialize

let routes =
    defaultRoutes
    |> Seq.append [
        "POST", "", runDeployment
        "GET", "/deployments/([^/]+)/", getAllReleasesFor
        "GET", @"/deployments/([^/]+)/\?status=active", getActiveReleaseFor
        "GET", @"/rollback/(.+)\?version=([^/]+)", runRollback
        "GET", "/deployments/?status=active", getActiveReleases
        "GET", "/deployments/", getAllReleases
    ]


let start log serverName port = 
    logger <- log
    routes |> Seq.iter (fun (v,r,_) -> tracefn "%s %s" v r)
    HttpListenerHelper.start log serverName port (routes |> createRoutes)