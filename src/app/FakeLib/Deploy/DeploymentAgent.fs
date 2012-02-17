module Fake.DeploymentAgent
    
open System
open System.IO
open System.Net
open System.Threading
open System.Diagnostics
open Fake.DeploymentHelper
open Fake.HttpListener

let mutable private logger : (string * EventLogEntryType) -> unit = ignore 

let private runDeployment (ctx : HttpListenerContext) =
    match (runDeployment (HttpListener.getBodyFromContext ctx)) with
    | response when response.Status = Success ->
        logger (sprintf "Successfully deployed %A" response.PackageName, EventLogEntryType.Information)
        response
    | response ->
        logger (sprintf "Deployment failed: %A" response.PackageName, EventLogEntryType.Information)
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
                                    "GET", "/releases/all/"+spec.Id, getAllReleasesFor spec.Id
                                    "GET", "/releases/active/"+spec.Id, getActiveReleaseFor spec.Id
                                    "GET", "/rollback/"+spec.Id+"?version="+spec.Version, runRollback spec.Id spec.Version
                                ])
    |> Seq.append [
        "POST", "", runDeployment
        "GET", "/releases/active", getActiveReleases
        "GET", "/releases/all", getAllReleases
    ] |> HttpListener.createRequestMap


let start port log = 
    logger <- log
    requestMap.Value |> Map.iter (fun k _ -> Console.WriteLine(k))
    HttpListener.start port log requestMap.Value

let stop() = 
    HttpListener.stop()
