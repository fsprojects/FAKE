# Contributing to FAKE

Thank you for your interest in contributing to FAKE! This guide explains everything you'll need to know to get started.

__Before diving in__, please note:

* You are encouraged to **improve this document** by sending a pull request to the FAKE project on GitHub. If you learn something while playing with FAKE, please record your [findings here](https://github.com/fsharp/FAKE/blob/release/next/help/markdown/contributing.md)!

* If you'd like to discuss a feature (a good idea!) or are looking for suggestions on how to to contribute:
  * **Check the [Issue list](https://github.com/fsharp/FAKE/issues)** on GitHub,
  * **Visit the [Gitter room](https://gitter.im/fsharp/FAKE)**

* Unless you explicitly state otherwise, any contribution intentionally submitted for inclusion in the Project shall be under the terms and conditions of the **Apache 2.0 license**. See [`/License.txt`](https://github.com/fsharp/FAKE/tree/release/next/License.txt) for details.

## Prerequisites

Before building and developing FAKE, you must:

### Install F#

**Linux and Mac** users should install the .NET Core SDK and Mono per this guide, "[Cross-Platform Development with F#](http://fsharp.org/guides/mac-linux-cross-platform/)".

**Windows** users can install **Visual Studio**. The [Community Edition](https://www.visualstudio.com/de/vs/community/) 
  is freely available for open-source projects.

<div class="alert alert-info">
    <h5>INFO</h5>
    When developing on Windows, make sure to have long paths enabled (<a target="_blank" rel="noopener noreferrer" href="https://superuser.com/questions/1119883/windows-10-enable-ntfs-long-paths-policy-option-missing">instructions available here</a>), otherwise the test-suite will fail -- although, the build should work.
</div> 

### Install an Editor

For FAKE development, [Visual Studio Code](https://code.visualstudio.com/Download) with [Ionide](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp) is highly recommended. The following IDEs are also excellent choices:

- JetBrains Rider [[download](https://www.jetbrains.com/rider/download)]
- Visual Studio for Mac [[download](https://visualstudio.microsoft.com/vs/mac/)]
- Visual Studio for Windows [[download](https://visualstudio.microsoft.com/vs/)]


### Install FAKE

You can quickly install and use FAKE with the dotnet SDK (we use fake-cli as local tool):
<pre>
    <code class="lang-bash">
    cd /projects/FAKE
    dotnet tool restore
    dotnet fake --version
    </code>
</pre>

For alternative methods of installing FAKE, please see the [Getting Started guide](fake-gettingstarted.html#Install-FAKE).

## Creating Pull Requests

1. **Fork** the [FAKE repo on GitHub](https://github.com/fsharp/FAKE).

2. **Clone** your personal fork locally.

3. **Add a new git remote** in order to retrieve upstream changes.
<pre>
    <code class="lang-bash">
    git remote add upstream https://github.com/fsharp/FAKE.git
    </code>
</pre>

4. **Checkout** the `release/next` branch. 
<pre>
    <code class="lang-bash">
    git checkout release/next
    </code>
</pre>

5. To verify that everything works, **build** `release/next` via:
<pre>
    <code class="lang-bash">
    dotnet fake build
    </code>
</pre>

6. Create a new **feature branch**.
<pre>
    <code class="lang-bash">
    git checkout -b myfeature
    </code>
</pre>

7. **Implement** your bugfix/feature.

8. Add a bit of **documentation** (see the section on [Contributing Documentation](#Contributing-Documentation)).

9. Re-run the build script to **confirm that all tests pass**.
<pre>
    <code class="lang-bash">
    dotnet fake build
    </code>
</pre>

10. **Commit** your changes, and **push** them to your fork.

11. Use GitHub's UI to <a href="https://github.com/fsharp/FAKE/pulls" target="_blank" rel="noopener noreferrer"><strong>create a pull request</strong></a>. (Write "WIP" into the pull request description if it's not completely ready.)<br/><br/>If you need to rebase you can do:
    <pre>
        <code class="lang-bash">
        git fetch upstream
        git rebase upstream/release/next
        git push origin myfeature -f
        </code>
    </pre>

    The pull request will be updated automatically.

## Contributing Documentation

The code for all documentation can be found in the [`help` directory on GitHub](https://github.com/fsharp/FAKE/tree/release/next/help). If you find a bug or add a new feature, *make sure you document it*!

### Building the Documentation

Documentation for FAKE is automatically generated using **the amazing [F# Formatting](https://github.com/tpetricek/FSharp.Formatting) library**.
It turns Markdown files `*.md` with embedded code snippets and F# script `*.fsx` files containing embedded Markdown documentation into nice HTML documentation.

To build the documentation from scratch, simply run: 
<pre>
    <code class="lang-bash">
    dotnet fake build target GenerateDocs
    </code>
</pre>

To save time, you may skip the prerequisite build steps and run the `GenerateDocs` target directly using the single target `-s` switch:
<pre>
    <code class="lang-bash">
    dotnet fake build -s target GenerateDocs
    </code>
</pre>
(Note: this assumes binaries are already built and have not been modified.)


### Viewing the Documentation

Running the following target spins up a webserver on localhost and opens the newly built docs in a browser window:
<pre>
    <code class="lang-bash">
    dotnet fake build target HostDocs
    </code>
</pre>

## Testing Modules

If you make a change to a module and would like to test it in a fake script, the easiest way to do this is to create a local nuget package and reference it in your script per the steps below:

1. Create a local nuget package for the module you've changed.  
e.g: Using dotnet cli
<pre>
    <code class="lang-bash">
    cd path/to/project
    dotnet pack
    </code>
</pre>

2. `dotnet pack` will create a default nuget package with version of 1.0.0 in the `bin/Debug` of your project. Set an additional paket source in your build script to this directory, and require this exact version in your paket references.
  
    e.g: If you wanted to test a local build of Fake.DotNet.NuGet

        #r "paket:
        source path/to/Fake.DotNet.NuGet/bin/Debug/
        source https://api.nuget.org/v3/index.json
        ...Other Dependencies...
        nuget Fake.DotNet.NuGet == 1.0.0 //" //Require version 1.0.0, which is the local build

## Style Guidlines

* When working on FAKE 5, [Visual Studio Code](https://code.visualstudio.com/) with [Ionide](http://ionide.io/) helps a lot!

* Read the [F# component design guidelines](http://fsharp.org/specs/component-design-guidelines/).

* Read the [API Design Guidelines](#API-Design-Guidelines) below.

* Add documentation for your feature

* If you add new markdown documentation, make sure to link if from an existing site. Ideally, add it to the [menu](https://github.com/fsharp/FAKE/blob/release/next/help/templates/template.cshtml)

* If you write API documentation but no extra markdown, please consider adding it to the menu as well.

### API Design Guidelines

[We've learned from our mistakes](fake-fake5-learn-more.html) and adopted new API design guidelines. **Please read them very carefully**, and please ask if you don't understand any of the following rules:

* `[<AutoOpen>]` is no longer used
* We replace `<verb><module>` functions with `<module>.<verb>`
  * Use Verbs as much as possible for functions
  * In order to have a more consistent API, we propose to always use camelCase naming for functions
  * For historic reasons, we allow constants and values in PascalCase. (They will not have a "verb" as they don't do anything)
  * If we provide optional parameters (via `static member`), we use PascalCase as well (Example: `Shell`-module)
* We assume the caller is not opening the module but only the global namespaces `Fake.Core`, `Fake.IO`, ...
  and make sure the code looks nice and structured on the caller side.
* For compatibility reasons (migration from legacy), we assume the user doesn't open the global `Fake` namespace.

  -> This means we don't add anything in there in the new API.
* Old APIs are marked as Obsolete with a link (hint) to the new API location. We use codes to make explicit 
  * FAKE0001 for moving part from one Module to another
  * FAKE0002 for removed API we don't know who is using it and how => please open an issue if you use it
  * FAKE0003 for API that is no more accessible (basically became internal) => please open an issue if you use it
  * FAKE0004 for API not yet migrated, waiting for your contribution
* Operators are opened seperatly with a separate `Operators` module
* We avoid the `Helpers` suffix (because we now expect users to write `<module>.<function>`)
* We generally use the `[<RequireQualifiedAccess>]` attribute on modules.

## Considerations Regarding FAKE 4

* Fake 4 (FakeLib) is in maintainance mode. Therefore new features need to be at least available as new FAKE 5 module (that might mean that the old module needs to be migrated as part of the PR).

* Fake 4 still allows hotfixes. Please send the PR against the [hotfix_fake4 branch](https://github.com/fsharp/FAKE/tree/hotfix_fake4).

  It would be helpful if a second PR against `release/next` is sent that merges the hotfix into `release/next` and adds the hotfix to the FAKE 5 code as well.

## Porting Modules to FAKE 5

As mentioned in the ["Fake 5 learn more"](fake-fake5-learn-more.html) section, we could use your help porting modules to FAKE 5. To save you from some pitfalls, this section provides a working approach to migrating modules.

Try the following:

* Copy one of the existing netcore projects and edit the project file by hand (rename)
* Copy the old implementation files from `src/app/FakeLib` to `/src/app/Fake.<ModuleType>.<Name>` (update project file again if required)
* (Optionally, there is no need for new stuff to appear in FakeLib at this point) Reference the new files in FakeLib (again updating `FakeLib.fsproj` by hand to properly reference the stuff)
* Open `Fake.sln`, add the project and go from there.
* Once stuff compiles in the (`Fake.sln`) solution you are usually good to go. Let us know if you struggle at this point (in the PR or a new issue).
* Add the info about the new module to the `dotnetAssemblyInfos` variable in `build.fsx`. From this point on the build script will let you know if anything is missing. Again, if you have problems let us know.
* Mark the old module with the `Obsolete` attribute.
* Test everything with a full `dotnet fake build`

These steps will ensure:

* People using the NuGet package will get the warnings to update the new API
* The new API is part of FakeLib (deprecated)
* The new API is available as separate module

## Staging environment

In order to test and preview our changes faster, we have a fully automated release process in place.
This staging environment is based on VSTS and MyGet.

If you ever need a release/bugfix, make sure to mention that in your PR. We can quickly provide a build on the following infrastructure:

* Website: https://staging.fake.build
* Chocolatey package: `choco install fake --version <version> --source https://www.myget.org/F/fake-chocolatey-vsts/api/v2`
* NuGet feed: https://www.myget.org/F/fake-vsts/api/v3/index.json
* GitHub Releases: https://github.com/fake-staging/FAKE/releases (if needed)

<div class="alert alert-info">
  <h5>INFO</h5>
     Because of package retention policies those builds will not be available forever! We will quickly release the builds once everything works.
     Those bits should be considered for "unblocking"-purposes or testing only.
</div>

The [release process](https://fakebuild.visualstudio.com/FSProjects/_releases2?definitionId=1&view=mine&_a=releases) is publicly available as well.
