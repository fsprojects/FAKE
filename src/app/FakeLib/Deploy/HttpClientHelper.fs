module Fake.HttpClientHelper

open System
open System.IO
open System.Net

type DeploymentResponse =
| Success
| Failure of obj
| RolledBack
| Cancelled
| Unknown
| QueryResult of seq<NuSpecPackage> 

let get f url = 
    let uri = new Uri(url, UriKind.Absolute)
    let client = new WebClient()
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    client.DownloadString(uri) |> f

let post url body = 
    let uri = new Uri(url, UriKind.Absolute)
    let client = new WebClient()
    let mutable uploaded = false
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    use ms = new MemoryStream(client.UploadData(uri, "POST", body))
    use sr = new StreamReader(ms, Text.Encoding.UTF8)
    sr.ReadToEnd() |> Json.deserialize<DeploymentResponse>

let put body url = 
    let uri = new Uri(url, UriKind.Absolute)
    let client = new WebClient()
    let mutable uploaded = false
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    use ms = new MemoryStream(client.UploadData(uri, "PUT", body))
    use sr = new StreamReader(ms, Text.Encoding.UTF8)
    sr.ReadToEnd() |> Json.deserialize<DeploymentResponse>

let getReleasesFor server appname status =
    if String.IsNullOrEmpty(appname)
    then server + "/deployments?status=" + status 
    else server + "/deployments/" + appname + "?status=" + status
    |> get (Json.deserialize<DeploymentResponse>)

let rollbackTo server appname version =
    server + "/deployments/"+ appname + "?version=" + version 
        |> put [||]

let getAllActiveReleases server = getReleasesFor server null "active"

let getActiveReleasesFor server appname = getReleasesFor server appname "active"

let getAllReleasesFor server appname = 
    if String.IsNullOrEmpty(appname)
    then server + "/deployments/"
    else server + "/deployments/" + appname + "/"
    |> get (Json.deserialize<DeploymentResponse>)

let getAllReleases server = getAllReleasesFor server null

let postDeploymentPackage url packageFileName = post url (ReadFileAsBytes packageFileName)

let PostDeploymentPackage url packageFileName = 
    match postDeploymentPackage url packageFileName with
    | Success -> tracefn "Deployment of %s successful" packageFileName
    | Failure exn -> failwithf "Deployment of %A failed\r\n%A" packageFileName exn
    | response -> failwithf "Deployment of %A failed\r\n%A" packageFileName response

let RollbackPackage url appName version = 
    match rollbackTo url appName version with
    | Success -> tracefn "Rollback of %s to %s successful" appName version
    | RolledBack -> tracefn "Rollback of %s to %s successful" appName version
    | Failure exn -> failwithf "Deployment of %s to %s failed\r\n%A" appName version exn
    | response -> failwithf "Deployment of %s to %s failed\r\n%A" appName version response