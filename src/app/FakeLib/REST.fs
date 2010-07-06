module Fake.REST

open System
open System.IO
open System.Net
open System.Web
open System.Xml
open System.Text

type PostMethod =
| GET
| POST

/// <summary>Executes an HTTP GET command and retrives the information.</summary>    
/// <param name="userName">The username to use with the request</param>
/// <param name="password">The password to use with the request</param>
/// <param name="url">The URL to perform the GET operation</param>
/// <returns>The response of the request, or null if we got 404 or nothing.</returns>
let ExecuteGetCommand (userName:string) (password:string) (url:string) =
    use client = new WebClient()
    if userName <> null || password <> null then    
        client.Credentials <- new NetworkCredential(userName, password)
  
    try 
        use stream = client.OpenRead(url)
        use reader = new StreamReader(stream)
        reader.ReadToEnd()
    with 
    | exn ->
         // TODO: Handle HTTP 404 errors gracefully and return a null string to indicate there is no content.
         null

/// <summary>Executes an HTTP POST command and retrives the information.    
/// This function will automatically include a "source" parameter if the "Source" property is set.</summary>
/// <param name="headerF">The client information to perform the POST operation.</param>
/// <param name="url">The URL to perform the POST operation</param>
/// <param name="userName">The username to use with the request</param>
/// <param name="password">The password to use with the request</param>
/// <param name="data">The data to post</param>
/// <returns> The response of the request, or null if we got 404 or nothing.</returns>
let ExecutePostCommand headerF (url:string) userName password (data:string) =
    System.Net.ServicePointManager.Expect100Continue <- false 
    let request = WebRequest.Create url
    if String.IsNullOrEmpty userName || String.IsNullOrEmpty password then 
      invalidArg userName "You have to specify username and password for post operations."
    request.Credentials <- new NetworkCredential(userName, password)
    request.ContentType <- "application/x-www-form-urlencoded"
    request.Method <- "POST"

    headerF request.Headers

    let bytes = Encoding.UTF8.GetBytes data

    request.ContentLength <- int64 bytes.Length 
    use requestStream = request.GetRequestStream()
    requestStream.Write(bytes, 0, bytes.Length)

    use response = request.GetResponse()
    use reader = new StreamReader(response.GetResponseStream())
    reader.ReadToEnd()

let ExecutePost url userName = ExecutePostCommand ignore url userName