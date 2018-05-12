/// Contains a http helper functions for FAKE.Deploy.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.FakeDeployAgentHelper

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Web
open HttpListenerHelper
open Fake.SshRsaModule

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
/// Authentication token received from a successful login
type AuthToken = 
    | AuthToken of Guid

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let mutable private authToken : Guid option = None

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
/// A http response type.
type Response = 
    { Messages : seq<Fake.ProcessHelper.ConsoleMessage>
      Exception : obj
      IsError : bool }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Url = string
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Action = string
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type FilePath = string

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Deployment = 
    { PackageFileName : FilePath
      Url : Url
      Timeout : TimeSpan
      Arguments : string list
      AuthToken : AuthToken option }

let private defaultTimeout = TimeSpan.FromMinutes(20.)

let private defaultDeployment = 
    { PackageFileName = ""
      Url = ""
      Timeout = defaultTimeout
      Arguments = []
      AuthToken = None }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
[<Literal>]
let scriptArgumentsHeaderName = "X-FAKE-Script-Arguments"

let private httpClient (timeout : TimeSpan) = 
    let client = new System.Net.Http.HttpClient()
    client.Timeout <- timeout
    client.DefaultRequestHeaders.Add("fake-deploy-use-http-response-messages", "true")
    match authToken with
    | None -> ()
    | Some t -> client.DefaultRequestHeaders.Add("AuthToken", string t)
    client

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let parseDeploymentResponse msg = 
    match msg |> Json.tryDeserialize<HttpResponseMessage<string>> with
    | Choice1Of2 m -> match m with
                        | Message msg -> Json.deserialize<DeploymentResponse> msg
                                        |> Message
                                        |> Choice1Of2
                        | Exception exn -> Exception exn |> Choice1Of2
    | Choice2Of2 exn -> match msg |> Json.tryDeserialize<DeploymentResponse> with
                        | Choice1Of2 m -> m |> Message |> Choice1Of2
                        | Choice2Of2 exn -> Choice2Of2 exn

/// Gets the http response from the given URL and runs it with the given function.
let private get timeout f (url: Url) = 
    try 
        use client = httpClient timeout
        let msg = client.GetStringAsync(url).Result
        f msg
    with exn -> Choice2Of2 exn

/// PUTS the given body to the given url
let private uploadData (url : Url) (body : byte []) timeout = 
    use client = httpClient timeout
    let fileStreamContent = new ByteArrayContent(body) :> HttpContent
    fileStreamContent.Headers.ContentType <- new Headers.MediaTypeHeaderValue("application/fake")
    let response = client.PutAsync(url, fileStreamContent).Result
    response.Content.ReadAsByteArrayAsync().Result

/// POSTs the given file to the given url
let private uploadFile (url : Url) (file : FilePath) (args : string []) timeout = 
    use client = httpClient timeout
    client.DefaultRequestHeaders.Add(scriptArgumentsHeaderName, args |> toHeaderValue)
    use fileStream = File.OpenRead file
    let fileStreamContent = new StreamContent(fileStream) :> HttpContent
    fileStreamContent.Headers.ContentType <- new Headers.MediaTypeHeaderValue("application/fake")
    let response = client.PostAsync(url, fileStreamContent).Result
    response.Content.ReadAsByteArrayAsync().Result

/// parses response body
let private processResponse (response : byte []) = 
    try 
        use ms = new MemoryStream(response)
        use sr = new StreamReader(ms, Text.Encoding.UTF8)
        let msg = sr.ReadToEnd()
        parseDeploymentResponse msg
    with exn -> Choice2Of2 exn

/// Posts the given file to the given URL.
let private post url file timeout = uploadFile url file timeout >> processResponse

