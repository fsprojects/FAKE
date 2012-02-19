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

let getReleasesFor server appname status =
    if String.IsNullOrEmpty(appname)
    then server + "/deployments?status=" + status 
    else server + "/deployments/" + appname + "?status=" + status
    |> get (Json.deserialize<DeploymentResponse>)

let rollbackFor server appname version =
    server + "/rollback/"+ appname + "?version=" + version 
        |> get (Json.deserialize<DeploymentResponse>)

let getAllActiveReleases server = getReleasesFor server null "active"

let getActiveReleasesFor server appname = getReleasesFor server appname "active"

let getAllReleasesFor server appname = 
    if String.IsNullOrEmpty(appname)
    then server + "/deployments/"
    else server + "/deployments/" + appname + "/"
    |> get (Json.deserialize<DeploymentResponse>)

let getAllReleases server = getAllReleasesFor server null

let postDeploymentPackage url packageFileName = ReadFileAsBytes packageFileName |> post url
