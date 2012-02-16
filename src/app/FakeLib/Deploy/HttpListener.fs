module Fake.HttpListener

open System
open System.IO
open System.Net
open System.Threading
open System.Diagnostics
open Fake.DeploymentHelper

type Route = {
        Verb : string
        Path : string
    }

let mutable private logger : (string * EventLogEntryType) -> unit = ignore 
let private cts = new CancellationTokenSource()

let private listener port = 
    let listener = new HttpListener()
    listener.Prefixes.Add(sprintf "http://+:%s/fake/" port)
    listener.Start()
    listener

let private writeResponse (ctx : HttpListenerContext) (str : string) = 
    let response = Text.Encoding.UTF8.GetBytes(str)
    ctx.Response.ContentLength64 <- response.Length |> int64
    ctx.Response.ContentEncoding <- Text.Encoding.UTF8
    ctx.Response.Close(response, true)

let private routeRequest (ctx : HttpListenerContext) (requestMap : Map<Route, (HttpListenerContext -> string option)>) =     
    try
        let route =  { Verb = ctx.Request.HttpMethod; Path = ctx.Request.RawUrl.Replace("fake/", "").Trim('/') }
        
        match Map.tryFind route requestMap with
        | Some(handler) -> handler(ctx) |> Option.iter (writeResponse ctx)
        | None -> writeResponse ctx (sprintf "Unknown route %s" ctx.Request.Url.AbsoluteUri)
    with e ->
        let msg = sprintf "Fake Deploy Request Error:\n\n%A" e
        logger (msg, EventLogEntryType.Error)
        writeResponse ctx msg

let createRequestMap routes : Map<Route, (HttpListenerContext -> string option)>= 
    routes
    |> Seq.map (fun (verb, route : string, func) -> { Verb = verb; Path = route.Trim([|'/'; '\\'|]) }, func)
    |> Map.ofSeq

let getBodyFromContext (ctx : HttpListenerContext) = 
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
    if ctx.Request.HasEntityBody 
    then (readAllBytes ctx.Request.InputStream).ToArray() 
    else failwith "Attempted To Read body from request when there is not one"

let start port log requestMap =
    logger <- log
    let listenerLoop = 
        async {
            try
                use l = listener port
                let prefixes = l.Prefixes |> separated ","
                log (sprintf "Fake Deploy now listening @ %s" prefixes, EventLogEntryType.Information)
                while true do
                    routeRequest (l.GetContext()) requestMap
            with e ->
                logger (sprintf "Listener Error:\n\n%A" e, EventLogEntryType.Error)
                    
        }
    Async.Start(listenerLoop, cts.Token)

let stop() = 
    cts.Cancel()