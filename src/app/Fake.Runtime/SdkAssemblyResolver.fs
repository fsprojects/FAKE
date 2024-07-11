module Fake.Runtime.SdkAssemblyResolver

open System
open System.IO
open System.Net
open System.Net.Http
open System.Threading
open System.Runtime.InteropServices
open Fake.Core
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.Runtime
open Newtonsoft.Json
open Paket
open Microsoft.Deployment.DotNet.Releases

/// here we will pin Fake runner execution framework to .NET 6 as in `SdkVersion`
/// We will also try to resolve the current SDK that the runner is executing, if it is the same as pinned
/// one then we will use runtime assemblies from that SDK version on its installation on disk. Otherwise,
/// we will default to NetStandard2.0 assemblies. We will download them since they are packaged in a NuGet
/// package extract them a and reference them.
/// the resolution of runtime version for the selected SDK is as follows; we will use the dotnet official release
/// package to get the releases for pinned framework version, and get the runtime. If the accessing the network
/// is not possible, then we will use a cached releases file.

type SdkAssemblyResolver(logLevel: Trace.VerboseLevel) =

    // following environment variables are used in testing for different scenarios that the SDK resolver
    // could encounter, they are not intended to be used other than that!
    let CustomDotNetHostPath =
        Environment.environVarOrDefault "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH" ""

    let RuntimeResolverResolveMethod =
        Environment.environVarOrDefault "FAKE_SDK_RESOLVER_RUNTIME_VERSION_RESOLVE_METHOD" ""

    // Defaults still .NET 6.0 but could be overriden with .NET 8.0 or even comma-separated "6.0,8.0"
    let RuntimeAssemblyVersions =
        let versions =
            Environment.environVarOrDefault "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_VERSION" "6.0"

        versions.Split([| ','; ';' |]) |> Array.toList

    member this.LogLevel = logLevel

    member this.SdkVersionRaws = RuntimeAssemblyVersions

    member this.SdkVersions =
        RuntimeAssemblyVersions |> List.map (fun v -> ReleaseVersion(v + ".0"))

    member this.PaketFrameworkIdentifiers =
        this.SdkVersions
        |> List.map (fun thisSdk ->
            FrameworkIdentifier.DotNetFramework(FrameworkVersion.TryParse(thisSdk.Major.ToString()).Value))

    member this.SdkVersionRaw = RuntimeAssemblyVersions |> Seq.head
    member this.SdkVersion = this.SdkVersions |> Seq.head
    member this.PaketFrameworkIdentifier = this.PaketFrameworkIdentifiers |> Seq.head

    member this.SdkVersionFromGlobalJson = DotNet.tryGetSDKVersionFromGlobalJson ()

    member this.IsSdkVersionFromGlobalJsonSameAsSdkVersion() =
        match this.SdkVersionFromGlobalJson with
        | Some version ->
            this.SdkVersions
            |> List.exists (fun thisSdk -> ReleaseVersion(version).Major.Equals thisSdk.Major)
        | None -> false

    member this.DotNetBinaryName = if Environment.isUnix then "dotnet" else "dotnet.exe"

    /// <summary>
    /// provides the path to the `dotnet` binary running this library, respecting various dotnet <see href="https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_root-dotnet_rootx86%5D">environment variables</see>.
    /// Also probes the PATH and checks the default installation locations
    /// </summary>
    member this.ResolveDotNetRoot() =
        let isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        let isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
        let isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        let isUnix = isLinux || isMac

        let potentialDotnetHostEnvVars =
            [ "DOTNET_HOST_PATH", id // is a full path to dotnet binary
              "DOTNET_ROOT", (fun s -> Path.Combine(s, this.DotNetBinaryName)) // needs dotnet binary appended
              "DOTNET_ROOT(x86)", (fun s -> Path.Combine(s, this.DotNetBinaryName)) ] // needs dotnet binary appended

        let existingEnvVarValue envVarValue =
            match envVarValue with
            | null
            | "" -> None
            | other -> Some other

        let tryFindFromEnvVar () =
            potentialDotnetHostEnvVars
            |> List.tryPick (fun (envVar, transformer) ->
                match Environment.GetEnvironmentVariable envVar |> existingEnvVarValue with
                | Some varValue -> Some(transformer varValue |> FileInfo)
                | None -> None)

        let PATHSeparator = if isUnix then ':' else ';'

        /// Fully resolve the symlink, returning a fully-resolved FileInfo
        /// that is not a symlink,
        /// or returning None if the file or any of its resolved targets
        /// does not exist.
        let rec resolveFile (fi: System.IO.FileInfo) : FileInfo option =
            if not fi.Exists then None
            elif isNull fi.LinkTarget then Some fi
            else resolveFile (System.IO.FileInfo fi.LinkTarget)

        let tryFindFromPATH () =
            Environment
                .GetEnvironmentVariable("PATH")
                .Split(PATHSeparator, StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
            |> Array.tryPick (fun d ->
                let fi = Path.Combine(d, this.DotNetBinaryName) |> FileInfo

                if fi.Exists then Some fi else None)

        let tryFindFromDefaultDirs () =
            let windowsPath = $"C:\\Program Files\\dotnet\\{this.DotNetBinaryName}"
            let macosPath = $"/usr/local/share/dotnet/{this.DotNetBinaryName}"
            let linuxPath = $"/usr/share/dotnet/{this.DotNetBinaryName}"

            let tryFindFile p =
                let f = FileInfo p

                if f.Exists then Some f else None

            if isWindows then tryFindFile windowsPath
            else if isMac then tryFindFile macosPath
            else if isLinux then tryFindFile linuxPath
            else None

        if not (String.isNullOrEmpty CustomDotNetHostPath) then
            Some CustomDotNetHostPath
        else
            tryFindFromEnvVar ()
            |> Option.orElseWith tryFindFromPATH
            |> Option.orElseWith tryFindFromDefaultDirs
            |> Option.bind resolveFile
            |> Option.map (fun dotnetRoot -> dotnetRoot.Directory.FullName)

    member this.TryResolveSdkRuntimeVersionFromNetwork() =
        if this.LogLevel.PrintVerbose then
            Trace.tracefn "Trying to resolve runtime version from network.."

        try
            let sdkVersionReleases =
                ProductCollection.GetAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> List.ofSeq
                |> List.find (fun product ->
                    this.SdkVersionRaws
                    |> List.exists (fun raws -> product.ProductVersion.Equals raws))

            sdkVersionReleases.GetReleasesAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> List.ofSeq
            |> Some
        with ex ->
            Trace.traceError $"Could not get SDK runtime version from network due to: {ex.Message}"
            None

    member this.TryResolveSdkRuntimeVersionFromCache() =
        if this.LogLevel.PrintVerbose then
            Trace.tracefn "Trying to resolve runtime version from cache.."

        try
            System.Reflection.Assembly.GetExecutingAssembly().Location
            |> Path.GetDirectoryName
            </> "cachedDotnetSdkReleases.json"
            |> Product.GetReleasesAsync
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> List.ofSeq
            |> Some
        with ex ->
            Trace.traceError $"Could not get SDK runtime version from cache due to: {ex.Message}"
            None

    member this.GetProductReleaseForSdk(version: ReleaseVersion) =
        let net60releases =
            if RuntimeResolverResolveMethod = "cache" then
                // for testing only!
                this.TryResolveSdkRuntimeVersionFromCache()
            else
                // this is the default case, we will try the network, if we could not, then we will reach for cached file.
                this.TryResolveSdkRuntimeVersionFromNetwork()
                |> Option.orElseWith (this.TryResolveSdkRuntimeVersionFromCache)

        let sdkRelease (release: ProductRelease) =
            release.Sdks
            |> List.ofSeq
            |> List.exists (fun sdk -> sdk.Version.Equals(version))

        net60releases |> Option.bind (List.tryFind sdkRelease)

    member this.ResolveSdkRuntimeVersion() =
        let versionOptions (options: DotNet.VersionOptions) =
            // If a custom CLI path is provided, configure the version command
            // to use that path.  This really only accomodates a test scenarios
            // in which FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH is set.
            match this.ResolveDotNetRoot() with
            | Some root ->
                options.WithCommon(fun common -> { common with DotNetCliPath = root </> this.DotNetBinaryName })
            | None -> options

        let sdkVersion = DotNet.getVersion versionOptions |> ReleaseVersion

        match this.GetProductReleaseForSdk sdkVersion with
        | Some release ->
            let version = release.Runtime.Version.ToString()

            if this.LogLevel.PrintVerbose then
                Trace.trace $"Resolved runtime version: {version}"

            version

        | None ->
            failwithf $"Could not find a suitable .NET 6 runtime version matching SDK version: {sdkVersion.ToString()}"

    member this.SdkReferenceAssemblies() =

        let referenceAssembliesPaths =
            this.SdkVersionRaws
            |> List.choose (fun rawVersion ->
                this.ResolveDotNetRoot()
                |> Option.map (fun dotnetRoot ->
                    dotnetRoot
                    </> "packs"
                    </> "Microsoft.NETCore.App.Ref"
                    </> this.ResolveSdkRuntimeVersion()
                    </> "ref"
                    </> "net" + rawVersion))

        if Seq.isEmpty referenceAssembliesPaths then
            failwithf "Could not find referenced assemblies, please check installed SDK and runtime versions"
        else
            if this.LogLevel.PrintVerbose then
                let paths = String.Join(",", referenceAssembliesPaths)
                Trace.tracefn $"Resolved referenced SDK paths: {paths}"

            let referenceAssembliesPath =
                referenceAssembliesPaths
                |> List.tryFind (fun referenceAssembliesPath -> Directory.Exists referenceAssembliesPath)

            match referenceAssembliesPath with
            | Some pathFound -> Directory.GetFiles(pathFound, "*.dll") |> Seq.toList
            | None ->
                let paths = String.Join(",", referenceAssembliesPaths)

                failwithf
                    "Could not find referenced assemblies in path: '%s', please check installed SDK and runtime versions"
                    paths

    member this.NetStandard20ReferenceAssemblies
        (
            nuGetPackage: string,
            version: string,
            pathSuffix: string,
            groupName: Domain.GroupName,
            paketDependenciesFile: Lazy<Paket.DependenciesFile>
        ) =
        // We need to use "real" reference assemblies as using the currently running runtime assemblies doesn't work:
        // see https://github.com/fsharp/FAKE/pull/1695

        // Therefore we download the reference assemblies (the NETStandard.Library or Microsoft.NETCore.App.Ref package)
        // and add them in addition to what we have resolved,
        // we use the sources in the paket.dependencies to give the user a chance to overwrite.

        // Note: This package/version needs to updated together with our "framework" variable below and needs to
        // be compatible with the runtime we are currently running on.
        let rootDir = Directory.GetCurrentDirectory()
        let packageName = Domain.PackageName(nuGetPackage)
        let version = SemVer.Parse(version)

        let existingPkg = NuGetCache.GetTargetUserNupkg packageName version

        let extractedFolder =
            match File.Exists existingPkg with
            | true ->
                // Shortcut in order to prevent requests to nuget sources if we have it downloaded already
                Path.GetDirectoryName existingPkg
            | false ->
                let sources = paketDependenciesFile.Value.Groups.[groupName].Sources

                let versions =
                    Paket.NuGet.GetVersions
                        false
                        None
                        rootDir
                        (PackageResolver.GetPackageVersionsParameters.ofParams sources groupName packageName)
                    |> Async.RunSynchronously
                    |> dict

                let source =
                    match versions.TryGetValue(version) with
                    | true, v when v.Length > 0 -> v |> Seq.head
                    | _ ->
                        failwithf
                            "Could not find package '%A' with version '%A' in any package source of group '%A', but fake needs this package to compile the script"
                            packageName
                            version
                            groupName

                let _, extractedFolder =
                    Paket.NuGet.DownloadAndExtractPackage(
                        None,
                        rootDir,
                        false,
                        PackagesFolderGroupConfig.NoPackagesFolder,
                        source,
                        [],
                        Paket.Constants.MainDependencyGroup,
                        packageName,
                        version,
                        PackageResolver.ResolvedPackageKind.Package,
                        false,
                        false,
                        false,
                        false
                    )
                    |> Async.RunSynchronously

                extractedFolder

        let sdkDir = Path.Combine(extractedFolder, pathSuffix)

        Directory.GetFiles(sdkDir, "*.dll") |> Seq.toList

    member this.ResolveSdkReferenceAssemblies
        (
            groupName: Domain.GroupName,
            paketDependenciesFile: Lazy<Paket.DependenciesFile>
        ) =
        if this.LogLevel.PrintVerbose then
            let versions =
                String.Join(", and", this.SdkVersions |> List.map (fun v -> $" .Net{v.Major}"))

            Trace.tracefn $"Using{versions} assemblies"

        this.SdkReferenceAssemblies()
