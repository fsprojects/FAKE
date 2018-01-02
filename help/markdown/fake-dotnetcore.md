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

For unix we don't have packages jet (please contribute!), but you can use the manual install process (see [Contributing](contributing.html))

## CLI

See [Fake command line](fake-commandline.html)

## Buildserver support

AppVeyor: [https://github.com/fsharp/FAKE/blob/master/appveyor.yml](https://github.com/fsharp/FAKE/blob/master/appveyor.yml)
Travis: [https://github.com/fsharp/FAKE/blob/master/.travis.yml](https://github.com/fsharp/FAKE/blob/master/.travis.yml)

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

See the [FAKE 5 modules](fake-fake5-modules.html) section.

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

- See [https://github.com/fsharp/FAKE/blob/master/build.fsx](https://github.com/fsharp/FAKE/blob/master/build.fsx)
  Note that with the "new" API you should call the modules directly instead of opening them. 
  Therefore this example is actually pretty bad because it just opened everything (for minimal diff to the "normal" build.fsx)

### Minimal example

Create a file named build.fsx with the following contents:
```
(* -- Fake Dependencies paket-inline
source https://api.nuget.org/v3/index.json

nuget Fake.Core.Target
-- Fake Dependencies -- *)
// include Fake modules, see Fake modules section

open Fake.Core

// *** Define Targets ***
Target.Create "Clean" (fun _ ->
  Trace.log " --- Cleaning stuff --- "
)

Target.Create "Build" (fun _ ->
  Trace.log " --- Building the app --- "
)

Target.Create "Deploy" (fun _ ->
  Trace.log " --- Deploying app --- "
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "Deploy"

// *** Start Build ***
Target.RunOrDefault "Deploy"
```

Run this file by executing
```
fake run build.fsx
```

## Downloads

Get the latest alpha packages from GitHub: https://github.com/fsharp/FAKE/releases
