module Fake.Runtime.SdkAssemblyResolver

open System.IO
open Fake.Core
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.Runtime
open Paket
open Microsoft.Deployment.DotNet.Releases

/// here we will pin Fake runner execution framework to .NET 6 as in `SdkVersion`
/// We will also try to resolve the current SDK that the runner is executing, if it is the same as pinned
/// one then we will use runtime assemblies from that SDK version on its installation on disk. Otherwise,
/// we will default to NetStandard2.0 assemblies. We will download them since they are packaged in a NuGet
/// package extract them a and reference them.
type SdkAssemblyResolver() =
#if DOTNETCORE
    let CustomDotNetHostPath = Environment.environVarOrDefault "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH" ""

    member this.SdkVersionRaw = "6.0"

    member this.SdkVersion = ReleaseVersion("6.0.0")

    member this.PaketFrameworkIdentifier =
        FrameworkIdentifier.DotNetFramework (
            FrameworkVersion.TryParse(this.SdkVersion.Major.ToString()).Value
        )

    member this.SdkVersionFromGlobalJson = DotNet.tryGetSDKVersionFromGlobalJson ()

    member this.IsSdkVersionFromGlobalJsonSameAsSdkVersion() =
        match this.SdkVersionFromGlobalJson with
        | Some version -> ReleaseVersion(version).Major.Equals(this.SdkVersion.Major)
        | None -> false

    member this.ResolveSdkRuntimeVersion() =
        let resolvedSdkVersion =
            this.SdkVersionFromGlobalJson
            |> Option.get
            |> ReleaseVersion

        let sdkVersionReleases =
            ProductCollection.GetAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> List.ofSeq
            |> List.find (fun product -> product.ProductVersion.Equals(this.SdkVersionRaw))

        let sdkVersionRelease =
            sdkVersionReleases.GetReleasesAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> List.ofSeq
            |> List.tryFind
                (fun release ->
                    release.Sdks
                    |> List.ofSeq
                    |> List.exists (fun sdk -> sdk.Version.Equals(resolvedSdkVersion)))
            |> Option.orElseWith
                (fun _ ->
                    failwithf "Could not find a sutable runtime version matching SDK version: %s" (resolvedSdkVersion.ToString()))
            |> Option.get

        sdkVersionRelease.Runtime.Version.ToString()

    member this.SdkReferenceAssemblies() =
        let dotnetHost =
            match Environment.isUnix with
            | true -> "dotnet"
            | false -> "dotnet.exe"

        let userInstallDir = DotNet.defaultUserInstallDir
        let systemInstallDir = DotNet.defaultSystemInstallDir

        let dotnetHostPath =
            if not(String.isNullOrEmpty CustomDotNetHostPath)
            then CustomDotNetHostPath
            else
                match File.Exists(userInstallDir </> dotnetHost) with
                | true -> userInstallDir
                | false -> systemInstallDir

        let referenceAssembliesPath =
            dotnetHostPath
            </> "packs"
            </> "Microsoft.NETCore.App.Ref"
            </> this.ResolveSdkRuntimeVersion()
            </> "ref"
            </> "net" + this.SdkVersionRaw

        Trace.traceVerbose <| sprintf "Resolved referenced SDK path: %s" referenceAssembliesPath
        match Directory.Exists referenceAssembliesPath with
        | true ->
            Directory.GetFiles(
                referenceAssembliesPath,
                "*.dll"
            )
            |> Seq.toList
        | false ->
            failwithf "Could not find referenced assemblies in path: '%s', please check installed SDK and runtime versions" referenceAssembliesPath

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

        let existingPkg =
            NuGetCache.GetTargetUserNupkg packageName version

        let extractedFolder =
            match File.Exists existingPkg with
            | true ->
                // Shortcut in order to prevent requests to nuget sources if we have it downloaded already
                Path.GetDirectoryName existingPkg
            | false ->
                let sources =
                    paketDependenciesFile.Value.Groups.[groupName]
                        .Sources

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

        let sdkDir =
            Path.Combine(extractedFolder, pathSuffix)

        Directory.GetFiles(sdkDir, "*.dll") |> Seq.toList

    member this.ResolveSdkReferenceAssemblies
        (
            groupName: Domain.GroupName,
            paketDependenciesFile: Lazy<Paket.DependenciesFile>
        ) =
        match this.IsSdkVersionFromGlobalJsonSameAsSdkVersion() with
        | true ->
            Trace.traceVerbose
            <| (sprintf "Using .Net %i assemblies" this.SdkVersion.Major)

            this.SdkReferenceAssemblies()
        | false ->
            Trace.traceVerbose
            <| (sprintf "%s" "Using .Netstandard assemblies")

            this.NetStandard20ReferenceAssemblies(
                "NETStandard.Library",
                "2.0.0",
                Path.Combine("build", "netstandard2.0", "ref"),
                groupName,
                paketDependenciesFile
            )
#endif
