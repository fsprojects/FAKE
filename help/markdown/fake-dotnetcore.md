# Fake dotnetcore

## Getting Started

Getting started with the Fake dotnetcore version is easy.
Just install the corresponding package for your system:

- Windows (chocolatey)

  - Install chocolatey (a windows package manager) if you have not installed it yet (see [https://chocolatey.org](https://chocolatey.org)).
    Basically open an admin `cmd.exe` and paste 

    <pre>
        <code class="lang-batch">
        @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"
        </code>
    </pre>

  - Install fake via chocolatey

    In an admin `cmd.exe` enter:

    <pre>
        <code class="lang-bash">
        choco install fake
        </code>
    </pre>

  - There are [other ways to get started](fake-gettingstarted.html#Install-FAKE) if chocolatey / an administrator-cmd or global installation is not an option.

- Dotnet sdk global tool

  - Install the .NET sdk (at least `2.1.300`)
  - Install FAKE

    <pre>
        <code class="lang-bash">
        dotnet tool install fake-cli -g
        </code>
    </pre>

- Debian package

  - the .deb file can be downloaded from the [releases-page](https://github.com/fsharp/FAKE/releases)

- Others

  We currently do not have packages for the various distributions, look for [other ways to install fake](fake-gettingstarted.html#Install-FAKE).
  Please consider [helping out!](https://github.com/fsharp/FAKE/issues/1828).

now you can use

<pre><code class="lang-bash">fake --help</code></pre>

This is basically it. You can now execute fake commands. Follow the [minimal example below](fake-dotnetcore.html#Minimal-example) for a quick start.

## FAKE Template
Use the FAKE `dotnet new` template. See [FAKE template]("fake-template.html") for more information.

- Install the template:

  <pre><code class="lang-bash">
  dotnet new -i "fake-template::*"
  </code></pre

- Run the template:

  <pre><code class="lang-bash">
  dotnet new fake
  </code></pre>

## CLI

See [Fake command line](fake-commandline.html)

## Buildserver support

AppVeyor: [https://github.com/fsharp/FAKE/blob/master/appveyor.yml](https://github.com/fsharp/FAKE/blob/master/appveyor.yml)
Travis: [https://github.com/fsharp/FAKE/blob/master/.travis.yml](https://github.com/fsharp/FAKE/blob/master/.travis.yml)

## One note on Intellisense

This section is to clarify when an how the intellisense updates when you add new modules (short form: Delete the `<script>.fsx.lock` file and re-run fake to update all files)

* Assume you have a script `myscript.fsx` with the following contents:

  ```fsharp
  #r "paket:
  nuget Fake.Core.Target prerelease"
  #load "./.fake/myscript.fsx/intellisense.fsx"
  ```

  Where you can add all the [fake modules](fake-fake5-modules.html) you need (work through the example below if you see this the first time).

* run the script to restore your dependencies and setup the intellisense support: `fake run myscript.fsx`.
  This might take some seconds depending on your internet connection  
<div class="alert alert-info">
    <h5>INFO</h5>
    <p>The warning <code>FS0988: Main module of program is empty: nothing will happen when it is run</code> indicates that you have not written any code into the script yet.</p>
</div>

* now open the script in VS Code with ionide-fsharp extension or Visual Studio.
<div class="alert alert-info">
    <h5>INFO</h5>
    <p>
    If you change your dependencies you need to delete <code>myscript.fsx.lock</code> and run the script again for intellisense to update.
    Intellisense is shown for the full framework while the script is run as <code>netcoreapp20</code> therefore intellisense might show APIs which are not actually usable.
    </p>
</div>

## What is the migration path?

See [Fake 5 Migration Guide](fake-migrate-to-fake-5.html)

## How to specify dependencies?

See the [FAKE 5 modules](fake-fake5-modules.html) section.

## Portable installation

We distribute a dotnetcore version of FAKE without the dotnetcore runtime.
This version assumes an existing dotnet sdk installation while the non-portable installation doesn't.

Just use the `-portable` version of the downloads, extract it and execute.

<pre><code class="lang-bash">dotnet fake.dll <regular-arguments></code></pre>

The advantage of this method is that it is portable (ie. distribute the same binaries) and requires less bandwidth.
The disadvantage is that you need to have a dotnet sdk installed.

## Examples

- See [https://github.com/fsharp/FAKE/blob/master/build.fsx](https://github.com/fsharp/FAKE/blob/master/build.fsx)
  Note that with the "new" API you should call the modules directly instead of opening them.
  Therefore this example is actually pretty bad because it just opened everything (for minimal diff to the "normal" build.fsx)

### Minimal example

Create a file named `build.fsx` with the following contents:

```fsharp
#r "paket:
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

Run this file by executing

<pre><code class="lang-bash">
fake run build.fsx
</code></pre>

## Downloads

Get the latest packages from GitHub: https://github.com/fsharp/FAKE/releases

Get the latest binaries from chocolatey: https://chocolatey.org/packages/fake

Get the latest dotnet-fake cli tool by adding `<DotNetCliToolReference Include="dotnet-fake" Version="5.*" />` to your dependencies: https://www.nuget.org/packages/dotnet-fake
