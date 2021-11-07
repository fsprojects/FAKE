module Fake.Runtime.SdkAssemblyResolver

open System.IO
open System.Runtime.InteropServices
open Fake.Core
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.Runtime
open Paket

/// here we will pin Fake runner execution framework to .NET 6 as in `SdkVersion`
/// We will also try to resolve the current SDK that the runner is executing, if it is the same as pinned
/// one then we will use runtime assemblies from that SDK version on its installation on disk. Otherwise,
/// we will default to NetStandard2.0 assemblies. We will download them since they are packaged in a NuGet
/// package extract them a and reference them.
type SdkAssemblyResolver() =
#if DOTNETCORE

    member this.SdkVersion =
        Paket.FrameworkIdentifier.DotNetFramework Paket.FrameworkVersion.V6

    member this.IsResolvedSdkVersionSameAsLTSVersion() =
        match DotNet.tryGetSDKVersionFromGlobalJson () with
        | Some version -> version.StartsWith "6" // this need to be kept in sync with SdkVersion number
        | None -> false

    member this.SdkReferenceAssemblies() =
        let fileName =
            match Environment.isUnix with
            | true -> "dotnet"
            | false -> "dotnet.exe"

        let userInstallDir = DotNet.defaultUserInstallDir
        let systemInstallDir = DotNet.defaultSystemInstallDir

        let dotnet6ReferenceAssembliesPath =
            match File.Exists(userInstallDir </> fileName) with
            | true -> userInstallDir
            | false -> systemInstallDir

        let dotnet6RuntimeVersion =
            RuntimeInformation.FrameworkDescription.Replace(".NET ", "")

        Directory.GetFiles(
            dotnet6ReferenceAssembliesPath
            </> "packs"
            </> "Microsoft.NETCore.App.Ref"
            </> dotnet6RuntimeVersion
            </> "ref"
            </> "net6.0",
            "*.dll"
        )
        |> Seq.toList

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

    member this.ResolveSdkReferenceAssemblies(groupName: Domain.GroupName, paketDependenciesFile: Lazy<Paket.DependenciesFile>) =
        // here we will match for .NET 6 sdk from a global.json file. If found we will use
        // .NET 6 runtime assemblies. Otherwise we will default to .Netstandard 2.0.3
        match this.IsResolvedSdkVersionSameAsLTSVersion() with
        | true ->
            Trace.traceVerbose
            <| (sprintf "%s" "Using .Net 6 assemblies")

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
#else
    member this.IsSdkVersionDotNet6() = false
#endif
