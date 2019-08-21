module Fake.DotNet.CliIntegrationTests

open Fake.IO
open Fake.DotNet
open System.IO
open Expecto
open Fake.Core.IntegrationTests.TestHelpers

[<Tests>]
let tests =
    testList "Fake.DotNet.CliIntegrationTests" [
        testCase "Make sure dotnet installer works in paths with spaces - #2319" <| fun _ ->
            use d = createTestDir()
            let installerDir = Path.Combine(d.Dir, "Temp Dir")
            Directory.create installerDir
            let preparedDir = Path.Combine(d.Dir, "Install Dir")
            Directory.create preparedDir

            let f = DotNet.install (fun option ->
                { option with
                    InstallerOptions = fun o ->
                        { option.InstallerOptions o with
                            CustomDownloadDir = Some installerDir }
                    ForceInstall = true
                    CustomInstallDir = Some preparedDir
                    Channel = DotNet.CliChannel.Current
                    Version = DotNet.CliVersion.Latest })

            let opts = f (DotNet.Options.Create())
            Expect.isTrue (File.Exists opts.DotNetCliPath) "Expected dotnet executable to exist"
            Expect.stringStarts opts.DotNetCliPath preparedDir "Expected dotnet cli to start with prepared directory"
        testCase "Make sure dotnet installer can install into path with spaces - #2319" <| fun _ ->
            use d = createTestDir()
            let preparedDir = Path.Combine(d.Dir, "Install Dir")

            let f = DotNet.install (fun option ->
                { option with
                    ForceInstall = true
                    CustomInstallDir = Some preparedDir
                    Channel = DotNet.CliChannel.Current
                    Version = DotNet.CliVersion.Latest })

            let opts = f (DotNet.Options.Create())
            Expect.isTrue (File.Exists opts.DotNetCliPath) "Expected dotnet executable to exist"
            Expect.stringStarts opts.DotNetCliPath preparedDir "Expected dotnet cli to start with prepared directory"
        testCase "Make sure dotnet installer works without spaces - #2319" <| fun _ ->
            use d = createTestDir()
            let preparedDir = Path.Combine(d.Dir, "InstallDir")

            let f = DotNet.install (fun option ->
                { option with
                    ForceInstall = true
                    Channel = DotNet.CliChannel.Current
                    CustomInstallDir = Some preparedDir
                    Version = DotNet.CliVersion.Latest })

            let opts = f (DotNet.Options.Create())
            Expect.isTrue (File.Exists opts.DotNetCliPath) "Expected dotnet executable to exist"
            Expect.stringStarts opts.DotNetCliPath preparedDir "Expected dotnet cli to start with prepared directory"
    ]
