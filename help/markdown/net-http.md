# Downloading Files Over HTTP

The `Fake.Net.Http` module provides a functionality to download files over HTTP.

[API-Reference](apidocs/v5/fake-net-http.html)

## Including the Fake.Net.Http dependency

In order to open the `Fake.Net.Http` module from a build script you have to add a `Fake.Net.Http` dependency into your
`paket.dependencies` file:

```
group NetcoreBuild
    source https://api.nuget.org/v3/index.json

    nuget Fake.Net.Http prerelease

    .... other dependencies, like
    nuget Fake.Core.Target prerelease
```

Please see more details on referencing FAKE 5 modules [here](fake-fake5-modules.html).

## Downloading a Single File

To download a single file over HTTP use `downloadFile` from the Http module:

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.Net.Http //"
open Fake.Net
open Fake.Core

Target.create "DownloadFile" (fun _ ->
    let absoluteFilePath = Http.downloadFile "/tmp/5.zip" @"http://ipv4.download.thinkbroadband.com/5MB.zip"
    printfn "File path: %s" absoluteFilePath
)
```

A console output should be:

    Downloading [http://ipv4.download.thinkbroadband.com/5MB.zip] ...
    Download succeeded
    File path: /tmp/5.zip

## Downloading Multiple Files

To download multiple files in parallel use `downloadFiles` from the Http module:

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.Net.Http //"
open Fake.Net
open Fake.Core

Target.create "DownloadFiles" (fun _ ->
    let files: Http.DownloadParameters list = [
                {Path = "/tmp/5.zip"; Uri = "http://ipv4.download.thinkbroadband.com/5MB.zip"};
                {Path = "/tmp/10.zip"; Uri = "http://ipv4.download.thinkbroadband.com/10MB.zip"}]
    let filePaths = Http.downloadFiles files
    printfn "File paths: %A" filePaths
)
```

A console output should be:

    Downloading [http://ipv4.download.thinkbroadband.com/5MB.zip] ...
    Downloading [http://ipv4.download.thinkbroadband.com/10MB.zip] ...
    Download succeeded
    File paths: ["/tmp/5.zip"; "/tmp/10.zip"]

## More Details

* `downloadFile` and `downloadFiles` throw an Exception and fail a FAKE Target if any error occurs (invalid URI, invalid local file
   path/permissions, etc.)

* file with the same name will be overwritten if exists in the target location
