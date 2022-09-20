# FAKE - F# Make &middot; [![FAKE Build and Test](https://github.com/fsprojects/FAKE/actions/workflows/build_and_test.yml/badge.svg)](https://github.com/fsprojects/FAKE/actions/workflows/build_and_test.yml) [![NuGet Badge](https://buildstats.info/nuget/Fake-Cli)](https://www.nuget.org/packages/Fake-Cli) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)]([https://reactjs.org/docs/how-to-contribute.html#your-first-pull-request](https://fake.build/guide/contributing.html)) [![Join the chat at https://gitter.im/fsharp/FAKE](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/fsharp/FAKE?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

"FAKE - F# Make" is a cross platform build automation system. Due to its integration
in F#, all benefits of the .NET Framework and functional programming can be used, including
the extensive class library, powerful debuggers and integrated development environments like
Visual Studio or MonoDevelop, which provide syntax highlighting and code completion.

The new DSL was designed to be succinct, typed, declarative, extensible and easy to use.

See the [project home page](https://fake.build/) for tutorials and [API documentation](https://fake.build/reference/index.html).

## Usage

See detailed instructions on [how to use](https://fake.build/guide/fake-gettingstarted.html#Install-FAKE) the project.

## Requirements

Fake runner requires .Net v6 SDK to be installed on the machine to run it. .Net v6 was chosen since it is the current LTS release.

Fake modules has target frameworks of `net6` and `netstandard2.0`. Please [see this link](https://docs.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0) which lists the supported .Net and .NET Framework versions by `netstandard2.0`

## Installation

* Either: Download and install the [Dotnet SDK](https://www.microsoft.com/net/learn/get-started) and run `dotnet tool restore` followed by `dotnet fake build`
* Or: Install FAKE 5 or later (for example via `choco install fake -pre`) and run `fake build`

> Note: You can find more details on the [contributing page](https://fake.build/guide/contributing.html)

Make sure to have long path enabled: https://superuser.com/questions/1119883/windows-10-enable-ntfs-long-paths-policy-option-missing
Otherwise the test-suite will fail (However, the compilation should work)

## Example
[Fake site](https://fake.build) has several examples and API samples to get you started. Here is an example to get a glimpse on FAKE:

```F#
// build.fsx

#r "paket:
nuget Fake.Core.Trace
nuget Fake.Core.Target //"
// include Fake modules, see Fake modules section

open Fake.Core

// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log " --- Cleaning stuff --- "
)

Target.create "Build" (fun _ ->
  Trace.log " --- Building the app --- "
)

Target.create "Deploy" (fun _ ->
  Trace.log " --- Deploying app --- "
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "Deploy"

// *** Start Build ***
Target.runOrDefault "Deploy"
```

This example pulls Fake's `Target` and `Trace` modules and define three targets: `Clean`, `Build`, and `Deploy`. By analogy with a .Net project;
* the `Clean` target can be used to clean the project before a build,
*  the `Build` target to call MSBuild or any build steps that are required for you application,
*  the `Deploy` target can push your built project to a cloud service.

At the bottom, the example define target dependencies, which specify that a `Deploy` must run after a `Build` which must run after a `Clean`.

## NuGet Packages

| Package Name         | Nuget                                                                                                                |
| -------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `Fake-Cli`           | [![NuGet Badge](https://buildstats.info/nuget/Fake-Cli)](https://www.nuget.org/packages/Fake-Cli)                    |
| `Fake.Core.Target`   | [![NuGet Badge](https://buildstats.info/nuget/Fake.Core.Target)](https://www.nuget.org/packages/Fake.Core.Target)    |
| `Fake.Core.Context`  | [![NuGet Badge](https://buildstats.info/nuget/Fake.Core.Context)](https://www.nuget.org/packages/Fake.Core.Context)  |
| Legacy FAKE          | [![NuGet Badge](https://buildstats.info/nuget/FAKE)](https://www.nuget.org/packages/FAKE)                            |

## Contributing

See the [contributing page](https://fake.build/guide/contributing.html).

## Maintainers

Although this project is hosted in the [fsprojects](https://github.com/fsprojects) organization, it is _not_ maintained and managed by the F# Core Engineering Group. The F# Core Engineering Group acknowledges that the independent owner and maintainer of this project is [Steffen Forkmann](https://github.com/forki).
