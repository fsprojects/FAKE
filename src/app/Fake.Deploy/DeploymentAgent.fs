module Fake.DeploymentAgent
    
    open System
    open System.Net
    open System.Threading
    open System.Diagnostics
    open Fake.DeploymentHelper

    let mutable logger : (string * EventLogEntryType) -> unit = ignore 

    let private listener port = 
        let l = new HttpListener()
        l.Prefixes.Add(sprintf "http://localhost:%s/fake/" port)
        l.Start()
        l

    let cts = new CancellationTokenSource()

    let writeResponse (str : string) (ctx : HttpListenerContext) = 
        let response = Text.Encoding.UTF8.GetBytes(str)
        ctx.Response.ContentLength64 <- response.Length |> int64
        ctx.Response.ContentEncoding <- Text.Encoding.UTF8
        ctx.Response.Close(response, true)

    let handleSuccess package (ctx : HttpListenerContext) = 
        let msg = sprintf "Successfully deployed %s" (package.ToString())
        logger (msg, EventLogEntryType.Information)
        let response = DeploymentResponse.Sucessful(package.Id, package.Version) |> Json.serialize
        writeResponse response ctx

    let handleFailure package (ctx : HttpListenerContext) exn = 
        let msg = sprintf "Deployment failed: %s " (package.ToString())
        logger (msg, EventLogEntryType.Information)
        let response = DeploymentResponse.Failure(package.Id, package.Version, exn) |> Json.serialize
        writeResponse response ctx

    let handleRequest (ctx : HttpListenerContext) = 
        if ctx.Request.HttpMethod = "POST" && ctx.Request.ContentType = "application/fake" && ctx.Request.HasEntityBody
        then 
            try
                use sr = new IO.StreamReader(ctx.Request.InputStream, Text.Encoding.UTF8)
                let package = sr.ReadToEnd() |> Json.deserialize
                match runDeployment package with
                | Choice1Of2(result, package) -> handleSuccess package ctx
                | Choice2Of2(e) -> handleFailure package ctx e
            with e ->
                let msg = sprintf "Fake Deploy Request Error:\n\n%A" e
                logger (msg, EventLogEntryType.Error)
                writeResponse msg ctx
                

    let start log port =
        logger <- log
        let listenerLoop = 
            async {
                try
                    use l = listener port
                    log (sprintf "Fake Deploy now listening @ %s" (String.Join(",", l.Prefixes |> Seq.map id |> Array.ofSeq)), EventLogEntryType.Information)
                    while true do
                        handleRequest (l.GetContext())
                with e ->
                    logger (sprintf "Fake Deploy Listener Error:\n\n%A" e, EventLogEntryType.Error)
                    
            }
        Async.Start(listenerLoop, cts.Token)

    let stop() = 
        cts.Cancel()



