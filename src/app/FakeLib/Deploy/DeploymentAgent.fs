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
    let response = runDeployment (getBodyFromContext ctx)
    match response with
    | (fileName,Success) -> logger (sprintf "Successfully deployed %A" fileName, EventLogEntryType.Information)
    | (fileName,response) -> logger (sprintf "Deployment failed: %A" fileName, EventLogEntryType.Information)

    response
    |> Json.serialize
    |> Some

let private getActiveReleases (ctx : HttpListenerContext) =
    getActiveReleases() |> Json.serialize |> Some

let private getAllReleases (ctx : HttpListenerContext) = 
    getAllReleases() |> Json.serialize |> Some

let private getAllReleasesFor appname (ctx : HttpListenerContext) = 
    getAllReleasesFor appname |> Json.serialize |> Some

let private getActiveReleaseFor appname (ctx : HttpListenerContext) =
    getActiveReleaseFor appname |> Json.serialize |> Some

let private runRollback appname version (ctx : HttpListenerContext) = 
    rollback appname version |> Json.serialize |> Some

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