/// Puts the given body to the given URL.
let private put url timeout = uploadData url timeout >> processResponse

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type DeployStatus = 
    | Active
    | Inactive

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type App = 
    { Name : string
      Version : string }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let buildExceptionString (r : Response) = 
    let msgs = 
        r.Messages
        |> Seq.map (fun m -> sprintf "  %s %s" (m.Timestamp.ToString("yyyy-MM-dd hh::mm:ss.fff")) m.Message)
        |> fun arr -> String.Join("\r\n", arr)
    sprintf "%O\r\n\r\nDeploy messages\r\n{\r\n%s\r\n}\r\n" r.Exception msgs

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
/// Authenticate against the given server with the given userId and private key
let authenticate server userId serverpathToPrivateKeyFile passwordForPrivateKey = 
    let privateKey = loadPrivateKey serverpathToPrivateKeyFile passwordForPrivateKey
    let challenge = REST.ExecuteGetCommand null null (server + "/login/" + userId)
    
    let signature = 
        challenge
        |> Convert.FromBase64String
        |> privateKey.Sign
        |> Convert.ToBase64String
    
    let postData = 
        sprintf "challenge=%s&signature=%s" (HttpUtility.UrlEncode challenge) (HttpUtility.UrlEncode signature)
    let response = REST.ExecutePost (server + "/login") "x" "x" postData
    authToken <- response.Trim([| '"' |])
                 |> Guid.Parse
                 |> Some
    authToken

/// Returns all releases of the given app from the given server.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let getReleasesFor server appname status = 
    if String.IsNullOrEmpty(appname) then server + "/deployments?status=" + status
    else server + "/deployments/" + appname + "?status=" + status
    |> get defaultTimeout parseDeploymentResponse

/// Performs a rollback of the given app on the server.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let rollbackTo server appname version = 
    put (server + "/deployments/" + appname + "?version=" + version) [||] defaultTimeout |> wrapFailure

/// Returns all active releases from the given server.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let getAllActiveReleases server = getReleasesFor server null "active" |> wrapFailure

/// Returns the active release of the given app from the given server.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let getActiveReleasesFor server appname = getReleasesFor server appname "active" |> wrapFailure

/// Returns all releases of the given app from the given server.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let getAllReleasesFor server appname = 
    if String.IsNullOrEmpty(appname) then server + "/deployments/"
    else server + "/deployments/" + appname + "/"
    |> get defaultTimeout parseDeploymentResponse
    |> wrapFailure

/// Returns all releases from the given server.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let getAllReleases server = getAllReleasesFor server null

/// Posts a deployment package to the given URL.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let postDeploymentPackage url packageFileName args = post url packageFileName args defaultTimeout |> wrapFailure


/// Posts a deployment package to the given URL, executes the script inside it with given arguments and handles the response.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let deployPackage (f : Deployment -> Deployment) =
    let d = f { defaultDeployment with 
                    AuthToken = 
                        match authToken with
                        | Some x -> Some ( AuthToken x)
                        | None -> None }
    authToken <- 
        match d.AuthToken with
        | Some a -> Some (match a with | AuthToken b -> b)
        | None -> None
    let result = post d.Url d.PackageFileName (d.Arguments |> Array.ofList) d.Timeout |> wrapFailure
    match result with
    | Success _ -> tracefn "Deployment of %s successful" d.PackageFileName
    | Failure exn -> failwithf "Deployment of %A failed\r\n%s" d.PackageFileName (buildExceptionString exn)
    | response -> failwithf "Deployment of %A failed\r\n%A" d.PackageFileName response

/// Posts a deployment package to the given URL, executes the script inside it with given arguments and handles the response.
/// Deprecated, use DeployPackage
[<Obsolete("Use deployPackage")>]
let DeployPackageWithArgs url packageFileName args = 
    deployPackage (fun x -> { x with Url = url; PackageFileName = packageFileName; Arguments = args |> List.ofArray })

/// Posts a deployment package to the given URL and handles the response.
/// Deprecated, use DeployPackage
[<Obsolete("Use deployPackage")>]
let DeployPackage url packageFileName = DeployPackageWithArgs url packageFileName [||]

/// Performs a rollback of the given app at the given URL and handles the response.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let RollbackPackage url appName version = 
    match rollbackTo url appName version with
    | Success _ -> tracefn "Rollback of %s to %s successful" appName version
    | Failure exn -> failwithf "Deployment of %s to %s failed\r\n%s" appName version (buildExceptionString exn)
    | response -> failwithf "Deployment of %s to %s failed\r\n%A" appName version response
