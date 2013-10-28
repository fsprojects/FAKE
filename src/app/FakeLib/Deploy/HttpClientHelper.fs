/// Contains a http listener for FAKE.Deploy.
module Fake.HttpClientHelper

open System
open System.IO
open System.Net

/// A http response type.
type Response = {
    Messages : seq<ConsoleMessage>
    Exception : obj
    IsError : bool }

/// Deployment result type.
type DeploymentResponse =
| Success of Response
| Failure of Response
| QueryResult of seq<NuSpecPackage>

/// Gets the http response from the given URL and runs it with the given function.
let get f url =
    let uri = new Uri(url, UriKind.Absolute)
    let client = new WebClient()
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    client.DownloadString(uri) |> f

/// Posts the given body to the given URL.
let post url body = 
    let uri = new Uri(url, UriKind.Absolute)
    let client = new WebClient()
    let mutable uploaded = false
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    use ms = new MemoryStream(client.UploadData(uri, "POST", body))
    use sr = new StreamReader(ms, Text.Encoding.UTF8)
    sr.ReadToEnd() |> Json.deserialize<DeploymentResponse>

/// Puts the given body to the given URL.
let put body url = 
    let uri = new Uri(url, UriKind.Absolute)
    let client = new WebClient()
    let mutable uploaded = false
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    use ms = new MemoryStream(client.UploadData(uri, "PUT", body))
    use sr = new StreamReader(ms, Text.Encoding.UTF8)
    sr.ReadToEnd() |> Json.deserialize<DeploymentResponse>

/// Returns all releases of the given app from the given server.
let getReleasesFor server appname status =
    if String.IsNullOrEmpty(appname)
    then server + "/deployments?status=" + status 
    else server + "/deployments/" + appname + "?status=" + status
    |> get (Json.deserialize<DeploymentResponse>)

/// Performs a rollback of the given app on the server.
let rollbackTo server appname version =
    server + "/deployments/"+ appname + "?version=" + version 
        |> put [||]

/// Returns all active releases from the given server.
let getAllActiveReleases server = getReleasesFor server null "active"

/// Returns the active release of the given app from the given server.
let getActiveReleasesFor server appname = getReleasesFor server appname "active"

/// Returns all releases of the given app from the given server.
let getAllReleasesFor server appname = 
    if String.IsNullOrEmpty(appname)
    then server + "/deployments/"
    else server + "/deployments/" + appname + "/"
    |> get (Json.deserialize<DeploymentResponse>)

/// Returns all releases from the given server.
let getAllReleases server = getAllReleasesFor server null

/// Posts a deployment package to the given URL.
let postDeploymentPackage url packageFileName = post url (ReadFileAsBytes packageFileName)

/// Posts a deployment package to the given URL and handles the response.
let PostDeploymentPackage url packageFileName = 
    match postDeploymentPackage url packageFileName with
    | Success _ -> tracefn "Deployment of %s successful" packageFileName
    | Failure exn -> failwithf "Deployment of %A failed\r\n%A" packageFileName exn.Exception
    | response -> failwithf "Deployment of %A failed\r\n%A" packageFileName response

/// Performs a rollback of the given app at the given URL and handles the response.
let RollbackPackage url appName version = 
    match rollbackTo url appName version with
    | Success _ -> tracefn "Rollback of %s to %s successful" appName version
    | Failure exn -> failwithf "Deployment of %s to %s failed\r\n%A" appName version exn.Exception
    | response -> failwithf "Deployment of %s to %s failed\r\n%A" appName version response