# Automating Deployment using FAKE and Octopus Deploy

[*Octopus Deploy*](http://octopusdeploy.com/) is a great tool for simple and user-friendly release management.

To see the available Octo APIs in FAKE, please see the [`API-Reference`]({{root}}reference/fake-tools-octo.html) for the Octo module.

## Installing Octopus Deploy

You can try the cloud version of Octopus [*here*](https://octopus.com/cloud). Otherwise you can download it 
from [*http://octopusdeploy.com/downloads*](http://octopusdeploy.com/downloads) - and then start 
a [*free trial*](https://octopus.com/trial). Then follow the [*Installation Instructions*](http://octopusdeploy.com/documentation/install/octopus) 
to get yourself up and running.

You will also need to install and configure at least one [*Tentacle*](http://octopusdeploy.com/documentation/install/tentacle) which you will 
deploy your software and services to. 

## `Octo.exe` CLI tool

The `Fake.Tools.Octo` module is a wrapper around the [*Octo.exe*](https://octopus.com/docs/api-and-integration/octo.exe-command-line) CLI tool 
which controls Octopus Deploy API. You'll need the `Octo.exe` tool itself accessible to your FAKE script. Download it from 
[*here*](https://octopus.com/downloads). Or you can install it as a Chocolaty tool from [*here*](https://octopus.com/downloads/octopuscli)

This module also supports use via a .NET Core Tool-Manifest. Installation is simple! From the root of your repository run the following

```bash
dotnet new tool-manifest # if one doesn't already exist
dotnet tool install Octopus.DotNet.Cli 
```

### Generate an API Key

In order to communicate with the Octopus Deploy API you will need an **API key** to authenticate with.

It is a good idea to create an account in Octopus Deploy for your Continuous Integration systems 
(eg: [*TeamCity*](https://www.jetbrains.com/teamcity/)) - and then generate a new API key for that account, 
that can be safely used from within your build process. See the following screenshots:

![My Profile]({{root}}content/img/octopusdeploy/myprofile.png "My Profile")

![Generate API Key]({{root}}content/img/octopusdeploy/apikey.png "Generate API Key")

## Define common options

You can define a function defining shared parameters like `ToolPath`  or your Octopus Deploy instance details. 
Then the function can be used in subsequent `Octo` calls.

```fsharp
open Fake.DotNet // needed for ToolType
open Fake.Tools

let setCommon (ps: Octo.Options) =
            { ps with
                ToolPath = "path to your octo.exe"
                Server =
                    { ServerUrl = "Your Octopus Server URL"
                      ApiKey = "Your API key" } }
```

## Push a package

Packages can be pushed to the built-in Octopus repository with the push command:

```fsharp
open Fake.Tools

Octo.push (fun ps ->
            { ps with
                Packages =
                    [ "mypackage.nupkg"
                      "mypackage2.nupkg" ]
                Common = setCommon ps.Common })
```

## Create a Release

Octopus Deploy relies on the concept of a **release**, which should be an immutable *NuGet Package* which has been built, tested, 
[*packaged and published*](apidocs/v5/legacy/fake-nugethelper.html) from your Continuous Integration systems - which you can of course 
completely manage with your FAKE script.

So once you have created a project you are able to create and push a release into Octopus Deploy. This can be done through the Octopus UI, 
command line tool, or in our case - from a FAKE script.

```fsharp
open Fake.Tools

Octo.createRelease (fun ps ->
        { ps with
            Project = "Your Project"
            Version = "latest"
            Common = setCommon ps.Common })
```


## Deploy a Release

You can automatically deploy a release when you create it, but using the optional deploy options when you create your release.

This is often a good idea when you want your FAKE build script to continue on to a second set of perhaps slower, unit tests 
that exercise behaviors in a more complete and perhaps integrated environment. 

```fsharp
open Fake.Tools

Octo.deployRelease (fun ps ->
            { ps with
                Project = "Your Project"
                DeployTo = "Staging"
                Version = "latest"
                Common = setCommon ps.Common })
```
