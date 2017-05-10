# Fake dotnetcore

## Getting Started

Getting started with the Fake dotnetcore version is easy.
Just install the corresponding package for your system:

```ps
choco install fake -pre
```

now you can use 

```ps
fake --help
```


This is basically it. You can now execute fake commands.

## CLI

```
$ ./fake.sh --help
USAGE: fake [--help] [--version] [--verbose] [<subcommand> [<options>]]

SUBCOMMANDS:

    run <options>         Runs a build script.

    Use 'fake <subcommand> --help' for additional information.

OPTIONS:

    --version             Prints the version.
    --verbose, -v         More verbose output.
    --help                display this list of options.
```

```
$ ./fake.sh run --help
USAGE: fake run [--help] [--script <string>] [--target <string>] [--environmentvariable <string> <string>]
                [--debug] [--singletarget] [--nocache] [--fsiargs <string>]

OPTIONS:

    --script <string>     Specify the script to run. (--script is optional)
    --target, -t <string> The target to run.
    --environmentvariable, -e <string> <string>
                          Set an environment variable.
    --debug, -d           Debug the script (set a breakpoint at the start).
    --singletarget, -s    Run only the specified target.
    --nocache, -n         Disable caching of the compiled script.
    --fsiargs <string>    Arguments passed to the f# interactive.
    --help                display this list of options.
```

Note that `./fake.sh run build.fsx` is valid (as `--script` is optional.)

## Buildserver support

AppVeyor: https://github.com/matthid/FAKE/blob/coreclr/appveyor.yml
Travis: https://github.com/matthid/FAKE/blob/coreclr/.travis.yml

## Why?

The goals are:

 - Provide a easy to use cross platform way to use FAKE. With a good bootstrapping experience
 - Cleanup 'FakeLib' 
 - Extract reusable libraries and make them usable for your projects or the fsi!
 - Make it easier to extend FAKE for your own use-case
 - Provide an easy way for simple scripting, automate everything, everywhere.

Please read https://github.com/fsharp/FAKE/issues/1232

## What is the migration path?

See [Fake 5 Migration Guide](migrate-to-fake5.html)

## How to specify dependencies?

The Fake runtime will restore packages before running the script. All you need to do is specify them.
Fake uses [Paket](https://fsprojects.github.io/Paket/) and a special annotation for integration.

### Specify by paket.dependencies (recommended, but not jet implemented)

The easiest way for projects already using Paket is to annotate a group in the paket.dependencies file

```
group netcoreBuild // FAKE GROUP
```

Fake will search for the comment and use the dependencies for the given group. If it finds no marked group or the script has a FAKE Header, Fake will ignore the dependencies file.

### Fake HEADER

To tell Fake which dependencies are needed a script can start with a header as well:

```
(* -- Fake Dependencies ***header***
*** Dependencies ***
-- Fake Dependencies -- *)
#load "./.fake/build.fsx/loadDependencies.fsx"
```

The last line `#load` is not required, however
this way the file can still be edited in editors (after restoring packages initially).
Fake will write a `loadDependencies.fsx` file for you importing all required references.

There are two headers known by Fake:

#### paket-inline

This way you can specify all your dependencies via pakets `paket.dependencies` syntax inline in your Fake script.
Fake will implicitly use the "Main" paket group for the script.

```
(* -- Fake Dependencies paket-inline
source http://nuget.org/api/v2

nuget Fake.Travis
nuget Fake.MsBuild
nuget FSharp.Formatting ~> 2.14
-- Fake Dependencies -- *)
#load "./.fake/build.fsx/loadDependencies.fsx"
```

> For now you probably want to add `source .fake/bin/core-v1.0-alpha-10/packages` if you want to use the FAKE Api.

#### paket.dependencies

It's also possible to use an existing `paket.dependencies` file and specify the file and group to use (defaults to "paket.dependencies" and "Main"):

```
(* -- Fake Dependencies paket.dependencies
file ./paket.dependencies
group Build
-- Fake Dependencies -- *)
#load "./.fake/build.fsx/loadDependencies.fsx"
```

## Portable installation

We distrubute a dotnetcore version of FAKE without the dotnetcore runtime.
This version assumes an existing dotnet sdk installation while the non-portable installation doesn't.

Just use the `-portable` version of the downloads, extract it and execute.

```
dotnet Fake.dll <regular-arguments>
```

The advantage of this method is that it is portable (ie. distribute the same binaries) and requires less bandwidth.
The disadvantage is that you need to have a dotnet sdk installed.

## Examples

- See https://github.com/matthid/FAKE/blob/coreclr/build.fsx
  Note that with the "new" API you should call the modules directly instead of opening them. 
  Therefore this example is actually pretty bad because it just opened everything (for minimal diff to the "normal" build.fsx)

TBD.

## Downloads

Currently Releases are on my branch uploaded to AppVeyor on Build:
 - https://ci.appveyor.com/project/SteffenForkmann/fake -> Find the latest build from my branch -> Artifacts
 - https://ci.appveyor.com/project/SteffenForkmann/fake/build/1.0.3115/artifacts
 - Or go directly via github -> https://github.com/fsharp/FAKE/pull/1281 -> Find last commit -> Naviate to the last green AppVeyor build -> Artifacts
You need to use the https://ci.appveyor.com/nuget/fake feed in your build scripts as long as there is no NuGet release of FAKE 5