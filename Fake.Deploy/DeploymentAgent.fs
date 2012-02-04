module Fake.DeploymentAgent
    
    open System
    open System.Net
    open System.Threading
    open System.Diagnostics
    open Newtonsoft.Json
    open Fake.DeploymentHelper

    let mutable eventLog : EventLog = null 

    let private listener = 
        let l = new HttpListener()
        l.Prefixes.Add("http://localhost:8080/fake/")
        l.Start()
        l

    let cts = new CancellationTokenSource()

    let writeResponse (str : string) (ctx : HttpListenerContext) = 
        let response = Text.Encoding.UTF8.GetBytes(str)
        ctx.Response.ContentLength64 <- response.Length |> int64
        ctx.Response.ContentEncoding <- Text.Encoding.UTF8
        ctx.Response.OutputStream.Write(response, 0, response.Length)
        ctx.Response.OutputStream.Close()

    let handleSuccess package (ctx : HttpListenerContext) = 
        let msg = sprintf "Successfully deployed %A" package
        eventLog.WriteEntry(msg, EventLogEntryType.Information)
        let response = JsonConvert.SerializeObject(DeploymentResponse.Sucessful(package.Id, package.Version))
        writeResponse response ctx

    let handleFailure package (ctx : HttpListenerContext) exn = 
        let msg = sprintf "Deployment failed: %A " package
        eventLog.WriteEntry(msg, EventLogEntryType.Information)
        let response = JsonConvert.SerializeObject(DeploymentResponse.Failure(package.Id, package.Version, exn))
        writeResponse response ctx

    let handleRequest (ctx : HttpListenerContext) = 
        if ctx.Request.HasEntityBody
        then 
            try
                use sr = new IO.StreamReader(ctx.Request.InputStream, Text.Encoding.UTF8)
                let package = (JsonConvert.DeserializeObject<DeploymentPackage>(sr.ReadToEnd()))
                match runDeployment package with
                | Choice1Of2(result, package) -> handleSuccess package ctx
                | Choice2Of2(e) -> handleFailure package ctx e
            with e ->
                let msg = sprintf "Fake Deploy Request Error:\n\n%A" e
                eventLog.WriteEntry(msg, EventLogEntryType.Error)
                writeResponse msg ctx
                

    let start log =
        eventLog <- log
        let listenerLoop = 
            async {
                try
                    use l = listener
                    while true do
                        handleRequest (l.GetContext())
                with e ->
                    eventLog.WriteEntry(sprintf "Fake Deploy Listener Error:\n\n%A" e, EventLogEntryType.Error)
                    
            }
        Async.Start(listenerLoop, cts.Token)

    let stop() = 
        ()



