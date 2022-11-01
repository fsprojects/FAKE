namespace Fake.DotNet.NuGet

/// <summary>
/// Contains types and tasks to interact with NuGet versions
/// </summary>
module Version =

    open Fake.Core
    open Fake.Net
    open System
    open System.Threading

    /// <summary>
    /// Holds data for search result of a NuGet package
    /// </summary>
    type NuGetSearchItemResult =
        {
            /// The package Id
            Id: string

            /// The package version
            Version: string

            /// The published date of the package
            Published: DateTime
        }

    /// Holds list of results of a NuGet search
    type NuGetSearchResult = { results: NuGetSearchItemResult list }

    /// The response type of NuGet search
    type NuGetSearchResponse = { d: NuGetSearchResult }

    /// NuGet version incrementer
    type NuGetVersionIncrement = SemVerInfo -> SemVerInfo

    /// <summary>
    /// Increment patch version
    /// </summary>
    ///
    /// <param name="v">The SemVer version to increment its patch component</param>
    let IncPatch: NuGetVersionIncrement =
        fun (v: SemVerInfo) ->
            { v with
                Build = 0I
                Patch = (v.Patch + 1u)
                Original = None }

    /// <summary>
    /// Increment minor version
    /// </summary>
    ///
    /// <param name="v">The SemVer version to increment its minor component</param>
    let IncMinor: NuGetVersionIncrement =
        fun (v: SemVerInfo) ->
            { v with
                Build = 0I
                Patch = 0u
                Minor = (v.Minor + 1u)
                Original = None }

    /// <summary>
    /// Increment major version
    /// </summary>
    ///
    /// <param name="v">The SemVer version to increment its major component</param>
    let IncMajor: NuGetVersionIncrement =
        fun (v: SemVerInfo) ->
            { v with
                Build = 0I
                Patch = 0u
                Minor = 0u
                Major = (v.Major + 1u)
                Original = None }

    /// <summary>
    /// Arguments for the next NuGet version number computing
    /// </summary>
    type NuGetVersionArg =
        {
            /// The NuGet server
            Server: string

            /// The package name
            PackageName: string

            /// The next version of the package after increment
            Increment: NuGetVersionIncrement

            /// The original/default version before increment
            DefaultVersion: string
        }

        /// Default arguments to compute next NuGet version number
        static member Default() =
            { Server = "https://www.nuget.org/api/v2"
              PackageName = ""
              Increment = IncMinor
              DefaultVersion = "1.0" }

    type internal NuGetLogger() =
        interface NuGet.Common.ILogger with
            member x.Log _ = () // printf "DEBUG: {data}".Dump();
            member x.Log(_, _) = () // printf "DEBUG: {data}".Dump();

            member x.LogAsync _ =
                System.Threading.Tasks.Task.FromResult 1 :> System.Threading.Tasks.Task // printf "DEBUG: {data}".Dump();

            member x.LogAsync(_, _) =
                System.Threading.Tasks.Task.FromResult 1 :> System.Threading.Tasks.Task // printf "DEBUG: {data}".Dump();

            member x.LogDebug _ = () // printf "DEBUG: {data}".Dump();
            member x.LogVerbose _ = () // $"VERBOSE: {data}".Dump();
            member x.LogInformation _ = () // $"INFORMATION: {data}".Dump();
            member x.LogMinimal _ = () // $"MINIMAL: {data}".Dump();
            member x.LogWarning(data) = eprintf "WARNING: %s" data
            member x.LogError(data) = eprintf "ERROR: %s" data
            member x.LogInformationSummary _ = () // eprintf "SUMMARY: %s" data

    open global.NuGet.Protocol.Core
    open global.NuGet.Protocol.Core.Types
    open global.NuGet.Protocol


    /// <summary>
    /// Retrieve current NuGet version number
    /// </summary>
    ///
    /// <param name="server">NuGet server</param>
    /// <param name="packageName">NuGet package name</param>
    let getLastNuGetVersion server (packageName: string) : SemVerInfo option =
        async {
            let logger = NuGetLogger()

            let providers = ResizeArray<Lazy<INuGetResourceProvider>>()

            providers.AddRange(Repository.Provider.GetCoreV3())
            let packageSource = NuGet.Configuration.PackageSource(server)

            let sourceRepository = SourceRepository(packageSource, providers)

            let! packageMetadataResource =
                sourceRepository.GetResourceAsync<PackageMetadataResource>() |> Async.AwaitTask

            let cacheContext = new SourceCacheContext()

            let! searchMetadata =
                packageMetadataResource.GetMetadataAsync(
                    packageName,
                    true,
                    true,
                    cacheContext,
                    logger,
                    CancellationToken.None
                )
                |> Async.AwaitTask

            return
                searchMetadata
                |> Seq.choose (fun m ->
                    try
                        Some <| SemVer.parse m.Identity.Version.OriginalVersion
                    with _ ->
                        None)
                |> Seq.sortByDescending (fun v -> v)
                |> Seq.tryHead
        }
        |> Async.RunSynchronously


    /// <summary>
    /// Compute next NuGet version number
    /// </summary>
    ///
    /// <param name="f">Function to override Nuget version parameters</param>
    let nextVersion (f: NuGetVersionArg -> NuGetVersionArg) =
        let arg = f (NuGetVersionArg.Default())

        match getLastNuGetVersion arg.Server arg.PackageName with
        | Some v -> (arg.Increment v).ToString()
        | None -> arg.DefaultVersion
