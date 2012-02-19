module Fake.HttpListenerHelper

open System
open System.IO
open System.Net
open System.Net.NetworkInformation
open System.Threading
open System.Diagnostics
open System.Text.RegularExpressions
open Fake.DeploymentHelper

type Route = {
        Verb : string
        Path : string
    }
    with 
        override x.ToString() = sprintf "%s %s" x.Verb x.Path
        
let private listener serverName port = 
    let listener = new HttpListener()
    listener.Prefixes.Add(sprintf "http://%s:%s/fake/" serverName port)
    listener.Start()
    listener

let private writeResponse (ctx : HttpListenerContext) (str : string) = 
    let response = Text.Encoding.UTF8.GetBytes(str)
    ctx.Response.ContentLength64 <- response.Length |> int64
    ctx.Response.ContentEncoding <- Text.Encoding.UTF8
    ctx.Response.Close(response, true)

let matchGroups (pat:string) (inp:string) =
    let m = Regex.Match(inp, pat) in
    if m.Success
    then Some (List.tail [ for g in m.Groups -> g.Value ])
    else None

let private routeRequest log (ctx : HttpListenerContext) (requestMap : Map<Route, (HttpListenerContext -> string option)>) =     
    try
        let route = { 
            Verb = ctx.Request.HttpMethod
            Path = ctx.Request.RawUrl.Replace("fake/", "").Trim('/').ToLower() }

        match Map.tryFind route requestMap with
        | Some handler -> 
            handler(ctx) 
              |> Option.iter (writeResponse ctx)
        | None -> writeResponse ctx (sprintf "Unknown route %s" ctx.Request.Url.AbsoluteUri)
    with e ->
        let msg = sprintf "Fake Deploy Request Error:\n\n%A" e
        log (msg, EventLogEntryType.Error)
        writeResponse ctx msg

let private getStatus (ctx : HttpListenerContext) =
    "Http listener is running"
    |> Some

let createRequestMap routes : Map<Route, (HttpListenerContext -> string option)>= 
    routes
    |> Seq.map (fun (verb, route : string, func) -> { Verb = verb; Path = route.Trim([|'/'; '\\'|]).ToLower() }, func)
    |> Map.ofSeq

let StatusRequestMap = 
    [ "GET", "", getStatus ] 
    |> createRequestMap

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

let getFirstFreePort() =
    let defaultPort = 8080
    let usedports = NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners() |> Seq.map (fun x -> x.Port)
    let ports = seq { for port in defaultPort .. defaultPort + 2048 do yield port }
    let port = ports |> Seq.find (fun p -> not <| Seq.contains p usedports)
    port.ToString()

let getPort configPort =    
    match configPort with
    | "*" -> getFirstFreePort()
    | _ -> configPort 


type Listener =
  { ServerName: string
    Port: string
    CancelF: unit -> unit }
  with
      member x.Cancel() = x.CancelF()
      member x.RootUrl = sprintf "http://%s:%s/fake/" x.ServerName x.Port

let emptyListener = { 
    ServerName = ""
    Port = ""
    CancelF = id }

let start log serverName port requestMap =
    let cts = new CancellationTokenSource()
    let usedPort = getPort port
    let listenerLoop = 
        async {
            try 
                log (sprintf "Trying to start Fake Deploy server @ %s port %s" serverName usedPort, EventLogEntryType.Information)
                use l = listener serverName usedPort
                let prefixes = l.Prefixes |> separated ","
                log (sprintf "Fake Deploy now listening @ %s" prefixes, EventLogEntryType.Information)
                while true do
                    routeRequest log (l.GetContext()) requestMap
            with e ->
                log (sprintf "Listener Error:\n\n%A" e, EventLogEntryType.Error)
                    
        }
    Async.Start(listenerLoop, cts.Token)
    { ServerName = serverName; Port = usedPort; CancelF = cts.Cancel }

let startWithConsoleLogger serverName port requestMap =
    start TraceHelper.logToConsole serverName port requestMap


    