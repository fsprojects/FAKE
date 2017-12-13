namespace Fake.Net

open System
open System.IO
open System.Net.Http

open Fake.Core

open FilePath
open ResultBuilder

/// Contains 
module Http = 

    let result = ResultBuilder()

    let createUri (uriStr: string) = 
        try
            Ok (Uri uriStr)
        with
        | ex -> 
            let err = sprintf "[%s] %A" uriStr ex.Message
            Error [err ]

    let showDownloadResult (result: Result<FilePath, string list>) =
        match result with
        | Ok (FilePath(filePath)) -> 
            Trace.log <| sprintf "Downloaded : [%s]" filePath
        | Error errs -> 
            Trace.traceError  <| sprintf "Failed: %A" errs

    let saveStreamToFile (filePath: FilePath) (stream: Stream) : Async<Result<FilePath,string list>>  = 
        async {
            let filePathStr = FilePath.value filePath
            try
                use fileStream = new FileStream(filePathStr, FileMode.Create, FileAccess.Write, FileShare.None)
                do! stream.CopyToAsync(fileStream) |> Async.AwaitTask
                return (Ok filePath)
            with
            | ex -> 
                let err = sprintf "[%s] %A" filePathStr ex.Message
                return Error [err ]
        }

    let downloadToFileStream (filePath: FilePath) (uri:Uri) : Async<Result<FilePath,string list>>  = 
        async {
            use client = new HttpClient()
            try
                // do not buffer the response
                let! response = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead) |> Async.AwaitTask
                response.EnsureSuccessStatusCode () |> ignore
                use! stream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask 
                return! saveStreamToFile filePath stream  
            with
            | ex -> 
                let err = sprintf "[%s] %A" uri.Host ex.Message
                return Error [err ]
            }

    /// Download file by the given file path and Url
    /// string -> string -> Result<FilePath,string list>
    let downloadFile (filePathStr: string) (url: string) : Result<FilePath,string list> =

        let downloadResult = result {
            let! filePath = FilePath.create filePathStr
            let! uri = createUri url
            let! result =  downloadToFileStream filePath uri |> Async.RunSynchronously
            return result
        }
        do showDownloadResult downloadResult
        downloadResult
