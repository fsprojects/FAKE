module Fake.DotNet.sdkAssemblyResolverTests

open System.Text.RegularExpressions
open Fake.IO
open Fake.DotNet
open System
open System.IO
open Fake.IO.FileSystemOperators
open Expecto
open Fake.Core.IntegrationTests.TestHelpers
open Fake.Runtime

[<Tests>]
let tests =
    testList
        "Fake.DotNet.sdkAssemblyResolverTests" [
          testCase "Runner run script with NETStandard2.0 SDK assemblies" <| fun _ ->
              let result =
                  handleAndFormat <| fun _ ->
                      fakeRunAndCheck Ctx.Verbose "reference-assemblies.fsx" "reference-assemblies.fsx" "core-reference-assemblies-netstandard20"

              let stdOut =
                  String.Join("\n", result.Messages).Trim()

              let expectedNetStandardPathPortion =
                  ".nuget"
                  </> "packages"
                  </> "netstandard.library"
                  </> "2.0.0"

              (sprintf "stdout should contain '%s', but was: '%s'" expectedNetStandardPathPortion stdOut)
              |> Expect.isTrue (stdOut.Contains expectedNetStandardPathPortion)

          testCase "Runner run script with 6.0.100 SDK version assemblies" <| fun _ ->
              try
                  use d = createTestDir()
                  let installerDir = Path.Combine(d.Dir, "Temp Dir")
                  Directory.create installerDir
                  let preparedDir = Path.Combine(d.Dir, "Install Dir")
                  Directory.create preparedDir

                  DotNet.install (fun option ->
                  { option with
                        InstallerOptions = fun o ->
                                  { option.InstallerOptions o with
                                        CustomDownloadDir = Some installerDir }
                        ForceInstall = true
                        WorkingDirectory = scenarioTempPath "core-reference-assemblies-net60100"
                        CustomInstallDir = Some preparedDir
                        Channel = DotNet.CliChannel.Version 6 0
                        Version = DotNet.CliVersion.Version "6.0.100" })
                  |> ignore

                  Environment.setEnvironVar "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH" preparedDir

                  let result =
                      handleAndFormat <| fun _ ->
                          fakeRunAndCheck Ctx.Verbose "reference-assemblies.fsx" "reference-assemblies.fsx" "core-reference-assemblies-net60100"

                  let stdOut =
                      String.Join("\n", result.Messages).Trim()

                  let expectedNet6PathPortion = "packs"</>"Microsoft.NETCore.App.Ref"</>"6.0.0"</>"ref"</>"net6.0"

                  (sprintf "stdout should contain path like '%s', but was: '%s'" expectedNet6PathPortion stdOut)
                  |> Expect.isTrue (stdOut.Contains expectedNet6PathPortion)
              finally
                  // clean up after the test run
                  Environment.setEnvironVar "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH" ""

          testCase "Runner run script with 6.0.101 SDK version assemblies" <| fun _ ->
              try
                  use d = createTestDir()
                  let installerDir = Path.Combine(d.Dir, "Temp Dir")
                  Directory.create installerDir
                  let preparedDir = Path.Combine(d.Dir, "Install Dir")
                  Directory.create preparedDir

                  DotNet.install (fun option ->
                  { option with
                        InstallerOptions = fun o ->
                                  { option.InstallerOptions o with
                                        CustomDownloadDir = Some installerDir }
                        ForceInstall = true
                        WorkingDirectory = scenarioTempPath "core-reference-assemblies-net60101"
                        CustomInstallDir = Some preparedDir
                        Channel = DotNet.CliChannel.Version 6 0
                        Version = DotNet.CliVersion.Version "6.0.101" })
                  |> ignore

                  Environment.setEnvironVar "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH" preparedDir

                  let result =
                      handleAndFormat <| fun _ ->
                          fakeRunAndCheck Ctx.Verbose "reference-assemblies.fsx" "reference-assemblies.fsx" "core-reference-assemblies-net60101"

                  let stdOut =
                      String.Join("\n", result.Messages).Trim()

                  let expectedNet6PathPortion = "packs"</>"Microsoft.NETCore.App.Ref"</>"6.0.1"</>"ref"</>"net6.0"

                  (sprintf "stdout should contain path like '%s', but was: '%s'" expectedNet6PathPortion stdOut)
                  |> Expect.isTrue (stdOut.Contains expectedNet6PathPortion)
              finally
                  // clean up after the test run
                  Environment.setEnvironVar "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH" ""
          
          testCase "Runner run script with 6.0.100 SDK version assemblies and resolve runtime version from cached file" <| fun _ ->
              try
                  use d = createTestDir()
                  let installerDir = Path.Combine(d.Dir, "Temp Dir")
                  Directory.create installerDir
                  let preparedDir = Path.Combine(d.Dir, "Install Dir")
                  Directory.create preparedDir

                  DotNet.install (fun option ->
                  { option with
                        InstallerOptions = fun o ->
                                  { option.InstallerOptions o with
                                        CustomDownloadDir = Some installerDir }
                        ForceInstall = true
                        WorkingDirectory = scenarioTempPath "core-reference-assemblies-net60100"
                        CustomInstallDir = Some preparedDir
                        Channel = DotNet.CliChannel.Version 6 0
                        Version = DotNet.CliVersion.Version "6.0.100" })
                  |> ignore

                  Environment.setEnvironVar "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH" preparedDir
                  Environment.setEnvironVar "FAKE_SDK_RESOLVER_RUNTIME_VERSION_RESOLVE_METHOD" "cache"

                  let result =
                      handleAndFormat <| fun _ ->
                          fakeRunAndCheck Ctx.Verbose "reference-assemblies.fsx" "reference-assemblies.fsx" "core-reference-assemblies-net60100"

                  let stdOut =
                      String.Join("\n", result.Messages).Trim()
                  
                  printfn "%s" stdOut

                  let expectedNet6PathPortion = "packs"</>"Microsoft.NETCore.App.Ref"</>"6.0.0"</>"ref"</>"net6.0"
                  let expectedCacheFileResolutionMessage = "Trying to resolve runtime version from cache.."

                  (sprintf "stdout should contain path like '%s', but was: '%s'" expectedNet6PathPortion stdOut)
                  |> Expect.isTrue (stdOut.Contains expectedNet6PathPortion)

                  (sprintf "stdout should contain a message that cache file is used in runtime version resolution")
                  |> Expect.isTrue (stdOut.Contains expectedCacheFileResolutionMessage)
              finally
                  // clean up after the test run
                  Environment.setEnvironVar "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH" ""
                  Environment.setEnvironVar "FAKE_SDK_RESOLVER_RUNTIME_VERSION_RESOLVE_METHOD" ""

        ]
