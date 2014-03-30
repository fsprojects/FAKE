/// Contains a http helper functions for FAKE.Deploy.
module Fake.FakeDeployAgentHelper

open System
open System.IO
open System.Net
open HttpListenerHelper

/// A http response type.
type Response = 
    { Messages : seq<Fake.ProcessHelper.ConsoleMessage>
      Exception : obj
      IsError : bool }

/// Deployment result type.
type DeploymentResponse = 
    | Success of Response
    | Failure of Response
    | QueryResult of seq<NuSpecPackage>

let private wrapFailure = 
    function 
    | Choice1Of2(Message msg) -> msg
    | Choice1Of2(Exception exn) -> 
        { Messages = Seq.empty
          IsError = true
          Exception = exn }
        |> Failure
    | Choice2Of2 exn -> 
        { Messages = Seq.empty
          IsError = true
          Exception = exn }
        |> Failure

let private webClient() = 
    let client = new WebClient()
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    client.Headers.Add("fake-deploy-use-http-response-messages", "true")
    client

/// Gets the http response from the given URL and runs it with the given function.
let private get f url = 
    try 
        let uri = new Uri(url, UriKind.Absolute)
        use client = webClient()
        let msg = client.DownloadString(uri)
        try 
            match msg |> Json.deserialize with
            | Message msg -> 
                f msg
                |> Message
                |> Choice1Of2
            | Exception exn -> Exception exn |> Choice1Of2
        with _ -> 
            f msg
            |> Message
            |> Choice1Of2
    with exn -> Choice2Of2 exn

/// sends the given body using the given action (POST or PUT) to the given url
let private sendData<'t> action url body = 
    try 
        let uri = new Uri(url, UriKind.Absolute)
        use client = webClient()
        use ms = new MemoryStream(client.UploadData(uri, action, body))
        use sr = new StreamReader(ms, Text.Encoding.UTF8)
        let msg = sr.ReadToEnd()
        try 
            match msg |> Json.deserialize with
            | Message msg -> 
                Json.deserialize<'t> msg
                |> Message
                |> Choice1Of2
            | Exception exn -> Exception exn |> Choice1Of2
        with _ -> 
            msg
            |> Json.deserialize<'t>
            |> Message
            |> Choice1Of2
    with exn -> Choice2Of2 exn

/// Posts the given body to the given URL.
let private post = sendData<DeploymentResponse> "POST"

/// Puts the given body to the given URL.
let private put = sendData<DeploymentResponse> "PUT"

type DeployStatus = 
    | Active
    | Inactive

type App = 
    { Name : string
      Version : string }

/// Returns all releases of the given app from the given server.
let getReleasesFor server appname status = 
    if String.IsNullOrEmpty(appname) then server + "/deployments?status=" + status
    else server + "/deployments/" + appname + "?status=" + status
    |> get (Json.deserialize<DeploymentResponse>)

/// Performs a rollback of the given app on the server.
let rollbackTo server appname version = 
    put (server + "/deployments/" + appname + "?version=" + version) [||] |> wrapFailure

/// Returns all active releases from the given server.
let getAllActiveReleases server = getReleasesFor server null "active" |> wrapFailure

/// Returns the active release of the given app from the given server.
let getActiveReleasesFor server appname = getReleasesFor server appname "active" |> wrapFailure

/// Returns all releases of the given app from the given server.
let getAllReleasesFor server appname = 
    if String.IsNullOrEmpty(appname) then server + "/deployments/"
    else server + "/deployments/" + appname + "/"
    |> get (Json.deserialize<DeploymentResponse>)
    |> wrapFailure

/// Returns all releases from the given server.
let getAllReleases server = getAllReleasesFor server null

/// Posts a deployment package to the given URL.
let postDeploymentPackage url packageFileName = post url (ReadFileAsBytes packageFileName) |> wrapFailure

/// Posts a deployment package to the given URL and handles the response.
let DeployPackage url packageFileName = 
    match postDeploymentPackage url packageFileName with
    | Success _ -> tracefn "Deployment of %s successful" packageFileName
    | Failure exn -> failwithf "Deployment of %A failed\r\n%A" packageFileName exn.Exception
    | response -> failwithf "Deployment of %A failed\r\n%A" packageFileName response

/// Deprecated, use DeployPackage
[<Obsolete("Use DeployPackage")>]
let PostDeploymentPackage = DeployPackage

/// Performs a rollback of the given app at the given URL and handles the response.
let RollbackPackage url appName version = 
    match rollbackTo url appName version with
    | Success _ -> tracefn "Rollback of %s to %s successful" appName version
    | Failure exn -> failwithf "Deployment of %s to %s failed\r\n%A" appName version exn.Exception
    | response -> failwithf "Deployment of %s to %s failed\r\n%A" appName version response
