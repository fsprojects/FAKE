# FAKE - F# Make

[![FAKE Build and Test](https://github.com/fsprojects/FAKE/actions/workflows/build_and_test.yml/badge.svg)](https://github.com/fsprojects/FAKE/actions/workflows/build_and_test.yml)
[![Join the chat at https://gitter.im/fsharp/FAKE](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/fsharp/FAKE?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

"FAKE - F# Make" is a cross platform build automation system. Due to its integration
in F#, all benefits of the .NET Framework and functional programming can be used, including
the extensive class library, powerful debuggers and integrated development environments like
Visual Studio or MonoDevelop, which provide syntax highlighting and code completion.

The new DSL was designed to be succinct, typed, declarative, extensible and easy to use.

See the [project home page](https://fake.build/) for tutorials and [API documentation](https://fake.build/apidocs/v5/index.html).

## Install the project

See detailed instructions on [how to install](https://fake.build/fake-gettingstarted.html#Install-FAKE) the project.

## Build the project

* Either: Download and install the [Dotnet SDK](https://www.microsoft.com/net/learn/get-started) and run `dotnet tool restore` followed by `dotnet fake build`
* Or: Install FAKE 5 (for example via `choco install fake -pre`) and run `fake build`

> Note: You can find more details on the [contributing page](https://fake.build/contributing.html)

Make sure to have long path enabled: https://superuser.com/questions/1119883/windows-10-enable-ntfs-long-paths-policy-option-missing
Otherwise the test-suite will fail (However, the compilation should work)

| Package Name         | Nuget                                                                                                                |
| -------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `Fake-Cli`           | [![NuGet Badge](https://buildstats.info/nuget/Fake-Cli)](https://www.nuget.org/packages/Fake-Cli)                    |
| `Fake.Core.Target`   | [![NuGet Badge](https://buildstats.info/nuget/Fake.Core.Target)](https://www.nuget.org/packages/Fake.Core.Target)    |
| `Fake.Core.Context`  | [![NuGet Badge](https://buildstats.info/nuget/Fake.Core.Context)](https://www.nuget.org/packages/Fake.Core.Context)  |
| Legacy FAKE          | [![NuGet Badge](https://buildstats.info/nuget/FAKE)](https://www.nuget.org/packages/FAKE)                            |

## How to contribute code

See the [contributing page](https://fake.build/contributing.html).

## Maintainers

Although this project is hosted in the [fsharp](https://github.com/fsharp) repository for historical reasons, it is _not_ maintained and managed by the F# Core Engineering Group. The F# Core Engineering Group acknowledges that the independent owner and maintainer of this project is [Steffen Forkmann](https://github.com/forki).
