module Fake.Tools.Testing.Octo

open Expecto

[<Tests>]
let defaultTests =
    testList "Fake.Tools.Octo Tests" [
        testCase "Create Release Default" <| fun _ ->
            let expectedCommand = [
                "create-release"
                ]
            let actual = 
                (Fake.Tools.Octo.releaseOptions, None) 
                |> Fake.Tools.Octo.Command.CreateRelease  
                |> Fake.Tools.Octo.commandLine
            Expect.hasLength actual expectedCommand.Length "With default options only expect the command"
            Expect.sequenceEqual actual expectedCommand "CreateRelease command should be the create-release string"

        testCase "Create Release Fully Filled Out" <| fun _ ->
            let expectedCommand = [
                "create-release"
                "--project=Project-1"
                "--version=Version-1"
                "--packageversion=PackageVersion-1"
                " --package=Package-1 --package=Package-2"
                "--packagesfolder=PackageFolder-1"
                "--releasenotes=ReleaseNotes-1"
                "--releasenotesfile=ReleaseNotesFile-1"
                " --ignoreExisting"
                "--channel=Channel-1"
                " --ignorechannelrules"
                ]
            let releaseOptions = {
                Fake.Tools.Octo.releaseOptions with
                    Project="Project-1"
                    Version="Version-1"
                    PackageVersion="PackageVersion-1"
                    Packages=["Package-1"; "Package-2"]
                    PackagesFolder=Some "PackageFolder-1"
                    ReleaseNotes="ReleaseNotes-1"
                    ReleaseNotesFile="ReleaseNotesFile-1"
                    IgnoreExisting=true
                    Channel=Some "Channel-1"
                    Common=Fake.Tools.Octo.commonOptions
                    IgnoreChannelRules=true
            }
            let actual = 
                (releaseOptions, None) 
                |> Fake.Tools.Octo.Command.CreateRelease  
                |> Fake.Tools.Octo.commandLine
            Expect.hasLength actual expectedCommand.Length "With default options only expect the command"
            Expect.sequenceEqual actual expectedCommand "CreateRelease command should be the create-release string"
            ]
