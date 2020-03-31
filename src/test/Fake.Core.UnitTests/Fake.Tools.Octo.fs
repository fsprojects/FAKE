module Fake.Tools.Testing.Octo

open Expecto

[<Tests>]
let defaultTests =
    testList "Fake.Tools.Octo Tests" [
        testCase "List Environments" <| fun _ ->
            let expected = ["list-environments"]
            let actual =
                Fake.Tools.Octo.Command.ListEnvironments
                |> Fake.Tools.Octo.commandLine
            Expect.hasLength actual expected.Length "ListEnvironment only has command"
            Expect.sequenceEqual actual expected "ListEnvironment command should be the list-environments string"

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

        testCase "Push Command" <| fun _ ->
            let expect = [
                    "push"
                    "--package=Package-1 --package=Package-2"
                    "--replace-existing"
                ]
            let (pushOption: Fake.Tools.Octo.PushOptions) = {
                Packages=["Package-1"; "Package-2"]
                ReplaceExisting=true
                Common={
                    ToolName="ToolName-1"
                    ToolPath="ToolPath-1"
                    WorkingDirectory="WorkingDirectory-1"
                    Server = {
                        ServerUrl="ServerUrl"
                        ApiKey="ApiKey"
                    }
                    Timeout=System.TimeSpan.MaxValue
                    UseManifestTool = false
                }
            }
            let actual =
                pushOption
                |> Fake.Tools.Octo.Command.Push
                |> Fake.Tools.Octo.commandLine
            Expect.hasLength actual expect.Length ""
            Expect.sequenceEqual actual expect ""

        testCase "Create Release Fully Filled Out" <| fun _ ->
            let expectedCommand = [
                "create-release"
                "--project=Project-1"
                "--version=Version-1"
                "--packageversion=PackageVersion-1"
                "--package=Package-1 --package=Package-2"
                "--packagesfolder=PackageFolder-1"
                "--releasenotes=ReleaseNotes-1"
                "--releasenotesfile=ReleaseNotesFile-1"
                "--ignoreExisting"
                "--channel=Channel-1"
                "--ignorechannelrules"
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
            Expect.hasLength actual expectedCommand.Length ""
            Expect.sequenceEqual actual expectedCommand ""

        testCase "Create Release With Common" <| fun _ ->
            let expectedCommand = [
                "create-release"
                "--project=Project-1"
                "--version=Version-1"
                "--packageversion=PackageVersion-1"
                "--package=Package-1 --package=Package-2"
                "--packagesfolder=PackageFolder-1"
                "--releasenotes=ReleaseNotes-1"
                "--releasenotesfile=ReleaseNotesFile-1"
                "--ignoreExisting"
                "--channel=Channel-1"
                "--ignorechannelrules"
                "--project=Project-1"
                "--deployto=Env-1"
                "--version=Version-1"
                "--progress"
                "--deploymenttimeout=10675199.02:48:05.4775807"
                "--deploymentchecksleepcycle=10675199.02:48:05.4775807"
                "--specificmachines=Machine-1"
                "--channel=Channel-1"
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
            let deployReleaseOptions: Fake.Tools.Octo.DeployReleaseOptions = {
                Project="Project-1"
                DeployTo="Env-1"
                Version="Version-1"
                Force=false
                WaitForDeployment=false
                NoRawLog=false
                Progress=true
                DeploymentTimeout=Some System.TimeSpan.MaxValue
                DeploymentCheckSleepCycle=Some System.TimeSpan.MaxValue
                SpecificMachines=Some "Machine-1"
                Channel=Some "Channel-1"
                Common = {
                    ToolName="ToolName-1"
                    ToolPath="ToolPath-1"
                    WorkingDirectory="WorkingDirectory-1"
                    Server = {
                        ServerUrl="ServerUrl"
                        ApiKey="ApiKey"
                    }
                    Timeout=System.TimeSpan.MaxValue
                    UseManifestTool=false
                }
            }

            let actual =
                (releaseOptions, Some deployReleaseOptions)
                |> Fake.Tools.Octo.Command.CreateRelease
                |> Fake.Tools.Octo.commandLine
            Expect.hasLength actual expectedCommand.Length ""
            Expect.sequenceEqual actual expectedCommand ""

        testCase "Create Deploy Full Filled Out" <| fun _ ->
            let expected = [
                "deploy-release"
                "--project=Project-1"
                "--deployto=Environment-1"
                "--version=123.123.123"
                "--force"
                "--waitfordeployment"
                "--norawlog"
                "--progress"
                "--deploymenttimeout=10675199.02:48:05.4775807"
                "--deploymentchecksleepcycle=10675199.02:48:05.4775807"
                "--specificmachines=Machine-1"
                "--channel=Channel-1"
            ]
            let (deployReleaseOption:Fake.Tools.Octo.DeployReleaseOptions) = {
                Project="Project-1"
                DeployTo="Environment-1"
                Version="123.123.123"
                Force=true
                WaitForDeployment=true
                NoRawLog=true
                Progress=true
                DeploymentTimeout=Some System.TimeSpan.MaxValue
                DeploymentCheckSleepCycle=Some System.TimeSpan.MaxValue
                SpecificMachines=Some "Machine-1"
                Channel=Some "Channel-1"
                Common={
                    ToolName="ToolName-1"
                    ToolPath="ToolPath-1"
                    WorkingDirectory="WorkingDirectory-1"
                    Server = {
                        ServerUrl="ServerUrl"
                        ApiKey="ApiKey"
                    }
                    Timeout=System.TimeSpan.MaxValue
                    UseManifestTool=false
                    }
                }

            let actual =
                deployReleaseOption
                |> Fake.Tools.Octo.Command.DeployRelease
                |> Fake.Tools.Octo.commandLine
            Expect.hasLength actual expected.Length ""
            Expect.sequenceEqual expected actual ""

        testCase "Delete Release fully Fulled Out" <| fun _ ->
            let expectedCommand = [
                "delete-releases"
                "--project=Project-1"
                "--minversion=1.0.0"
                "--maxversion=123.123.123"
                "--channel=Channel-1"
            ]
            let (deleteReleaseOptions: Fake.Tools.Octo.DeleteReleasesOptions) = {
                Project="Project-1"
                MinVersion="1.0.0"
                MaxVersion="123.123.123"
                Channel=Some "Channel-1"
                Common={
                    ToolName="ToolName-1"
                    ToolPath="ToolPath-1"
                    WorkingDirectory="WorkingDirectory-1"
                    Server = {
                        ServerUrl="ServerUrl"
                        ApiKey="ApiKey"
                    }
                    Timeout=System.TimeSpan.MaxValue
                    UseManifestTool=false
                }
            }

            let actual =
                deleteReleaseOptions
                |> Fake.Tools.Octo.Command.DeleteReleases
                |> Fake.Tools.Octo.commandLine
            Expect.hasLength actual expectedCommand.Length ""
            Expect.sequenceEqual actual expectedCommand ""

        testCase "Realistic Octo Args To Windows Command Line" <| fun _ ->
            let setCommon (ps:Fake.Tools.Octo.Options) =
                {
                    ps with
                        ToolPath = "Z:\tools"
                        ToolName = "octo.exe"
                        Server = {
                            ServerUrl = "https://myoctopus-server.com"
                            ApiKey = "octoApiKey"
                        }
                }
            let setReleaseParams = (fun (ro:Fake.Tools.Octo.CreateReleaseOptions) ->
                { ro with Project = "MyProject"; Version = "1234567890.12.34"; Packages=["MyPackage:1234567890.12.34"]; Common = setCommon ro.Common })
            let setDeployParams = (fun (dOpt: Fake.Tools.Octo.DeployReleaseOptions) ->
                { dOpt with Project = "MyProject"; Version = "1234567890.12.34"; DeployTo = "MyEnvironment"; Common = setCommon dOpt.Common; Progress=true } |> Some)
            let command = (Fake.Tools.Octo.CreateRelease ((setReleaseParams Fake.Tools.Octo.releaseOptions), (setDeployParams Fake.Tools.Octo.deployOptions)))
            let releaseOptions = setReleaseParams Fake.Tools.Octo.releaseOptions

            let args = (List.append (Fake.Tools.Octo.commandLine command) (Fake.Tools.Octo.serverCommandLine releaseOptions.Common.Server)) |> Fake.Core.Arguments.OfArgs
            let command = args.ToWindowsCommandLine
            Expect.equal command "create-release --project=MyProject --version=1234567890.12.34 --package=MyPackage:1234567890.12.34 --project=MyProject --deployto=MyEnvironment --version=1234567890.12.34 --progress --server=https://myoctopus-server.com --apikey=octoApiKey" "The output should be runnable on windows."

        testCase "UseManifestTool=true produces correct tool" <| fun _ ->
            let expected = "dotnet"
            let opts = { Fake.Tools.Octo.commonOptions with UseManifestTool = true }
            let actual = 
                opts 
                |> Fake.Tools.Octo.getTool 
            Expect.equal actual expected "UseManifestTool=true should return dotnet"

        testCase "UseManifestTool=false produces correct tool" <| fun _ ->
            let expected = "Octo.exe"
            let opts = { Fake.Tools.Octo.commonOptions with UseManifestTool = true }
            let actual = 
                opts 
                |> Fake.Tools.Octo.getTool 
            Expect.stringEnds actual expected "UseManifestTool=true should return dotnet"

        testCase "Create Release Default with UseManifestTool=true" <| fun _ ->
            let expectedCommand = [
                "dotnet-octo"
                "create-release"
                ]
            let opts = { Fake.Tools.Octo.commonOptions with UseManifestTool = true }
            let releaseOptions = 
                ({ Fake.Tools.Octo.releaseOptions with Common = opts }, None) 
                |> Fake.Tools.Octo.Command.CreateRelease  
            let actual = 
                opts
                |> Fake.Tools.Octo.getArgs releaseOptions
            Expect.hasLength actual expectedCommand.Length "With UseManifestTool options expects two commands"
            Expect.sequenceEqual actual expectedCommand "CreateRelease command with UseManifestTool should have dotnet-octo and create-release"
        
        testCase "Create Release Default with UseManifestTool=false" <| fun _ ->
            let expectedCommand = [
                "create-release"
                ]
            let opts = Fake.Tools.Octo.commonOptions
            let releaseOptions = 
                (Fake.Tools.Octo.releaseOptions, None) 
                |> Fake.Tools.Octo.Command.CreateRelease  
            let actual = 
                opts
                |> Fake.Tools.Octo.getArgs releaseOptions
            Expect.hasLength actual expectedCommand.Length "With default options only expect the command"
            Expect.sequenceEqual actual expectedCommand "CreateRelease command should be the create-release string"
    ]
