# FAKE - F# Make

[![Join the chat at https://gitter.im/fsharp/FAKE](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/fsharp/FAKE?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

"FAKE - F# Make" is a cross platform build automation system. Due to its integration 
in F#, all benefits of the .NET Framework and functional programming can be used, including 
the extensive class library, powerful debuggers and integrated development environments like 
Visual Studio or MonoDevelop, which provide syntax highlighting and code completion.

The new DSL was designed to be succinct, typed, declarative, extensible and easy to use.

See the [project home page](https://fake.build/) for tutorials and [API documentation](http://fake.build/apidocs/v5/index.html).

# Build the project

* Quick: Download and install the [Dotnet SDK](https://www.microsoft.com/net/learn/get-started) and run `dotnet build build.proj`
* Fake5: Install FAKE 5 (for example via `choco install fake -pre`) and run `fake build`

[![Azure Pipelines build status](https://dev.azure.com/fakebuild/FSProjects/_apis/build/status/FAKE-CI?branchName=release/next)](https://dev.azure.com/fakebuild/FSProjects/_build/latest?definitionId=1&branchName=release/next)
[![Travis build status](https://travis-ci.org/fsharp/FAKE.png)](https://travis-ci.org/fsharp/FAKE)
[![AppVeyor build status](https://ci.appveyor.com/api/projects/status/lk1dfo1qi99ri78f)](https://ci.appveyor.com/project/SteffenForkmann/fake) [![CircleCI build status](https://circleci.com/gh/fsharp/FAKE.svg?style=shield&circle-token=:circle-token)](https://circleci.com/gh/fsharp/FAKE) [![Gitlab CI build status](https://gitlab.com/matthid/FAKE/badges/release/next/build.svg)](https://gitlab.com/matthid/FAKE)

> Note: The build of the "legacy" parts (FAKE 4) will most likely fail
>
> - when you are on linux and do not have mono installed
> - when you are on windows and do not have Visual Studio installed
>
> Either build only the `netcore` parts via `fake build target FullDotNetCore` or install the requirements according to the [contributing page](http://fsharp.github.com/FAKE/contributing.html)

Make sure to have long path enabled: https://superuser.com/questions/1119883/windows-10-enable-ntfs-long-paths-policy-option-missing
Otherwise the test-suite will fail (However, the compilation should work)

[![NuGet Badge](https://buildstats.info/nuget/FAKE)](https://www.nuget.org/packages/FAKE)
[![NuGet Badge](https://buildstats.info/nuget/Fake.Core.Target)](https://www.nuget.org/packages/Fake.Core.Target) [![NuGet Badge](https://buildstats.info/nuget/Fake.Core.Context)](https://www.nuget.org/packages/Fake.Core.Context)

## How to contribute code

See the [contributing page](http://fsharp.github.com/FAKE/contributing.html).

## Maintainers

Although this project is hosted in the [fsharp](https://github.com/fsharp) repository for historical reasons, it is _not_ maintained and managed by the F# Core Engineering Group. The F# Core Engineering Group acknowledges that the independent owner and maintainer of this project is [Steffen Forkmann](http://github.com/forki).
