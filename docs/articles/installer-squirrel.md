# Releasifying NuGet Packages with Squirrel

[*Squirrel.Windows*](https://github.com/Squirrel/Squirrel.Windows) is an installation tool with an auto-update system 
for Windows desktop apps.

To see the available Squirrel APIs in FAKE, please see the [`API-Reference`]({{root}}reference/fake-installer-squirrel.html) for the Squirrel module.

## `Squirrel.exe` CLI tool

This module is a wrapper around the [*Squirrel.exe*](https://github.com/Squirrel/Squirrel.Windows/blob/master/docs/using/squirrel-command-line.md) 
CLI tool that is available in the [*squirrel.windows*](https://www.nuget.org/packages/squirrel.windows/) NuGet package.

## Releasify

[*Releasifying*](https://github.com/Squirrel/Squirrel.Windows/blob/master/docs/getting-started/2-packaging.md#releasifying) create all files 
necessary for release from a NuGet package.

```fsharp
open Fake.Installer

Squirrel.releasify "./my.nupkg" (fun p -> { p with ReleaseDir = "./squirrel_release")
```

