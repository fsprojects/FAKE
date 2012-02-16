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

let requestMap = 
    [
        "POST", "", runDeployment
        "GET", "/releases/active/", getActiveReleases
        "GET", "/releases/all/", getAllReleases
    ] |> HttpListener.createRequestMap


let start port log = 
    logger <- log
    HttpListener.start port log requestMap

let stop() = 
    HttpListener.stop()
