# Generate semantic version number from Git with GitVersion

[*GitVersion*](https://gitversion.net/docs/) is a tool that generates a Semantic Version number based on your Git history.

To see the available GitVersion APIs in FAKE, please see the [`API-Reference`]({{root}}reference/fake-tools-gitversion.html) for the GitVersion module.

## Minimal working example
To get started, [*install GitVersion*](https://gitversion.net/docs/usage/cli/installation) as a Chocolaty tool
or if you would like to use the DotNet tool version of GitVersion then you need to provide full path to
GitVersion installed location.

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.Tools.GitVersion
//"

open Fake.Core
open Fake.DotNet
open Fake.Tools

let install = lazy DotNet.install DotNet.Versions.FromGlobalJson

Target.create "Version" (fun _ ->
    GitVersion.generateProperties (fun p ->
        { p with ToolType = ToolType.CreateCLIToolReference(install.Value) })
)

```
