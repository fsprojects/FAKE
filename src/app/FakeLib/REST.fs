/// Contains functions to execute typical HTTP/REST calls.
module Fake.REST

open System
open System.IO
open System.Net
open System.Web
open System.Xml
open System.Text

/// Option type for the HTTP verb
type PostMethod = 
    | GET
    | POST

/// Executes an HTTP GET command and retrives the information.
/// It returns the response of the request, or null if we got 404 or nothing.
/// ## Parameters
///
///  - `userName` - The username to use with the request.
///  - `password` - The password to use with the request.
///  - `url` - The URL to perform the GET operation.
let ExecuteGetCommand (userName : string) (password : string) (url : string) = 
    use client = new WebClient()
    if userName <> null || password <> null then client.Credentials <- new NetworkCredential(userName, password)
    try 
        use stream = client.OpenRead(url)
        use reader = new StreamReader(stream)
        reader.ReadToEnd()
    with exn -> 
        // TODO: Handle HTTP 404 errors gracefully and return a null string to indicate there is no content.
        null

/// Executes an HTTP POST command and retrives the information.    
/// This function will automatically include a "source" parameter if the "Source" property is set.
/// It returns the response of the request, or null if we got 404 or nothing.
/// ## Parameters
///
///  - `headerF` - A function which allows to manipulate the HTTP headers.
///  - `url` - The URL to perform the POST operation.
///  - `userName` - The username to use with the request.
///  - `password` - The password to use with the request.
///  - `data` - The data to post.
let ExecutePostCommand headerF (url : string) userName password (data : string) = 
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

/// Executes an HTTP POST command and retrives the information.
/// It returns the response of the request, or null if we got 404 or nothing.
/// ## Parameters
///
///  - `url` - The URL to perform the POST operation.
///  - `userName` - The username to use with the request.
///  - `password` - The password to use with the request.
///  - `data` - The data to post.
let ExecutePost url userName password data = ExecutePostCommand ignore url userName password data
