namespace Fake.Net

open System
open System.IO
open System.Net
open System.Net.Http
open Fake.Core
open Fake.Net.Async
open Fake.Net.Result
open Fake.Net.List
open System.Net.Http.Headers

/// <summary>
/// HTTP Client for downloading files
/// </summary>
module Http =

    /// <summary>
    /// Input parameter type
    /// </summary>
    type DownloadParameters =
        {
            /// The URI from which to download data
            Uri: string
            /// The name of the local file that is to receive the data
            Path: string
        }

    /// Type aliases for local file path and error messages
    type private FilePath = string
    type private Err = string

    /// Contains validated Uri and FilePath info for further download
    type private DownloadInfo = { Uri: Uri; LocalFilePath: FilePath }

    let private createFilePath (filePathStr: string) : Result<FilePath, Err list> =
        try
            let fullPath = Path.GetFullPath(filePathStr)
            Ok fullPath
        with ex ->
            let err = sprintf "[%s] %s" filePathStr ex.Message
            Error [ err ]

    let private createUri (uriStr: string) : Result<Uri, Err list> =
        try
            Ok(Uri uriStr)
        with ex ->
            let err = sprintf "[%s] %s" uriStr ex.Message
            Error [ err ]

    let private createDownloadInfo (input: DownloadParameters) : Result<DownloadInfo, Err list> =
        let (<!>) = Result.map
        let (<*>) = Result.apply

        let createDownloadInfoRecord (filePath: FilePath) (uri: Uri) = { Uri = uri; LocalFilePath = filePath }

        let filePathResult = createFilePath input.Path
        let urlResult = createUri input.Uri
        createDownloadInfoRecord <!> filePathResult <*> urlResult

    /// Unwraps the Result type and throws an exception if download process failed
    let private processResults result =
        match result with
        | Ok result ->
            Trace.log "Download succeeded"
            result
        | Error errs -> failwith <| sprintf "Download failed : [%A]" errs

    let private saveStreamToFileAsync (filePath: FilePath) (stream: Stream) : Async<Result<FilePath, Err list>> =
        async {
            try
                use fileStream =
                    new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)

                do! stream.CopyToAsync(fileStream) |> Async.AwaitTask
                return (Ok filePath)
            with ex ->
                let err = sprintf "[%s] %s" filePath ex.Message
                return Error [ err ]
        }

    let private downloadStreamToFileAsync (info: DownloadInfo) : Async<Result<FilePath, Err list>> =
        async {
            use client = new HttpClient()

            try
                Trace.log <| sprintf "Downloading [%s] ..." info.Uri.OriginalString
                // do not buffer the response
                let! response =
                    client.GetAsync(info.Uri, HttpCompletionOption.ResponseHeadersRead)
                    |> Async.AwaitTask

                response.EnsureSuccessStatusCode() |> ignore
                use! stream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                return! saveStreamToFileAsync info.LocalFilePath stream
            with ex ->
                let err = sprintf "[%s] %s" info.Uri.OriginalString ex.Message
                return Error [ err ]
        }

    let private downloadFileAsync (input: DownloadParameters) : Async<Result<FilePath, Err list>> =
        let valImp = createDownloadInfo input

        match valImp with
        | Ok x -> downloadStreamToFileAsync x
        | Error errs -> Async.result (Error errs)

    /// <summary>
    /// Download file by the given file path and Uri
    /// </summary>
    ///
    /// <param name="localFilePath">A local file path to download file</param>
    /// <param name="uri">A Uri to download from</param>
    /// <returns>String value contains a downloaded file path</returns>
    let downloadFile (localFilePath: string) (uri: string) : string =
        downloadFileAsync { Uri = uri; Path = localFilePath }
        |> Async.RunSynchronously
        |> processResults

    /// <summary>
    /// Download list of Uri's in parallel
    /// </summary>
    ///
    /// <param name="input">List of <c>Http.DownloadParameters</c>. Each Http.DownloadParameters record type contains
    /// Uri and file path</param>
    /// <returns>List of string values contains a list of downloaded files paths</returns>
    let downloadFiles (input: DownloadParameters list) : string list =
        input
        // DownloadParameters -> "Async<Result<FilePath, Err list>> list"
        |> List.map downloadFileAsync
        // "Async<Result<FilePath, Err list>> list" -> "Async<Result<FilePath, Err list> list>"
        |> List.sequenceAsyncA
        // "Async<Result<FilePath, Err list> list>" -> "Async<Result<FilePath list, Err list>>"
        |> Async.map List.sequenceResultA
        |> Async.RunSynchronously
        |> processResults

    /// <summary>
    /// Option type for the HTTP verb
    /// </summary>
    type PostMethod =
        | GET
        | POST

    /// <summary>
    /// Executes an HTTP GET command and retrieves the information.
    /// It returns the response of the request, or null if we got 404 or nothing.
    /// </summary>
    ///
    /// <param name="headerF">A function which allows to manipulate the HTTP headers.</param>
    /// <param name="userName">The username to use with the request.</param>
    /// <param name="password">The password to use with the request.</param>
    /// <param name="url">The URL to perform the GET operation.</param>
    let private getAsync headerF (userName: string) (password: string) (url: string) =
        async {
            use client = new HttpClient()

            if not (isNull userName) || not (isNull password) then
                let byteArray =
                    System.Text.Encoding.ASCII.GetBytes(sprintf "%s:%s" userName password)

                client.DefaultRequestHeaders.Authorization <-
                    AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray))

            let request = new HttpRequestMessage(HttpMethod.Get, url)
            headerF request.Headers
            let! response = client.SendAsync(request) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore

            let headers =
                response.Headers :> seq<System.Collections.Generic.KeyValuePair<string, seq<string>>>
                |> Seq.append (
                    response.Content.Headers :> seq<System.Collections.Generic.KeyValuePair<string, seq<string>>>
                )
                |> Seq.map (fun kv -> kv.Key, kv.Value |> Seq.toList)
                |> Map.ofSeq

            match response.StatusCode with
            | HttpStatusCode.NotFound
            | HttpStatusCode.NoContent -> return headers, ""
            | _ ->
                use! stream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                use reader = new StreamReader(stream)
                return headers, reader.ReadToEnd()
        }

    /// <summary>
    /// Executes an HTTP GET command and retrieves the information.
    /// It returns the response of the request, or null if we got 404 or nothing.
    /// </summary>
    ///
    /// <param name="userName">The username to use with the request.</param>
    /// <param name="password">The password to use with the request.</param>
    /// <param name="url">The URL to perform the GET operation.</param>
    let get userName password url : string =
        getAsync ignore userName password url |> Async.RunSynchronously |> snd

    /// <summary>
    /// Executes an HTTP POST command and retrieves the information.
    /// This function will automatically include a "source" parameter if the "Source" property is set.
    /// It returns the response of the request, or null if we got 404 or nothing.
    /// </summary>
    ///
    /// <param name="headerF">A function which allows to manipulate the HTTP headers.</param>
    /// <param name="url">The URL to perform the POST operation.</param>
    /// <param name="userName">The username to use with the request.</param>
    /// <param name="password">The password to use with the request.</param>
    /// <param name="data">The data to post.</param>
    let internal postCommandAsync headerF (url: string) userName password (data: string) =
        async {
            let client = new HttpClient()

            if not (String.IsNullOrEmpty userName) || not (String.IsNullOrEmpty password) then
                if String.IsNullOrEmpty userName || String.IsNullOrEmpty password then
                    invalidArg userName "You have to specify username and password for post operations."

                let byteArray =
                    System.Text.Encoding.ASCII.GetBytes(sprintf "%s:%s" userName password)

                client.DefaultRequestHeaders.Authorization <-
                    AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray))


            let request = new HttpRequestMessage(HttpMethod.Post, url)
            headerF request.Headers
            let bytes = System.Text.Encoding.UTF8.GetBytes data

            request.Content <- new ByteArrayContent(bytes, 0, bytes.Length)
            request.Content.Headers.ContentType <- MediaTypeHeaderValue("application/x-www-form-urlencoded")
            let! response = client.SendAsync(request) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore

            let headers =
                response.Headers :> seq<System.Collections.Generic.KeyValuePair<string, seq<string>>>
                |> Seq.append (
                    response.Content.Headers :> seq<System.Collections.Generic.KeyValuePair<string, seq<string>>>
                )
                |> Seq.map (fun kv -> kv.Key, kv.Value |> Seq.toList)
                |> Map.ofSeq

            use! stream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
            use reader = new StreamReader(stream)
            return headers, reader.ReadToEnd()
        }


    /// <summary>
    /// Executes an HTTP POST command and retrieves the information.
    /// This function will automatically include a "source" parameter if the "Source" property is set.
    /// It returns the response of the request, or null if we got 404 or nothing.
    /// </summary>
    ///
    /// <param name="headerF">A function which allows to manipulate the HTTP headers.</param>
    /// <param name="url">The URL to perform the POST operation.</param>
    /// <param name="userName">The username to use with the request.</param>
    /// <param name="password">The password to use with the request.</param>
    /// <param name="data">The data to post.</param>
    let postCommand headerF url userName password data : string =
        postCommandAsync headerF url userName password data
        |> Async.RunSynchronously
        |> snd


    /// <summary>
    /// Executes an HTTP POST command and retrieves the information.
    /// It returns the response of the request, or null if we got 404 or nothing.
    /// </summary>
    ///
    /// <param name="url">The URL to perform the POST operation.</param>
    /// <param name="userName">The username to use with the request.</param>
    /// <param name="password">The password to use with the request.</param>
    /// <param name="data">The data to post.</param>
    let post url userName password data =
        postCommand ignore url userName password data

    let internal uploadAsync (url: string) file =
        async {
            // See https://stackoverflow.com/questions/16416601/c-sharp-httpclient-4-5-multipart-form-data-upload
            use client = new HttpClient()
            let request = new HttpRequestMessage(HttpMethod.Post, url)

            use content =
                new MultipartFormDataContent(
                    "Upload----"
                    + DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture)
                )

            use fileStream = File.OpenRead(file)
            use streamContent = new StreamContent(fileStream)
            streamContent.Headers.ContentType <- MediaTypeHeaderValue.Parse("application/octet-stream")
            content.Add(streamContent, "file", file)
            request.Content <- content

            let! response = client.SendAsync(request) |> Async.AwaitTask

            response.EnsureSuccessStatusCode() |> ignore
        }

    /// <summary>
    /// Upload the given file to the given endpoint
    /// </summary>
    ///
    /// <param name="url">The URL to perform the POST operation.</param>
    /// <param name="file">The file to upload.</param>
    let upload url file =
        uploadAsync url file |> Async.RunSynchronously

    /// Like 'get' but allow to set headers and returns the response headers.
    ///
    /// <param name="userName">The username to use with the request.</param>
    /// <param name="password">The password to use with the request.</param>
    /// <param name="headerF">A function which allows to manipulate the HTTP headers.</param>
    /// <param name="url">The URL to perform the POST operation.</param>
    let getWithHeaders userName password headerF (url: string) : Map<string, string list> * string =
        getAsync headerF userName password url |> Async.RunSynchronously
