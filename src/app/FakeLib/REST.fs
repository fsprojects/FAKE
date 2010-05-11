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

/// Executes an HTTP GET command and retrives the information.    
///   param userName: The username to use with the request
///   param password: The password to use with the request
///   param url: The URL to perform the GET operation  
///   returns: The response of the request, or null if we got 404 or nothing.
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

/// Executes an HTTP POST command and retrives the information.    
/// This function will automatically include a "source" parameter if the "Source" property is set.
///   param headerF: The client information to perform the POST operation  
///   param url: The URL to perform the POST operation
///   param userName: The username to use with the request
///   param password: The password to use with the request
///   param data: The data to post
///   returns: The response of the request, or null if we got 404 or nothing.
let ExecutePostCommand headerF (url:string) userName password (data:string) =
  System.Net.ServicePointManager.Expect100Continue <- false 
  let request = WebRequest.Create(url)    
  if String.IsNullOrEmpty userName || String.IsNullOrEmpty password then 
    invalidArg userName "You have to specify username and password for post operations."
  request.Credentials <- new NetworkCredential(userName, password)
  request.ContentType <- "application/x-www-form-urlencoded"
  request.Method <- "POST"

  headerF request.Headers

  let bytes = Encoding.UTF8.GetBytes(data)

  request.ContentLength <- (int64)bytes.Length 
  use requestStream = request.GetRequestStream()
  requestStream.Write(bytes, 0, bytes.Length)
  try
    use response = request.GetResponse()
    use reader = new StreamReader(response.GetResponseStream())
    reader.ReadToEnd()
  with
  | :? WebException as ex -> 
      Diagnostics.Trace.WriteLine ex.Message
      raise ex

 /// Gets the result as xml
let GetAsXML output =
    if isNullOrEmpty output then null else
    let xmlDocument = new XmlDocument()
    xmlDocument.LoadXml output
    xmlDocument

let ExecutePost url userName = ExecutePostCommand ignore url userName