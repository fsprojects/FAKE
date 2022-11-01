# Contributing to FAKE

Table of Content:

* [Prerequisites](#Prerequisites)
    * [Install F#](#Install-F)
    * [Install an Editor](#Install-an-Editor)
    * [Install FAKE](#Install-FAKE)
* [Creating Pull Requests](#Creating-Pull-Requests)
* [Contributing Documentation](#Contributing-Documentation)
    * [Building the Documentation](#Building-the-Documentation)
    * [Viewing the Documentation](#Viewing-the-Documentation)
* [Testing Modules](#Testing-Modules)
* [Style Guidelines](#Style-Guidelines)
    * [API Design Guidelines](#API-Design-Guidelines)
* [Considerations Regarding FAKE 4](#Considerations-Regarding-FAKE-4)
* [Porting Legacy Modules to Current Version of FAKE](#Porting-Legacy-Modules-to-Current-Version-of-FAKE)
* [Release Process](#Release-Process)
* [Staging environment](#Staging-environment)
* [Notes for Maintainers](#Notes-for-Maintainers)

Thank you for your interest in contributing to FAKE! This guide explains everything you'll need to know to get started.

__Before diving in__, please note:

* You are encouraged to **improve this document** by sending a pull request to the FAKE project on GitHub. If you learn 
* something while playing with FAKE, please record your [<ins>findings here</ins>](https://github.com/fsprojects/FAKE/blob/master/docs/guide/contributing.md)!

* If you'd like to discuss a feature (a good idea!) or are looking for suggestions on how to to contribute:
  * **Check the [<ins>Issue list</ins>](https://github.com/fsprojects/FAKE/issues)** on GitHub,
  * **Visit the [<ins>Gitter room</ins>](https://gitter.im/fsprojects/FAKE)**

* Unless you explicitly state otherwise, any contribution intentionally submitted for inclusion in the Project shall 
* be under the terms and conditions of the **Apache 2.0 license**. See [*<ins>`/License.txt`</ins>*](https://github.com/fsprojects/FAKE/tree/master/License.txt) for details.

## Prerequisites

Before building and developing FAKE, you must:

### Install F#

**Linux and Mac** users should install the .NET Core SDK and Mono per this guide, 
"[<ins>Cross-Platform Development with F#</ins>](http://fsharp.org/guides/mac-linux-cross-platform/)".

**Windows** users can install **Visual Studio**. The [<ins>Community Edition</ins>](https://www.visualstudio.com/de/vs/community/) 
  is freely available for open-source projects.

> When developing on Windows, make sure to have long paths enabled 
> ([<ins>instructions available here</ins>](https://superuser.com/questions/1119883/windows-10-enable-ntfs-long-paths-policy-option-missing)), 
> otherwise the test-suite will fail -- although, the build should work. 

### Install an Editor

For FAKE development, [<ins>Visual Studio Code</ins>](https://code.visualstudio.com/Download) 
with [<ins>Ionide</ins>](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp) is highly recommended. 
The following IDEs are also excellent choices:

- JetBrains Rider [[<ins>download</ins>](https://www.jetbrains.com/rider/download)]
- Visual Studio for Mac [[<ins>download</ins>](https://visualstudio.microsoft.com/vs/mac/)]
- Visual Studio for Windows [[<ins>download</ins>](https://visualstudio.microsoft.com/vs/)]

### Install FAKE

You can quickly install and use FAKE with the dotnet SDK (we use fake-cli as local tool):
```shell
cd /projects/FAKE
dotnet tool restore
dotnet fake --version
```

For alternative methods of installing FAKE, please see the [<ins>Getting Started guide</ins>]({{root}}guide/gettingstarted.html#Install-FAKE).

## Creating Pull Requests

1. **Fork** the [<ins>FAKE repo on GitHub</ins>](https://github.com/fsprojects/FAKE).

2. **Clone** your personal fork locally.

3. **Add a new git remote** in order to retrieve upstream changes.
```shell
git remote add upstream https://github.com/fsprojects/FAKE.git
```

4. **Checkout** the `master` branch. 
```shell
git checkout master
```

5. To verify that everything works, **build** `master` via:
```shell
dotnet fake build
```

6. Create a new **feature branch**.
```shell
git checkout -b myfeature
```

7. **Implement** your bugfix/feature.

8. Add a bit of **documentation** (see the section on [<ins>Contributing Documentation</ins>](#Contributing-Documentation)).

9. Re-run the build script to **confirm that all tests pass**.
```shell
dotnet fake build
```

10. **Commit** your changes, and **push** them to your fork.

11. Use GitHub's UI to [<ins>create a pull request</ins>](https://github.com/fsprojects/FAKE/pulls). (Write "WIP" into the pull request description if it's not completely ready.)<br/><br/>If you need to rebase you can do:
```shell
git fetch upstream
git rebase upstream/master
git push origin myfeature -f
```

The pull request will be updated automatically.

## Contributing Documentation

The code for all documentation can be found in the [<ins>`docs` directory on GitHub</ins>](https://github.com/fsprojects/FAKE/tree/master/docs). 
If you find a bug or add a new feature, **make sure you document it**!

The documentation uses the following stack:

* [<ins>TailwindCSS</ins>](https://tailwindcss.com/) as a styling framework
* [<ins>AlpineJS</ins>](https://alpinejs.dev/) as a JS framework for adding interactivity to website
* [<ins>FSDocs</ins>](https://fsprojects.github.io/FSharp.Formatting/) to generate API documentation from FAKE modules

TailwindCSS can be considered a pre step in build process, after that it will be handled to FSDocs. FSDocs is an 
**amazing [<ins>F# Docs</ins>](https://github.com/fsprojects/FSharp.Formatting) library**
that turns Markdown files `*.md` with embedded code snippets and F# script `*.fsx` files containing embedded 
Markdown documentation into nice HTML documentation.


The `docs` directory is first built using NPM by running the command `npm run build` to generate styles and other files.
Next `fsdocs` is called to generate the complete site and API documentation. FSDocs uses template pages to generate the site.
We have two template pages:

* `docs/_template.html`: used for markdown files in `guide` and `articles` directories
* `docs/reference/_template.html`: used in API documentation

The two templates are 90% identical, except some styles for markdown files to use [<ins>TailwindCSS typography plugin</ins>](https://tailwindcss.com/docs/typography-plugin).

The next part is the `docs/data.json` file. This file has the navigation and content of the site. The side navigation
for **guide** and **API Docs** is built using this file. **So to add new articles or modules to FAKE, this file need
to be updated to include the new module/article**.

Another part is the following. If you want to modify the styles, you can run TailwindCSS dev server by navigating
to `docs` directory and entering the following command in a CMD:

```shell
npm run dev
```

### Building the Documentation

To build the documentation from scratch, simply run: 

```shell
dotnet fake build target GenerateDocs
```

To save time, you may skip the prerequisite build steps and run the `GenerateDocs` target directly using the single target `-s` switch:

```shell
dotnet fake build -s target GenerateDocs
```
(Note: this assumes binaries are already built and have not been modified.)

### Viewing the Documentation

Running the following target spins up a webserver on localhost and opens the newly built docs in a browser window:

```shell
dotnet fake build target HostDocs
```

## Testing Modules

If you make a change to a module and would like to test it in a fake script, the easiest way to do this is to create a 
local nuget package and reference it in your script per the steps below:

1. Create a local nuget package for the module you've changed.  
e.g: Using dotnet cli

```shell
cd path/to/project
    dotnet pack
```

2. `dotnet pack` will create a default nuget package with version of 1.0.0 in the `bin/Debug` of your project. Set an additional paket source in your build script to this directory, and require this exact version in your paket references.
  
e.g: If you wanted to test a local build of Fake.DotNet.NuGet
```fsharp
#r "paket:
        source path/to/Fake.DotNet.NuGet/bin/Debug/
        source https://api.nuget.org/v3/index.json
        ...Other Dependencies...
        nuget Fake.DotNet.NuGet == 1.0.0 //" //Require version 1.0.0, which is the local build
```

## Style Guidelines

From FAKE v6, FAKE uses [Fantomas](https://fsprojects.github.io/fantomas/) as a code formatter and style
guideline tool. The tool will be run automatically on codebase to check if any style guidelines are not
being followed. To accomplish that, the target `CheckFormatting` in `build.fsx` script will be run on
each build of the codebase to ensure all files follow guideline. If not, then build will fail with
instructions on what should be done to follow the guideline. 

A useful way to ensure you are not waiting for build to fail when run it, is to add a GIT hook on pre-commit
to run Fantomas. Please see [Fantomas GIT hook documentation page](https://fsprojects.github.io/fantomas/docs/end-users/GitHooks.html).
For FAKE usage, you need to run the following command:
```shell
dotnet fantomas  src/app/ src/template/ src/test/ --recurse
```

For development setup, we advise the following:

* When working on FAKE 5 or above, [<ins>Visual Studio Code</ins>](https://code.visualstudio.com/) with [<ins>Ionide</ins>](http://ionide.io/) helps a lot!
* Read the [<ins>F# component design guidelines</ins>](http://fsharp.org/specs/component-design-guidelines/).
* Read the [<ins>API Design Guidelines</ins>](#API-Design-Guidelines) below.
* Add documentation for your feature
* If you add new markdown documentation, make sure to link to it from an existing site. Ideally, add it to the 
* [<ins>menu</ins>](https://github.com/fsprojects/FAKE/blob/master/help/templates/template.cshtml)
* If you write API documentation but no extra markdown, please consider adding it to the menu as well.

### API Design Guidelines

[<ins>We've learned from our mistakes</ins>](/guide/fake-history.html) and adopted new API design guidelines. 
**Please read them very carefully**, and please ask if you don't understand any of the following rules:

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

* Fake 4 (FakeLib) is in maintenance mode. Therefore new features need to be at least available as a new FAKE 5 and above module 
* (that might mean that the old module needs to be migrated as part of the PR).

* Fake 4 still allows hotfixes. Please send the PR against the [*hotfix_fake4 branch*](https://github.com/fsprojects/FAKE/tree/hotfix_fake4).

  It would be helpful if a second PR against `master` is sent that merges the hotfix into `master` 
* and adds the hotfix to the FAKE 5 and above code as well.

## Porting Legacy Modules to Current Version of FAKE

As mentioned in the [*Fake 5 and above learn more*](/guide/fake-history.html) section, we could use your help porting modules to FAKE 5. 
To save you from some pitfalls, this section provides a working approach to migrating modules.

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

## Release Process
To publish a release, merge the changes to `master` branch and prepare release notes in `RELEASE_NOTES.md` file, and trigger the `release` GitHub action providing as an input the release version.
The release process needs an admin approval, once that is approved the release action will start. FAKE release push packages to the following
registries:

1. Pushing all FAKE modules to NuGet source.
2. Pushing FAKE as a chocolatey package.
3. Publish site to GitHub Pages.

## Staging environment

In order to test and preview our changes faster, we have a fully automated release process in place.
This staging environment is based on VSTS and MyGet.

If you ever need a release/bugfix, make sure to mention that in your PR. We can quickly provide a build on the following infrastructure:

* Website: https://staging.fake.build
* Chocolatey package: `choco install fake --version <version> --source https://www.myget.org/F/fake-chocolatey-vsts/api/v2`
* NuGet feed: https://www.myget.org/F/fake-vsts/api/v3/index.json
* GitHub Releases: https://github.com/fake-staging/FAKE/releases (if needed)

> Because of package retention policies those builds will not be available forever! We will quickly release the builds once everything works.
> Those bits should be considered for "unblocking"-purposes or testing only.

The [<ins>release process</ins>](https://fakebuild.visualstudio.com/FSProjects/_releases2?definitionId=1&view=mine&_a=releases) is publicly available as well.

## Notes for Maintainers

FAKE uses GitHub actions in build process. We have two GitHub actions; the `build_and_test` and `release` actions.
The `build_and_test` action is the action triggered on PRs and pushes to `master` branch. To validate changes
in a PR.

The `release` action is responsible on release a new release of FAKE. It is triggered manually and needs an admin 
approval be it kick-off the release process. The `release` action uses API Keys to interact with services that 
packages will be pushed to. These keys are hosted in production environment in GitHub repository.
