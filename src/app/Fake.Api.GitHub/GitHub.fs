namespace Fake.Api

open Octokit
open Octokit.Internal
open System
open System.Threading
open System.Net.Http
open System.Reflection
open System.IO

/// Contains tasks to interact with [GitHub](https://github.com/) releases
module GitHub =

    [<NoComparison>]
    type Release =
        { Client : GitHubClient
          Owner : string
          RepoName : string
          Release : Octokit.Release }

    type CreateReleaseParams =
        { /// The name of the release
          Name : string 
          /// The text describing the contents of the release
          Body : string
          /// Indicates whether the release will be created as a draft
          Draft : bool 
          /// Indicates whether the release will be created as a prerelease
          Prerelease : bool }      

    // wrapper re-implementation of HttpClientAdapter which works around
    // known Octokit bug in which user-supplied timeouts are not passed to HttpClient object
    // https://github.com/octokit/octokit.net/issues/963
    type private HttpClientWithTimeout(timeout : TimeSpan) as this =
        inherit HttpClientAdapter(fun () -> HttpMessageHandlerFactory.CreateDefault())
        let setter = lazy(
            match typeof<HttpClientAdapter>.GetField("_http", BindingFlags.NonPublic ||| BindingFlags.Instance) with
            | null -> ()
            | f ->
                match f.GetValue(this) with
                | :? HttpClient as http -> http.Timeout <- timeout
                | _ -> ())

        interface IHttpClient with
            member __.Send(request : IRequest, ct : CancellationToken) =
                setter.Force()
                match request with :? Request as r -> r.Timeout <- timeout | _ -> ()
                base.Send(request, ct)

    /// A version of 'reraise' that can work inside computation expressions
    let private captureAndReraise ex =
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw()
        Unchecked.defaultof<_>

    /// Retry the Octokit action count times
    let rec private retry count asyncF =
        async {
            try
                return! asyncF
            with ex ->
                return!
                    match (ex, ex.InnerException) with
                    | (:? AggregateException, (:? AuthorizationException as ex)) -> captureAndReraise ex
                    | _ when count > 0 -> retry (count - 1) asyncF
                    | (ex, _) -> captureAndReraise ex
        }

    /// Retry the Octokit action count times after input succeed
    let private retryWithArg count input asycnF =
        async {
            let! choice = input |> Async.Catch
            match choice with
            | Choice1Of2 input' ->
                return! (asycnF input') |> retry count
            | Choice2Of2 ex ->
                return captureAndReraise ex
        }

    /// Creates a GitHub API v3 client using the specified credentials    
    let CreateClient user password =
        async {
            let httpClient = new HttpClientWithTimeout(TimeSpan.FromMinutes 20.)
            let connection = Connection(ProductHeaderValue("FAKE"), httpClient)
            let github = GitHubClient(connection)
            github.Credentials <- Credentials(user, password)
            return github
        }

    /// Creates a GitHub API v3 client using the specified token
    let CreateClientWithToken token =
        async {
            let httpClient = new HttpClientWithTimeout(TimeSpan.FromMinutes 20.)
            let connection = Connection(ProductHeaderValue("FAKE"), httpClient)
            let github = GitHubClient(connection)
            github.Credentials <- Credentials(token)
            return github
        }

    /// Creates a GitHub API v3 client to GitHub Enterprise server at the specified url using the specified credentials
    let CreateGHEClient url user password =
        async {
            let credentials = Credentials(user, password)
            let httpClient = new HttpClientWithTimeout(TimeSpan.FromMinutes 20.)
            let connection = Connection(ProductHeaderValue("FAKE"), Uri(url), InMemoryCredentialStore(credentials), httpClient, SimpleJsonSerializer())
            let github = GitHubClient(connection)
            github.Credentials <- credentials
            return github
        }

    /// Creates a GitHub API v3 client to GitHub Enterprise server at the specified url using the specified token
    let CreateGHEClientWithToken url token =
        async {
            let credentials = Credentials(token)
            let httpClient = new HttpClientWithTimeout(TimeSpan.FromMinutes 20.)
            let connection = Connection(ProductHeaderValue("FAKE"), Uri(url), InMemoryCredentialStore(credentials), httpClient, SimpleJsonSerializer())
            let github = GitHubClient(connection)
            github.Credentials <- credentials
            return github
        }

    /// Creates a GitHub Release for the specified repository and tag name
    /// ## Parameters
    /// - `owner` - the repository's owner
    /// - `repoName` - the repository's name
    /// - `tagName` - the name of the tag to use for this release
    /// - `setParams` - function used to override the default release parameters
    /// - `client` - GitHub API v3 client
    let CreateRelease owner repoName tagName setParams (client : Async<GitHubClient>) =    
        retryWithArg 5 client <| fun client' -> async {
            let p = 
                { Name = tagName
                  Body = ""
                  Draft = true 
                  Prerelease = false } |> setParams
            
            let data = NewRelease(tagName)
            data.Name <- p.Name
            data.Body <- p.Body
            data.Draft <- p.Draft
            data.Prerelease <- p.Prerelease

            let! release = Async.AwaitTask <| client'.Repository.Release.Create(owner, repoName, data)

            let draftWord = if data.Draft then " draft" else ""

            printfn "Created%s release id %d" draftWord release.Id

            return {
                Client = client'
                Owner = owner
                RepoName = repoName
                Release = release }
        }

    /// Creates a draft GitHub Release for the specified repository and tag name
    /// ## Parameters
    /// - `owner` - the repository's owner
    /// - `repoName` - the repository's name
    /// - `tagName` - the name of the tag to use for this release
    /// - `prerelease` - indicates whether the release will be created as a prerelease
    /// - `notes` - collection of release notes that will be inserted into the Body of the release
    /// - `client` - GitHub API v3 client
    let CreateDraftWithNotes owner repoName tagName prerelease (notes : seq<string>) client =
        let setParams p = 
            { p with 
                Body = String.Join(Environment.NewLine, notes) 
                Prerelease = prerelease }
        CreateRelease owner repoName tagName setParams client

    /// Uploads and attaches the specified file to the specified release
    let UploadFile fileName (release : Async<Release>) =
        retryWithArg 5 release <| fun release' -> async {
            let fi = FileInfo(fileName)
            let archiveContents = File.OpenRead(fi.FullName)
            let assetUpload = ReleaseAssetUpload(fi.Name,"application/octet-stream",archiveContents,Nullable<TimeSpan>())
            let! asset = Async.AwaitTask <| release'.Client.Repository.Release.UploadAsset(release'.Release, assetUpload)
            printfn "Uploaded %s" asset.Name
            return release'
        }

    /// Uploads and attaches the specified files to the specified release
    let UploadFiles fileNames (release : Async<Release>) = async {
        let! release' = release
        let releaseW = async { return release' }
        let! _ = Async.Parallel [for f in fileNames -> UploadFile f releaseW ]
        return release'
    }

    /// Publishes the specified release by removing its Draft status
    let ReleaseDraft (release : Async<Release>) =
        retryWithArg 5 release <| fun release' -> async {
            let update = release'.Release.ToUpdate()
            update.Draft <- Nullable<bool>(false)
            let! released = Async.AwaitTask <| release'.Client.Repository.Release.Edit(release'.Owner, release'.RepoName, release'.Release.Id, update)
            printfn "Released %d on GitHub" released.Id
        }

    /// Gets the latest release for the specified repository
    let GetLastRelease owner repoName (client : Async<GitHubClient>) =
        retryWithArg 5 client <| fun client' -> async {
            let! release = Async.AwaitTask <| client'.Repository.Release.GetLatest(owner, repoName)

            printfn "Latest release id: %d" release.Id
            printfn "Latest release tag: %s" release.TagName
            printfn "Latest release assets: %d" (Seq.length release.Assets)

            return {
                Client = client'
                Owner = owner
                RepoName = repoName
                Release = release }
        }

    /// Gets release with the specified tag for the specified repository
    let GetReleaseByTag (owner:string) repoName tagName (client : Async<GitHubClient>) =
        retryWithArg 5 client <| fun client' -> async {
            let! releases = client'.Repository.Release.GetAll(owner, repoName) |> Async.AwaitTask
            let matches = releases |> Seq.filter (fun (r: Octokit.Release) -> r.TagName = tagName)

            if Seq.isEmpty matches then
                failwithf "Unable to locate tag %s" tagName

            let release = matches |> Seq.head

            printfn "Release id: %d" release.Id
            printfn "Release tag: %s" release.TagName
            printfn "Release assets: %d" (Seq.length release.Assets)

            return {
                Client = client'
                Owner = owner
                RepoName = repoName
                Release = release }
        }

    /// Downloads the asset with the specified id to the specified destination
    let DownloadAsset id destination (release : Async<Release>) =
        retryWithArg 5 release <| fun release' -> async {
            let! asset = Async.AwaitTask <| release'.Client.Repository.Release.GetAsset(release'.Owner,release'.RepoName,id)
            let! resp = Async.AwaitTask <| release'.Client.Connection.Get(Uri(asset.Url), new System.Collections.Generic.Dictionary<string,string>(),"application/octet-stream")

            let bytes = resp.HttpResponse.Body :?> byte[]
            let filename = Path.Combine(destination, asset.Name)

            File.WriteAllBytes(filename, bytes)

            printfn "Downloaded %s" filename
        }

    /// Downloads all assets for the specified release to the specified destination
    let DownloadAssets destination (release : Async<Release>) = async {
        let! release' = release
        let releaseW = async { return release' }

        let! _ = Async.Parallel [for f in release'.Release.Assets -> DownloadAsset f.Id destination releaseW ]

        ()
    }
