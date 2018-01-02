namespace Fake.Net

open System
open System.IO
open System.Net.Http

open Fake.Core

open Fake.Net.Async
open Fake.Net.Result
open Fake.Net.List

/// HTTP Client for downloading files
module Http = 

    /// Input parameter type
    type DownloadParameters = {
        Uri: string
        Path: string
    }

    /// Type aliases for local file path and error messages
    type private FilePath = string
    type private Err = string

    /// Contains validated Uri and FilePath info for further download
    type private DownloadInfo = {
        Uri: Uri
        LocalFilePath: FilePath
    }

    /// [omit]
    let private createFilePath (filePathStr: string): Result<FilePath, Err list>  = 
        try
            let fullPath = Path.GetFullPath(filePathStr)
            Ok (fullPath)
        with
        | ex -> 
            let err = sprintf "[%s] %s" filePathStr ex.Message
            Error [err ]

    /// [omit]
    let private createUri (uriStr: string): Result<Uri, Err list> = 
        try
            Ok (Uri uriStr)
        with
        | ex -> 
            let err = sprintf "[%s] %s" uriStr ex.Message
            Error [err ]

    /// [omit]
    let private createDownloadInfo (input: DownloadParameters): Result<DownloadInfo, Err list> = 
        let (<!>) = Result.map
        let (<*>) = Result.apply

        let createDownloadInfoRecord (filePath: FilePath) (uri:Uri)  = 
            { Uri=uri;  LocalFilePath=filePath }

        let filePathResult = createFilePath input.Path
        let urlResult = createUri input.Uri
        createDownloadInfoRecord <!> filePathResult <*> urlResult
       
    /// [omit]
    let private printDownloadResults result =
        match result with
            | Ok result -> 
                Trace.log <| sprintf "Downloaded : [%A]" result
            | Error errs -> 
                Trace.traceError <| sprintf "Failed: %A" errs
        result

    /// [omit]
    let private saveStreamToFileAsync (filePath: FilePath) (stream: Stream) : Async<Result<FilePath, Err list>> =
        async {
            try
                use fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)
                do! stream.CopyToAsync(fileStream) |> Async.AwaitTask
                return (Ok filePath)
            with
            | ex -> 
                let err = sprintf "[%s] %s" filePath ex.Message
                return Error [err ]
        }

    /// [omit]
    let private downloadStreamToFileAsync (info: DownloadInfo) : Async<Result<FilePath, Err list>>  =
        async {
            use client = new HttpClient()
            try
                Trace.log <| sprintf "Downloading [%s] ..." info.Uri.OriginalString
                // do not buffer the response
                let! response = client.GetAsync(info.Uri, HttpCompletionOption.ResponseHeadersRead) |> Async.AwaitTask
                response.EnsureSuccessStatusCode () |> ignore
                use! stream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                return! saveStreamToFileAsync info.LocalFilePath stream
            with
            | ex ->
                let err = sprintf "[%s] %s" info.Uri.Host ex.Message
                return Error [err ]
            }

    /// [omit]     
    let private downloadFileAsync (input: DownloadParameters): Async<Result<FilePath, Err list>> =
        let valImp = createDownloadInfo input
        match valImp with
            | Ok x ->
                downloadStreamToFileAsync x
            | Error errs ->
                Async.result (Error errs)
        
    /// Download file by the given file path and Uri
    /// string -> string -> Result<FilePath,string list>
    /// ## Parameters
    ///  - `localFilePath` - A local file path to download file
    ///  - `uri` - A Uri to download from
    /// ## Returns
    ///  - `Result` type. Success branch contains a downloaded file path. Failure branch contains a list of errors
    let downloadFile (localFilePath: string) (uri: string) : Result<string, string list> =
        downloadFileAsync { Uri=uri;  Path=localFilePath }
        |> Async.RunSynchronously
        |> printDownloadResults

    /// Download list of Uri's in parallel
    /// DownloadParameters -> Result<FilePath, Err list>
    /// ## Parameters
    ///  - `input` - List of Http.DownloadParameters. Each Http.DownloadParameters record type contains Uri and file path
    /// ## Returns
    ///  - `Result` type. Success branch contains a list of downloaded file paths. Failure branch contains a list of errors
    let downloadFiles (input: DownloadParameters list) : Result<string list, string list> =
        input
        // DownloadParameters -> "Async<Result<FilePath, Err list>> list"
        |> List.map downloadFileAsync
        // "Async<Result<FilePath, Err list>> list" -> "Async<Result<FilePath, Err list> list>"
        |> List.sequenceAsyncA
        // "Async<Result<FilePath, Err list> list>" -> "Async<Result<FilePath list, Err list>>"
        |> Async.map List.sequenceResultA
        |> Async.RunSynchronously
        |> printDownloadResults