module Fake.DotNet.NuGet.Version

open Fake.DotNet.NuGet.NuGet
open Fake.Core
open Fake.Net
open Newtonsoft.Json
open System
open System.Threading
open System.Xml.Linq

type NuGetSearchItemResult =
    { Id:string
      Version:string
      Published:DateTime }
type NuGetSearchResult =
    { results:NuGetSearchItemResult list }
type NuGetSearchResponse =
    { d:NuGetSearchResult }
type NuGetVersionIncrement = SemVerInfo -> SemVerInfo

/// Increment patch version
let IncPatch:NuGetVersionIncrement =
    fun (v:SemVerInfo) ->
        { v with Build=0I; Patch=(v.Patch+1u) }

/// Increment minor version
let IncMinor:NuGetVersionIncrement =
    fun (v:SemVerInfo) ->
        { v with Build=0I; Patch=0u; Minor=(v.Minor+1u) }

/// Increment major version
let IncMajor:NuGetVersionIncrement =
    fun (v:SemVerInfo) ->
        { v with Build=0I; Patch=0u; Minor=0u; Major=(v.Major+1u) }

/// Arguments for the next NuGet version number computing
type NuGetVersionArg =
    { Server:string
      PackageName:string
      Increment:NuGetVersionIncrement
      DefaultVersion:string }
    /// Default arguments to compute next NuGet version number
    static member Default() =
        { Server="https://www.nuget.org/api/v2"
          PackageName=""
          Increment=IncMinor
          DefaultVersion="1.0" }

type internal NuGetLogger () =
    interface NuGet.Common.ILogger with
        member x.Log(data) = () // printf "DEBUG: {data}".Dump();
        member x.Log(level, date) = () // printf "DEBUG: {data}".Dump();
        member x.LogAsync(data) = System.Threading.Tasks.Task.FromResult 1 :> System.Threading.Tasks.Task // printf "DEBUG: {data}".Dump();
        member x.LogAsync(level, date) = System.Threading.Tasks.Task.FromResult 1 :> System.Threading.Tasks.Task // printf "DEBUG: {data}".Dump();
        member x.LogDebug(data) = () // printf "DEBUG: {data}".Dump();
        member x.LogVerbose(data) = () // $"VERBOSE: {data}".Dump();
        member x.LogInformation(data) = () // $"INFORMATION: {data}".Dump();
        member x.LogMinimal(data) = () // $"MINIMAL: {data}".Dump();
        member x.LogWarning(data) = eprintf "WARNING: %s" data
        member x.LogError(data) = eprintf "ERROR: %s" data
        member x.LogInformationSummary(data) = () // eprintf "SUMMARY: %s" data

open global.NuGet.Protocol.Core
open global.NuGet.Protocol.Core.Types
open global.NuGet.Protocol


/// Retrieve current NuGet version number
let getLastNuGetVersion server (packageName:string) : SemVerInfo option =
    async {
        let logger = new NuGetLogger()
        let providers = new ResizeArray<Lazy<NuGet.Protocol.Core.Types.INuGetResourceProvider>>()
        
        providers.AddRange(global.NuGet.Protocol.Core.Types.Repository.Provider.GetCoreV3())
        let packageSource = new NuGet.Configuration.PackageSource(server)
        let sourceRepository = new NuGet.Protocol.Core.Types.SourceRepository(packageSource, providers)
        let! packageMetadataResource =  sourceRepository.GetResourceAsync<NuGet.Protocol.Core.Types.PackageMetadataResource>() |> Async.AwaitTask
        let cacheContext = new SourceCacheContext()
        let! searchMetadata =  packageMetadataResource.GetMetadataAsync(packageName, true, true, cacheContext, logger, CancellationToken.None) |> Async.AwaitTask
        return
            searchMetadata
            |> Seq.choose (fun m -> try Some <| SemVer.parse m.Identity.Version.OriginalVersion with _ -> None)
            |> Seq.sortByDescending (fun v -> v)
            |> Seq.tryHead
    }
    |> Async.RunSynchronously


/// Compute next NuGet version number
let nextVersion (f : NuGetVersionArg -> NuGetVersionArg) =
    let arg = f (NuGetVersionArg.Default())
    match getLastNuGetVersion arg.Server arg.PackageName with
    | Some v -> (arg.Increment v).ToString()
    | None -> arg.DefaultVersion

