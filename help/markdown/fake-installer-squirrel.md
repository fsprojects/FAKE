# Releasifying NuGet packages with Squirrel

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later.</p>
</div>

[Squirrel.Windows](https://github.com/Squirrel/Squirrel.Windows) is an installation an auto-update system for Windows desktop apps.

[API-Reference](apidocs/v5/fake-installer-squirrel.html)

## Squirrel.exe CLI tool

This module is a wrapper around the [Squirrel.exe](https://github.com/Squirrel/Squirrel.Windows/blob/master/docs/using/squirrel-command-line.md) CLI tool that is available in the [squirrel.windows](https://www.nuget.org/packages/squirrel.windows/) NuGet package.

## Releasify

[Releasifying](https://github.com/Squirrel/Squirrel.Windows/blob/master/docs/getting-started/2-packaging.md#releasifying) create all files necessary for release from a NuGet package

```fsharp
open Fake.Installer

Squirrel.releasify "./my.nupkg" (fun p -> { p with ReleaseDir = "./squirrel_release")
```

