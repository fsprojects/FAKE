module Fake.DeploymentAgent
    
open System
open System.IO
open System.Net
open System.Threading
open System.Diagnostics
open Fake.DeploymentHelper
open Fake.HttpListenerHelper

let mutable private logger : (string * EventLogEntryType) -> unit = ignore 

let private runDeployment (ctx : HttpListenerContext) =
    let packageBytes = getBodyFromContext ctx

    let package,scriptFile = unpack false packageBytes
    let response = doDeployment package.Name scriptFile
    
    match response with
    | Success -> logger (sprintf "Successfully deployed %s %s" package.Id package.Version, EventLogEntryType.Information)
    | response -> logger (sprintf "Deployment failed of %s %s failed" package.Id package.Version, EventLogEntryType.Information)

    response
    |> Json.serialize

let private getActiveReleases (ctx : HttpListenerContext) =
    getActiveReleases() |> Json.serialize

let private getAllReleases (ctx : HttpListenerContext) = 
    getAllReleases() |> Json.serialize

let private getAllReleasesFor appname (ctx : HttpListenerContext) = 
    getAllReleasesFor appname |> Json.serialize

let private getActiveReleaseFor appname (ctx : HttpListenerContext) =
    getActiveReleaseFor appname |> Json.serialize

let private runRollback appname version (ctx : HttpListenerContext) = 
    rollback appname version |> Json.serialize

let requestMap =
    lazy
    DeploymentHelper.getAllReleases() 
    |> Seq.collect (fun spec -> 
                                [
                                    "GET", "/deployments/" + spec.Id, getAllReleasesFor spec.Id
                                    "GET", "/deployments/" + spec.Id + "?status=active", getActiveReleaseFor spec.Id
                                    "GET", "/rollback/" + spec.Id + "?version="+spec.Version, runRollback spec.Id spec.Version
                                ])
    |> Seq.append [
        "POST", "", runDeployment
        "GET", "/deployments/?status=active", getActiveReleases
        "GET", "/deployments/", getAllReleases
    ] |> createRequestMap


let start log serverName port = 
    logger <- log
    requestMap.Value |> Map.iter (fun k _ -> Console.WriteLine(k))
    HttpListenerHelper.start log serverName port requestMap.Value