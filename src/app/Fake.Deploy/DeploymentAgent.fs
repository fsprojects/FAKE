module Fake.DeploymentAgent
    
open System
open System.Net
open System.Threading
open System.Diagnostics
open Fake.DeploymentHelper

let mutable logger : (string * EventLogEntryType) -> unit = ignore 

let private listener port = 
    let listener = new HttpListener()
    listener.Prefixes.Add(sprintf "http://localhost:%s/fake/" port)
    listener.Start()
    listener

let cts = new CancellationTokenSource()

let writeResponse (ctx : HttpListenerContext) (str : string) = 
    let response = Text.Encoding.UTF8.GetBytes(str)
    ctx.Response.ContentLength64 <- response.Length |> int64
    ctx.Response.ContentEncoding <- Text.Encoding.UTF8
    ctx.Response.Close(response, true)

let handleSuccess package (ctx : HttpListenerContext) = 
    let msg = sprintf "Successfully deployed %s" (package.ToString())
    logger (msg, EventLogEntryType.Information)
    DeploymentResponse.Sucessful package.Key 
    |> Json.serialize
    |> writeResponse ctx

let handleFailure package (ctx : HttpListenerContext) exn = 
    let msg = sprintf "Deployment failed: %s " (package.ToString())
    logger (msg, EventLogEntryType.Information)
    DeploymentResponse.Failure(package.Key, exn) 
    |> Json.serialize
    |> writeResponse ctx

let handleRequest (ctx : HttpListenerContext) = 
    if ctx.Request.HttpMethod = "POST" && ctx.Request.ContentType = "application/fake" && ctx.Request.HasEntityBody then 
        try
            use sr = new IO.StreamReader(ctx.Request.InputStream, Text.Encoding.UTF8)
            let package = sr.ReadToEnd() |> Json.deserialize
            match runDeployment package with
            | Choice1Of2(result, package) -> handleSuccess package ctx
            | Choice2Of2(e) -> handleFailure package ctx e
        with e ->
            let msg = sprintf "Fake Deploy Request Error:\n\n%A" e
            logger (msg, EventLogEntryType.Error)
            writeResponse ctx msg
                

let start log port =
    logger <- log
    let listenerLoop = 
        async {
            try
                use l = listener port
                let prefixes = l.Prefixes |> separated ","
                log (sprintf "Fake Deploy now listening @ %s" prefixes, EventLogEntryType.Information)
                while true do
                    handleRequest (l.GetContext())
            with e ->
                logger (sprintf "Fake Deploy Listener Error:\n\n%A" e, EventLogEntryType.Error)
                    
        }
    Async.Start(listenerLoop, cts.Token)

let stop() = 
    cts.Cancel()