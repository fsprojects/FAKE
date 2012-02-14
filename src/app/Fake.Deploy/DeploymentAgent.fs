module Fake.DeploymentAgent
    
open System
open System.IO
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

let readAllBytes (s : Stream) =
    let ms = new MemoryStream()
    let buf = Array.zeroCreate 8192
    let rec impl () = 
        let read = s.Read(buf, 0, buf.Length) 
        if read > 0 then 
            ms.Write(buf, 0, read)
            impl ()
    impl ()
    ms

let handleRequest (ctx : HttpListenerContext) =     
    if ctx.Request.HttpMethod <> "POST" || ctx.Request.ContentType <> "application/fake" || not ctx.Request.HasEntityBody then () else

    try
        use sr =  readAllBytes (ctx.Request.InputStream)
        match (runDeployment (sr.ToArray())) with
        | response when response.Status = Success ->
            logger (sprintf "Successfully deployed %A" response.Key, EventLogEntryType.Information)
            response
        | response ->
            logger (sprintf "Deployment failed: %A" response.Key, EventLogEntryType.Information)
            response 
        |> Json.serialize

    with e ->
        let msg = sprintf "Fake Deploy Request Error:\n\n%A" e
        logger (msg, EventLogEntryType.Error)
        msg
        
    |> writeResponse ctx
                

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