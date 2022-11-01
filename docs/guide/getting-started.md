# Getting Started

Table of Content:

* [Different ways to run FAKE](#Different-ways-to-run-FAKE)
    * [Run FAKE using FAKE runner](#Run-FAKE-using-FAKE-runner)
    * [Run FAKE using F# interactive (FSI)](#Run-FAKE-using-F-interactive-FSI)
    * [Run FAKE using a dedicated build project](#Run-FAKE-using-a-dedicated-build-project)
* [Install FAKE Runner](#Install-FAKE-Runner)
    * [One note on Intellisense](#One-note-on-Intellisense)
* [Examples](#Examples)
    * [Minimal Example](#Minimal-Example)
    * [Compiling and Building your .NET Application](#Compiling-and-Building-your-NET-Application)
        * [Getting Started](#Getting-Started)
        * [Cleaning the Last Build Output](#Cleaning-the-Last-Build-Output)
        * [Compiling the Application](#Compiling-the-Application)
        * [Compiling Test Projects](#Compiling-Test-Projects)
        * [Running the Tests with NUnit](#Running-the-Tests-with-NUnit)
    * [Advanced Examples](#Advanced-Examples)
* [Runtime Assemblies in FAKE Runner](#Runtime-Assemblies-in-FAKE-Runner)
* [Get to Know FAKE CLI](#Get-to-Know-FAKE-CLI)
* [What's Next?](#What-s-Next)


In this tutorial you will learn how to set up a complete build infrastructure with FAKE. This includes: 
how to install and use the latest FAKE version, how to edit and run scripts, how to automatically compile 
your C# or F# projects, and finally how to automatically run NUnit tests on your projects.

> If you are interested in what FAKE actually is, see our [<ins>FAQ</ins>](/guide/what-is-fake.html).

But before going into these details, let's first discuss the possible ways you can use to run FAKE.


## Different ways to run FAKE
FAKE is built with modularity in mind, in which it doesn't constrain you with one way to write or run your build
scripts. Over the years, FAKE evolves and allowed users to consume and run FAKE in multiple ways, the currently
used ways are:

* Run FAKE using FAKE runner
* Run FAKE using F# interactive (FSI)
* Run FAKE using a dedicated build project

Let's discuss each approach.

### Run FAKE using FAKE runner

The simplest way to run a build script is to use the FAKE runner. The FAKE runner was the de-facto for running build
scripts in FAKE, because it offers multiple features that were not possible before, including automatic resolution
of dependencies using NuGet or Paket dependency managers and caching which makes FAKE super fast, among others.

Assuming you have already installed FAKE runner (please see [<ins>Install FAKE Runner</ins>](#Install-FAKE-Runner) section below
on how to install FAKE runner). To get started with FAKE runner, create an F# interactive script (`build.fsx`):

```fsharp
#r "paket:
nuget Fake.Core.Target //"
// include Fake modules, see Fake modules section

open Fake.Core

// *** Define Targets ***
Target.create "Hello" (fun _ ->
  printfn "hello from FAKE!"
)

// *** Start Build ***
Target.runOrDefault "Hello"
```

Next, in a command line interface, navigate to the script directory and enter the following command (assuming
you installed FAKE runner as a global .Net tool):

```shell
fake build
```

You should see the message `hello from FAKE!` printed on the screen! You have ran your first script in FAKE!

The above script uses Paket dependency manager to resolve and download FAKE's tagret module, which allows you
to define targets and organize your script. You can check [<ins>targets module documentation</ins>](/guide/core-targets.html) 
for more info. After resolving the target module, the script open the module and define a tagret named `hello` 
which prints a message to standard output. Finally, the final statement in the script is the entry point of your
script which designate the `Hello` target as the default target to run.

The automatic resolution of dependencies, caching and many other features are provided by the FAKE runner under the hood
to make running your script a breeze.


### Run FAKE using F# interactive (FSI)

Next, let's discuss the second way, which it utilizing FSI to run our script. FAKE runner was written to provide
features that were not available back then in FSI, including the dependencies resolution using a dependency manager
such as NuGet or Paket. However, FSI has evolved as well and now it offers these capabilities.

From the previous way, note that the script is just a plain F# interactive file! Nothing special about it! However,
to successfully run this script using FSI we need to tell FSI that it need to use Paket dependency manager to resolve
the dependency we specified (FAKE's tagret module). Paket has step-by-step guide on how to let FSI knows about it, please
see [<ins>Paket's F# Interactive Integration Guide</ins>](https://fsprojects.github.io/Paket/fsi-integration.html).

After configuring Paket, now you are ready to run your script, enter the following command to run it:

```shell
dotnet si build.fsx --compilertool:"~/.nuget/packages/fsharp.dependencymanager.paket/6.0.0-alpha055/lib/netstandard2.0"
```


### Run FAKE using a dedicated build project

The last method we will discuss is using a dedicated build project to host your build script and its
dependencies. The steps you need to take include:

* Create a new fsharp console project (`build.fsproj`)
* Put your existing build script in the project (`build.fs`)
* Include your dependencies in the project using paket or nuget
* Add an `[<EntryPoint>]` to your script
* Call your project using dotnet run. Probably from a `build.cmd` or `build.sh` file

This method utilize .Net project managment and dependecy resolution and offers you possibility to stay
within project based approach. The build project is a normal .Net console application that you can manage
dependencies to it using .Net command line, and run it also using .Net command line.

The advantages of this approach is that, it saves you from cases in which FAKE runner or FSI may encouner
running your build script. Specifically, dependency resolution and when upgrading to a newer version of
.Net SDk. This approach is becoming incresingly adopted in FAKE community. Some of the projects that are
using this approach are:

* [<ins>TheAngryByrd/MiniScaffold</ins>](https://github.com/TheAngryByrd/MiniScaffold)
* [<ins>CompositionalIT/SAFE-Dojo</ins>](https://github.com/CompositionalIT/SAFE-Dojo)

You can examine these two repository to get to know the approach and how to implement it in your projects.

> Thanks to [<ins>@baronfel</ins>](https://github.com/baronfel) and [<ins>@aboy021</ins>](https://github.com/aboy021) for
> proposing this solution and discussing it in this StackOverflow question: 
[<ins>Fix for "Package manager key paket was not registered" in build.fsx</ins>](https://stackoverflow.com/questions/66665009/fix-for-package-manager-key-paket-was-not-registered-in-build-fsx/)




## Install FAKE Runner

Now that you know the possible ways to run FAKE scripts, let's zoom in and see how to install the FAKE runner.
FAKE is completely written in F# and all build scripts will also be written in F#, but this doesn't imply that you
have to learn programming in F#. In fact the FAKE syntax is hopefully very easy to learn.

There are various ways to install FAKE:

1. Install FAKE as a local `dotnet tool` (easiest, but
   needs [<ins>.NET Core SDK Version 3 or newer</ins>](https://dotnet.microsoft.com/download)): First you need to create
   a [<ins>tool manifest</ins>](https://medium.com/@bilalfazlani/net-core-local-tools-are-here-fe9ac2464481), which
   should be committed to your repository:
   ```shell
   dotnet new tool-manifest
   ```

   To install FAKE, run:
   ```shell
   dotnet tool install fake-cli
   ```

   Use `--version` to display the version of FAKE. See
   the [<ins>`local_tool`<ins>](https://github.com/FakeBuild/fake-bootstrap/tree/local_tool) branch of `fake-bootstrap`
   for ideas to bootstrap in your CI process.
   To run fake use `dotnet fake build`. To restore/download fake on another machine use `dotnet tool restore` after this
   command `dotnet fake build` should work as expected.

2. Install FAKE as a global `dotnet tool` (easiest, but
   needs [<ins>.NET Core SDK</ins>](https://dotnet.microsoft.com/download)):
   To install FAKE globally, run:
    ```shell
    dotnet tool install fake-cli -g
    ```

   To install FAKE into `your_tool_path`, run:
    ```shell
    dotnet tool install fake-cli --tool-path your_tool_path
    ```

   Use `--version` to display the version of FAKE. See
   the [<ins>`global_tool`</ins>](https://github.com/FakeBuild/fake-bootstrap/tree/global_tool) branch
   of `fake-bootstrap` for ideas to bootstrap in your CI process.
   If you run into issues, see [<ins>this</ins>](https://github.com/fsharp/FAKE/issues/2346).

3. Bootstrap via the `fake dotnet new` [<ins>template</ins>](/guide/fake-template.html). The template
   bootstraps FAKE and sets up a basic build-script. To install the template run:
    ```shell
    dotnet new -i "fake-template::*"
    ```

   Then run the template with:
    ```shell
    dotnet new fake
    ```

   See the [<ins>template</ins>](/guide/fake-template.html) page for more information. Install the `fake`
   or `fake-netcore` package for your system (
   currently [<ins>chocolatey</ins>](https://chocolatey.org/install)).
   Example `choco install fake`.
   We also provide a Debian package at the [<ins>releases-page</ins>](https://github.com/fsharp/FAKE/releases)

4. Download the portable zip.
   We distribute a .NET Core version of FAKE without the .NET Core runtime.
   This version assumes an existing dotnet sdk installation while the non-portable installation doesn't.

   Just use the `-portable` version of [<ins>the downloads</ins>](https://github.com/fsharp/FAKE/releases), extract it
   and execute.

    ```shell
    dotnet fake.dll
    ```

   The advantage of this method is that it is portable (ie. distribute the same binaries) and requires less bandwidth.
   The disadvantage is that you need to have a dotnet sdk installed and that you need to prefix all calls
   with `dotnet /full/path/to/fake.dll <args>` which is equal to `fake <args>` in other installation methods.

5. Download the runtime specific zip.
   Just use the `-<runtime>` version matching your specific platform
   of [<ins>the downloads</ins>](https://github.com/fsharp/FAKE/releases) (for example `fake-dotnetcore-win7-x64.zip`),
   extract it and execute the `fake` binary. Add the extracted binary to your `PATH` to just execute `fake` from any
   directory.

6. Bootstrap via a shell script (`fake.cmd/fake.sh`), see
   this [<ins>example project</ins>](https://github.com/FakeBuild/fake-bootstrap)
   > These scripts have no versioning story. You either need to take care of versions yourself (and lock them) or your
   > builds might break on major releases.

   now you can use
   ```shell
   fake --help
   ```
   This is basically it. You can now execute fake commands.


### One note on Intellisense

This section is to clarify when and how the intellisense updates when you add new modules (short form: Delete
the `<script>.fsx.lock` file and re-run fake to update all files).

> We recommend [<ins>Visual Studio Code</ins>](https://code.visualstudio.com/) with
> the [<ins>Ionide extension</ins>](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp)
> for best FAKE tooling support, including proper debugging.

* Assume you have a script `myscript.fsx` with the following contents:

  ```fsharp
  #r "paket:
  nuget Fake.Core.Target prerelease"
  #load "./.fake/myscript.fsx/intellisense.fsx"
  ```

  Where you can add all the [<ins>fake modules</ins>](/guide/fake-modules.html) you need (work through the
  example below if you see
  this the first time).

* run the script to restore your dependencies and setup the intellisense support: `fake run myscript.fsx`.
  This might take some seconds depending on your internet connection

> The warning `FS0988: Main module of program is empty: nothing will happen when it is run` indicates that you have not
> written any code into the script yet.

* now open the script in VS Code with ionide-fsharp extension or Visual Studio.

> If you change your dependencies you need to delete `myscript.fsx.lock` and run the script again for
> intellisense to update. Intellisense is shown for the full framework while the script is run as
> `netcoreapp20` therefore intellisense might show APIs which are not actually usable.


## Examples

This section offer detailed steps to write a FAKE script to compile, test and run a .Net application. We will start
with a meinimal example as a refresh for FAKE syntax and script structure. Then discuss the .Net application build
script, and finally, we will close this section with some advanced examples of FAKE scripts. Let's dive in.

### Minimal Example

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

This is the minimal FAKE script, which has the following major parts:
1. The `#r` directive at the top define the dependencies the project need.
2. The `// *** Define Targets ***` section define targets using FAKE target module. These targets will hold your build logic.
3. The `// *** Define Dependencies ***` section define the dependencies between your targets. In this example we have specified
that to run `Deploy`, `Build` must be run first, and to run `Build`, `Clean` must be run first. So order is `Clean` then `Build`
then `Deploy`.
4. The final statements designate the `Deploy` target as the default target to run.

Run this file by executing:

```shell
fake run build.fsx
```

### Compiling and Building your .NET Application

This example will guide you by adding a fake script to your existing .NET application.

#### Getting Started

Initially we need to create a file called `build.fsx` where all our build-logic will reside.
Create a new file with Visual Studio or Visual Studio Code (with ionide) and paste the following content:

```fsharp
#r "paket:
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"
```

This is all we need for now to declare that we need the `Fake.Core.Target` module and want to enable intellisense.

Now run `fake run build.fsx` to make fake prepare our environment. Now our IDE can load the dependencies and we will
have intellisense enabled (you might need to reopen the script file on some editors).

Now that we have setup our basic environment to edit the script file we can add our first target:

```fsharp
#r "paket:
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core

// Default target
Target.create "Default" (fun _ ->
  Trace.trace "Hello World from FAKE"
)

// start build
Target.runOrDefault "Default"
```

As you can see the code is really simple. The first few lines (`nuget Fake.Core.Target` and `open Fake.Core`) load the
fake modules we need and this is vital for all build scripts to support creating and running targets. The `#load` line
is optional but a good way to make the IDE aware of all the modules (for intellisense and IDE support)

After this header the *Default* target is defined. A target definition contains two important parts. The first is the
name of the target (here "Default") and the second is an action (here a simple trace of "Hello world").

The last line runs the "Default" target - which means it executes the defined action of the target.

Try running your new target via `fake run build.fsx` or the shortcut for a file called `build.fsx`: `fake build`

#### Cleaning the Last Build Output

A typical first step in most build scenarios is to clean the output of the last build. We can achieve this in two steps:

First change your header to the following by adding the `Fake.IO.FileSystem` module:

```fsharp
#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"
```

Now we need to remove the `build.fsx.lock` file and run `fake build` in order to restore the newly
added `Fake.IO.FileSystem` module.

Since we now can rely on intellisense we can easily discover the various modules and functions in `Fake.IO`, for example
the `Shell` module provides various functions you expect from regular shell scripting, we will use `Shell.cleanDir`
which will ensure the given directory is empty by deleting everything within or creating the directory if required:

```fsharp
#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO

// Properties
let buildDir = "./build/"

// Targets
Target.create "Clean" (fun _ ->
  Shell.cleanDir buildDir
)

Target.create "Default" (fun _ ->
  Trace.trace "Hello World from FAKE"
)

// Dependencies
open Fake.Core.TargetOperators

"Clean"
  ==> "Default"

// start build
Target.runOrDefault "Default"
```

> You can explore the APIs for example by writing `Fake.IO.` and waiting for intellisense (or pressing
`Ctrl+Space`). You can remove `Fake.IO` once you put `open Fake.IO` on top.

We introduced some new concepts in this snippet. At first we defined a global property called `buildDir` with the
relative path of a temporary build folder.

In the `Clean` target we use the `Shell.cleanDir` task to clean up this build directory. As explained above this simply
deletes all files in the folder or creates the directory if necessary.

In the dependencies section we say that the *Default* target has a dependency on the *Clean* target. In other words *
Clean* is a prerequisite of *Default* and will run before the *Default* target is executed:

![alt text](/content/img/gettingstarted/afterclean.png "We introduced a Clean target")

#### Compiling the Application

In the next step we want to compile our C# libraries, which means we want to compile all csproj-files under */src/app*
with MSBuild.

Again we need a new module for this, namely `Fake.DotNet.MSBuild`.

Just like before add the required module on top via `nuget Fake.DotNet.MSBuild`, delete the `build.fsx.lock` file and
run the script.
Now edit the script so it looks like this:

```fsharp
#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MSBuild
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.IO.Globbing.Operators //enables !! and globbing
open Fake.DotNet
open Fake.Core

// Properties
let buildDir = "./build/"

// Targets
Target.create "Clean" (fun _ ->
  Shell.cleanDir buildDir
)

Target.create "BuildApp" (fun _ ->
  !! "src/app/**/*.csproj"
    |> MSBuild.runRelease id buildDir "Build"
    |> Trace.logItems "AppBuild-Output: "
)

Target.create "Default" (fun _ ->
  Trace.trace "Hello World from FAKE"
)

open Fake.Core.TargetOperators

"Clean"
  ==> "BuildApp"
  ==> "Default"

// start build
Target.runOrDefault "Default"
```

We defined a new build target named "BuildApp" which compiles all csproj-files with the MSBuild task and the build
output will be copied to `buildDir`.

In order to find the right project files FAKE scans the folder *src/app/* and all subfolders with the given pattern (
the `!!` operator was imported from `Fake.IO.FileSystem` via `open Fake.IO.Globbing.Operators`). Therefore a similar
FileSet definition like in NAnt or MSBuild (see [<ins>project page</ins>](https://github.com/fsharp/FAKE) for details)
is used.

In addition the target dependencies are extended again. Now *Default* is dependent on *BuildApp* and *BuildApp* needs
*Clean* as a prerequisite.

This means the execution order is: Clean ==> BuildApp ==> Default.

![alt text](/content/img/gettingstarted/aftercompile.png "We introduced a Build target")

#### Compiling Test Projects

Now our main application will be built automatically and it's time to build the test project. We use the same concepts
as before:

```fsharp
#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MSBuild
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.Core


// Properties
let buildDir = "./build/"
let testDir  = "./test/"

// Targets
Target.create "Clean" (fun _ ->
  Shell.cleanDirs [buildDir; testDir]
)

Target.create "BuildApp" (fun _ ->
    !! "src/app/**/*.csproj"
    |> MSBuild.runRelease id buildDir "Build"
    |> Trace.logItems "AppBuild-Output: "
)

Target.create "BuildTest" (fun _ ->
  !! "src/test/**/*.csproj"
    |> MSBuild.runDebug id testDir "Build"
    |> Trace.logItems "TestBuild-Output: "
)

Target.create "Default" (fun _ ->
  Trace.trace "Hello World from FAKE"
)

open Fake.Core.TargetOperators
"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Default"

// start build
Target.runOrDefault "Default"
```

This time we defined a new target "BuildTest" which compiles all C# projects below *src/test/* in Debug mode and we put
the target into our build order.

#### Running the Tests with NUnit

Now all our projects will be compiled and we can use FAKE's NUnit task in order to let NUnit test our assembly (we have
to add a new module for this: `Fake.DotNet.Testing.NUnit`):

```fsharp
#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.NUnit
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Core

// Properties
let buildDir = "./build/"
let testDir  = "./test/"

// Targets
Target.create "Clean" (fun _ ->
    Shell.CleanDirs [buildDir; testDir]
)

Target.create "BuildApp" (fun _ ->
   !! "src/app/**/*.csproj"
     |> MSBuild.runRelease id buildDir "Build"
     |> Trace.logItems "AppBuild-Output: "
)

Target.create "BuildTest" (fun _ ->
    !! "src/test/**/*.csproj"
      |> MSBuild.runDebug id testDir "Build"
      |> Trace.logItems "TestBuild-Output: "
)

Target.create "Test" (fun _ ->
    !! (testDir + "/NUnit.Test.*.dll")
      |> NUnit3.run (fun p ->
          {p with
                ShadowCopy = false })
)

Target.create "Default" (fun _ ->
    Trace.trace "Hello World from FAKE"
)

// Dependencies
open Fake.Core.TargetOperators
"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"
  ==> "Default"

// start build
Target.runOrDefault "Default"
```

Our new *Test* target scans the test directory for test assemblies and runs them with the NUnit runner. FAKE
automatically tries to locate the runner in one of your subfolders. See
the [<ins>NUnit3 task documentation</ins>](/reference/fake-dotnet-testing-nunit3.html) if you need to specify the
tool path
explicitly.

The mysterious part **`(fun p -> ...)`** simply overrides the default parameters of the NUnit task and allows to specify
concrete parameters.

![alt text](/content/img/gettingstarted/alltestsgreen.png "All tests green")

### Advanced Examples

You can check these repositories for advanced usage of FAKE:
- See [FAKE <ins>`build.fsx`</ins>](https://github.com/fsprojects/FAKE/blob/master/build.fsx)
- See [SAFE-Stack <ins>`build.fsx`</ins>](https://github.com/SAFE-Stack/SAFE-template/blob/master/build.fsx)
- See [csharptofsharp <ins>`build.fsx`</ins>](https://github.com/matthid/csharptofsharp/blob/master/build.fsx)


## Runtime Assemblies in FAKE Runner

FAKE runner uses **.Net6** runtime assemblies when compiling and running a script.

## Get to Know FAKE CLI

See [<ins>Fake command line</ins>](/articles/commandline.html) for more information on command line and available
commands and options

## What's Next?

* Add more modules specific to your application and discover the Fake-APIs
* look at the [<ins>FAQ</ins>](//articles/what-is-fake.html) for some commonly asked questions.
* look at some of the samples in [<ins>FakeBuild</ins>](https://github.com/FakeBuild)
* look at [<ins>FAKEs own build script</ins>](https://github.com/fsharp/FAKE/blob/master/build.fsx) or other examples
  across the F#
  ecosystem.
* Add fake build scripts to your projects and let us know.
* Automate stuff with FAKE and use standalone scripts.
* Write your own modules and let us know - we love to add them to the navigation or announce them
  on [<ins>twitter</ins>](https://twitter.com/fsharpMake).
* Contribute :)


