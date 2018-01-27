# Getting started with FAKE - F# Make

**Note:  This documentation is for FAKE.exe version 5.0 or later. The old documentation can be found [here](legacy-gettingstarted.html)**

In this tutorial you will learn how to set up a complete build infrastructure with "FAKE - F# Make". This includes:

* how to install the latest FAKE version
* how to automatically compile your C# or F# projects
* how to automatically resolve nuget dependencies
* how to automatically run NUnit tests on your projects
* how to zip the output to a deployment folder

## Install FAKE

"FAKE - F# Make" is completely written in F# and all build scripts will also be written in F#, but this doesn't imply that you have to learn programming in F#. In fact the "FAKE - F# Make" syntax is hopefully very easy to learn.

There are various ways to install FAKE 5

- Install the 'fake' or 'fake-netcore' package for you system (currenty chocolatey)
  Example `choco install fake -pre`
- Use it as dotnet tool (soon)
- Bootstrap via shell script (build.cmd/build.sh) (documented soon)
  > DISCLAIMER: These scripts have no versioning story. You either need to take care of versions yourself (and lock them) or your builds might break on major releases.

## Create and Edit scripts with Intellisense

Once `fake` is available you can start creating your script:

- Create a new file `myscript.fsx` with the following contents:

```fsharp
(* -- Fake Dependencies paket-inline
storage: none
source https://api.nuget.org/v3/index.json

nuget Fake.Core.Target prerelease
-- Fake Dependencies -- *)
#load "./.fake/myscript.fsx/intellisense.fsx"
```

Where you can add all the [fake modules](fake-fake5-modules.html) you need.

- run the script to restore your dependencies and setup the intellisense support: `fake run myscript.fsx`.
  This might take some seconds depending on your internet connection

  > The warning `FS0988: Main module of program is empty: nothing will happen when it is run` indicates that you have not written any code into the script yet.

- now open the script in VS Code with ionide-fsharp extension or Visual Studio.

> Note: If you change your dependencies you need to delete `.fake/myscript.fsx/paket.lock` and run the script again for intellisense to update.

> Note: Intellisense is shows for the full framework while the script is run as `netcoreapp20` therefore intellisense might show APIs which are not actually usable.

## TBD.

The best way is currently to use the [quick start guide](fake-dotnetcore.html)