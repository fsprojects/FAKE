# FAKE - F# Make

[![Join the chat at https://gitter.im/fsharp/FAKE](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/fsharp/FAKE?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

"FAKE - F# Make" is a cross platform build automation system. Due to its integration 
in F#, all benefits of the .NET Framework and functional programming can be used, including 
the extensive class library, powerful debuggers and integrated development environments like 
Visual Studio or MonoDevelop, which provide syntax highlighting and code completion.

The new DSL was designed to be succinct, typed, declarative, extensible and easy to use.

See the [project home page](http://fsharp.github.com/FAKE/) for tutorials and [API documentation](http://fsharp.github.io/FAKE/apidocs/index.html).

# Build the project

* Mono: Run *build.sh*  [![Travis build status](https://travis-ci.org/fsharp/FAKE.png)](https://travis-ci.org/fsharp/FAKE)
* Windows: Run *build.cmd* [![AppVeyor build status](https://ci.appveyor.com/api/projects/status/lk1dfo1qi99ri78f)](https://ci.appveyor.com/project/SteffenForkmann/fake)
  Make sure to have long path enabled: https://superuser.com/questions/1119883/windows-10-enable-ntfs-long-paths-policy-option-missing
Otherwise the test-suite will fail (However, the build should work)

[![Issue Stats](http://www.issuestats.com/github/fsharp/FAKE/badge/pr)](http://www.issuestats.com/github/fsharp/FAKE) [![Issue Stats](http://www.issuestats.com/github/fsharp/FAKE/badge/issue)](http://www.issuestats.com/github/fsharp/FAKE)

[![NuGet Badge](https://buildstats.info/nuget/FAKE)](https://www.nuget.org/packages/FAKE)

## How to contribute code

See the [contributing page](http://fsharp.github.com/FAKE/contributing.html).

## Maintainers

Although this project is hosted in the [fsharp](https://github.com/fsharp) repository for historical reasons, it is _not_ maintained and managed by the F# Core Engineering Group. The F# Core Engineering Group acknowledges that the independent owner and maintainer of this project is [Steffen Forkmann](http://github.com/forki).
