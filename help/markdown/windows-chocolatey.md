# Using Chocolatey

## Namespace

To be used, the `Choco` module needs the `Fake.Windows` namespace:

```fsharp
#r "paket:
nuget Fake.Windows.Chocolatey //"

open Fake.Windows
```

## Install

The [`Install`](apidocs/v5/fake-choco.html) method allow to easily install packages from [Chocolatey](https://chocolatey.org).
By default all user interaction are skipped but it can be modified through the `NonInteractive` property.

```fsharp
"BuildApp" =?> ("InspectCodeAnalysis", Choco.IsAvailable)

Target.create "InspectCodeAnalysis" (fun _ ->
    "resharper-clt.portable" |> Choco.install id
    ...
)
```

## Pack

The [`Pack`](apidocs/v5/fake-choco.html) and [`PackFromTemplate`](apidocs/v5/fake-choco.html) methods allow to pack a .nuspec, chocolateyInstall.ps1 and/or chocolateyUninstall.ps1 file to a package (.nupkg).
It is based on [`NuGet`](create-nuget-package.html) but have some specifics, the package can be base on templates for the .nuspec, the chocolateyInstall.ps1 and/or chocolateyUninstall.ps1 but it's not mandatory.
It is also possible to only defines the fields in ChocoPackParams and the corresponding files will be created.

```fsharp
Target.create "ChocoPack" (fun _ ->
    Choco.pack (fun p ->
        { p with
            PackageId = "nvika"
            Version = version
            Title = "NVika"
            Authors = ["laedit"]
            Owners = ["laedit"]
            ProjectUrl = "https://github.com/laedit/vika"
            IconUrl = "https://cdn.rawgit.com/laedit/vika/master/icon.png"
            LicenseUrl = "https://github.com/laedit/vika/blob/master/LICENSE"
            BugTrackerUrl = "https://github.com/laedit/vika/issues"
            Description = "Parse analysis reports (InspectCode, ...) and send messages to build server or console."
            Tags = ["report"; "parsing"; "build"; "server"; "inspectcode"]
            ReleaseNotes = "https://github.com/laedit/vika/releases"
            PackageDownloadUrl = "https://github.com/laedit/vika/releases/download/" + tag + "/NVika." + version + ".zip"
            Checksum = Checksum.CalculateFileHash ("NVika." + version + ".zip")
            ChecksumType = Choco.ChocolateyChecksumType.Sha256
        })
)
```

### Nuspec
It adds Chocolatey specific fields:

Placeholder | replaced by (`ChocoPackParams` record field)
:--- | :---
`@docsUrl@` | `DocsUrl`: Url pointing to the location of the wiki or docs of the software.
`@mailingListUrl@` | `MailingListUrl`: Url pointing to the forum or email list group for the software.
`@bugTrackerUrl@` | `BugTrackerUrl`: Url pointing to the location where issues and tickets can be accessed.
`@projectSourceUrl@` | `ProjectSourceUrl`: Url pointing to the location of the underlying software source.
`@packageSourceUrl@` | `PackageSourceUrl`: Url to the chocolatey package repository, not the software (unless they are the same).

### chocolateyInstall.ps1
To use a chocolateyInstall.ps1 template, a file with the same name must exists in a `tool` folder alongside the .nuspec template file.
If it doesn't exists but at least `Title` and `PackageDownloadUrl` are defined, the chocolateyInstall.ps1 will be created

Placeholder | replaced by (`ChocoPackParams` record field)
:--- | :---
`@packageName@` | `Title`: Title of the package.
`@url@` | `PackageDownloadUrl`: Url pointing to the installer (exe, msi, zip) of the package.
`@url64@` | `PackageDownload64Url`: Url pointing to the installer (exe, msi, zip) of the 64 bits version of the package.
`@silentArgs@` | `SilentArgs`: Silent args for the installer.
`@unzipLocation@` | `UnzipLocation`: Unzip location for zip package. Default: Chocolatey install folder.
`@installerType@` | `InstallerType`: Installer type. Default: Zip.
`@checksum@` | `Checksum`: The checksum hash value of the PackageDownloadUrl resource.
`@checksumType@` | `ChecksumType`: The type of checksum that the file is validated with. Default: Sha256.
`@checksum64@` | `Checksum64`: The checksum hash value of the PackageDownload64Url resource.
`@checksum64Type@` | `Checksum64Type`: The type of checksum that the file is validated with. Default: Sha256.

### chocolateyUninstall.ps1
To use a chocolateyUninstall.ps1 template, a file with the same name must exists in a `tool` folder alongside the .nuspec template file.
If it doesn't exists but at least `Title` and `UninstallPath` are defined, the chocolateyUninstall.ps1 will be created

Placeholder | replaced by (`ChocoPackParams` record field)
:--- | :---
`@packageName@` | `Title`: Title of the package.
`@silentArgs@` | `SilentArgs`: Silent args for the installer.
`@installerType@` | `InstallerType`: Installer type. Default: Zip.
`@uninstallPath@` | `UnzipLocation`: For zip: the zip filename originally installed. For exe or msi: the full path to the native uninstaller to run

## Push

The [`Push`](apidocs/v5/fake-choco.html) method allow to push a package (.nupkg) to Chocolatey.
If need the source could be modified to a private feed for example.
It is heavily recommended to indicate your Chocolatey api key, specifically for the build servers which don't have registered Chocolatey api key.
In order to keep it secret you can encrypt it, for example with |AppVeyor](https://www.appveyor.com) you can [encrypt an environment variable](https://www.appveyor.com/docs/build-configuration#secure-variables) and use it in your FAKE script:

```fsharp
Target.create "ChocoPush" (fun _ ->
    "pretzel.0.5.0.nupkg" |> Choco.push (fun p -> { p with ApiKey = environVar myChocolateyApiKey })
)
```
