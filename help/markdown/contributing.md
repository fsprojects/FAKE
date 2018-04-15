# Contributing to FAKE

This page should provide you with some basic information if you're thinking about contributing to FAKE.

* This page can be edited by sending a pull request to the FAKE project on GitHub, so if you learn something when playing with FAKE please record your [findings here](https://github.com/fsharp/FAKE/blob/master/help/markdown/contributing.md)!

* If you want to discuss a feature (a good idea!), or if you want to look at suggestions how you might contribute, check out:
  * the [Issue list](https://github.com/fsharp/FAKE/issues) on GitHub,
  * [Gitter room](https://gitter.im/fsharp/FAKE)

* Unless you explicitly state otherwise, any contribution intentionally submitted for inclusion in the Project shall be under the terms and conditions of the Apache 2.0 license. See License.txt for details.

## Documentation

The documentation for FAKE is automatically generated using the amazing [F# Formatting](https://github.com/tpetricek/FSharp.Formatting) library.
It turns `*.md` (Markdown with embedded code snippets) and `*.fsx` files (F# script file with embedded Markdown documentation) into a nice HTML documentation.

* The code for all the documents can be found in the `help` directory [on GitHub](https://github.com/fsharp/FAKE/tree/master/help). If you find a bug or add a new feature, make sure you document it!

* If you want to build the documentation, simply run the build script ([GitHub link](https://github.com/fsharp/FAKE/blob/master/build.fsx)) which builds the documentation.

## Creating pull requests

### Prerequisites

#### Git / GitHub

* Fork the [FAKE repo on GitHub](https://github.com/fsharp/FAKE).

* Clone your personal fork locally.

* Add a new git remote in order to retrieve upstream changes.

        git remote add upstream https://github.com/fsharp/FAKE.git

#### Build tools

* Windows users can install Visual Studio 2017 (the [Community Edition](https://www.visualstudio.com/de/vs/community/) 
  is freely available for open-source projects).

  > Make sure to have long path enabled: https://superuser.com/questions/1119883/windows-10-enable-ntfs-long-paths-policy-option-missing
  > Otherwise the test-suite will fail (However, the build should work)

* Linux and Mac users can read "[Guide - Cross-Platform Development with F#](http://fsharp.org/guides/mac-linux-cross-platform/)" 
  to find out the required tools.

* Install FAKE
  * For example on windows run `choco install fake -pre` (remember to update from time to time with `choco update fake -pre`)
  * On unix we don't have fake properly packaged yet (please HELP!). You can use the steps outlined in [`.travis.yml`](https://github.com/fsharp/FAKE/blob/master/.travis.yml#L14-L18)
  * alternatively you can install the dotnet-sdk and use `dotnet restore build.proj` and `dotnet fake <arguments>` instead of `fake <arguments>`

* Alternately, you can use [Vagrant](https://www.vagrantup.com/) in-pair with [VirtualBox](https://www.virtualbox.org/) 
  to automatically deploy a preconfigured virtual machine. See the [Vagrant docs](vagrant.html) to get in touch with the tool.

> Note: The vagrant file might be outdated at this time, please help updating it and removing this banner.

* Ubuntu / Windows Subsystem for Linux:
  * Install Mono, as of today 2017-09-30 you need at least alpha to have the msbuild package (http://www.mono-project.com/download/beta/#download-lin)
  * `apt-get install msbuild mono-complete`
  * `apt-cache search libunwind`
  * `# apt-get Install the libunwind runtime (one of the search results)`
  * `apt-cache search libcurl # Install`
  * `# apt-get Install the libcurl library (one of the search results)`
  * `./build.sh`

### Programming

* Checkout the `master` branch.

* Run the build via `fake run build.fsx` in order to check if everything works.

* Create a new feature branch.

        git checkout -b myfeature

* Implement your bugfix/feature.

* Add a bit of documentation (see above).

* Run the build script again, to confirm that all tests pass.

* Commit and push to your fork.

* Use GitHub's UI to create a pull request.
    Write "WIP" into the pull request description if it's not completely ready

* If you need to rebase you can do:

        git fetch upstream
        git rebase upstream/master
        git push origin myfeature -f

* The pull request will be updated automatically.

## General considerations

* Fake 4 (FakeLib) is in maintainance mode. Therefore new features need to be at least available as new FAKE 5 module (that might mean that the old module needs to be migrated as part of the PR).

* Fake 4 still allows hotfixes, please send the PR against the https://github.com/fsharp/FAKE/tree/hotfix_fake4

  It would be helpful if a second PR against master is sent which merges the hotfix into master and adds the hotfix to the FAKE 5 code as well.

## Text editor / Code style

* When working on FAKE 5 core stuff [Visual Studio Code](https://code.visualstudio.com/) with [Ionide](http://ionide.io/) help a lot!

* Read the [F# component design guidelines](http://fsharp.org/specs/component-design-guidelines/).

* Read the API-Design-Guidelines below.

## API-Design

We [learned from our mistakes](fake-fake5-learn-more.html), so we use the following guidelines, **please read them very carefully** (ask if you don't understand any rule):

* AutoOpen is no longer used
* We replace `<verb><module>` functions with `<module>.<verb>`
  * Use Verbs as much as possible for functions
  * In order, to have a more consistent API we propose to always use camelCase naming for functions
  * For historic reasons we allow constants and values in PascalCase (they will not have a "verb" as they don't do anything)
  * If we provide optional parameters (via `static member`) we use PascalCase as well (Example: `Shell`-module)
* We assume the caller is not opening the module but only the global namespaces `Fake.Core`, `Fake.IO`, ...
  and make sure the code looks nice and structured on the caller side.
* For compatibility reasons (migration from legacy). We assume the user doesn't open the global `Fake` namespace.

  -> This means we don't add anything in there in the new API.
* Old APIs are marked as Obsolete with a link (hint) to the new API location. We use codes to make explicit 
  * FAKE0001 for moving part from one Module to another
  * FAKE0002 for removed API we don't know who is using it and how => please open an issue if you use it
  * FAKE0003 for API that is no more accessible (basically became internal) => please open an issue if you use it
  * FAKE0004 for API not yet migrated, waiting for your contribution
* Operators are opened seperatly with a separate `Operators` module
* We avoid the `Helpers` suffix (because we now expect users to write `<module>.<function>`)

## Porting a module to FAKE 5

As mentioned in the ["Fake 5 learn more"](fake-fake5-learn-more.html) section we have a large list to choose from to help the project. One of these things is porting modules to FAKE 5. To save you from some pitfalls this sections guides you in migrating modules with an (at least for me) working approach.

Tooling in netcore it not optimal yet so some things have to be done by hand, but with these steps you have pretty good IDE support:

* Copy one of the existing netcore projects and edit the project file by hand (rename)
* Copy the old implementation files from `src/app/FakeLib` to `/src/app/Fake.<ModuleType>.<Name>` (update project file again if required)
* Reference the new files in FakeLib (again updating `FakeLib.fsproj` by hand to properly reference the stuff)
* Open `Fake.sln` and go from there. Because in F# you can only reference stuff defines in files ABOVE, this is ALMOST perfect
* Once stuff compiles in the (`Fake.sln`) solution the remaining changes to make the netcore project compile are usually straightforward (you basically only need to fix project references or add framework nuget packages if needed). Let us know if you struggle at this point (in the PR or a new issue).
* Add the info about the new module to the `dotnetAssemblyInfos` variable in `build.fsx`. From this point on the build script will let you know if anything is missing. Again, if you have problems let us know.
* Mark the old module with the `Obsolete` attribute.

> Note that `src/Fake-netcore.sln` is currently not used (as IDEs don't support that yet). However it is used so speed up the build, `fake run build.fsx` will let you know what to do in the error message.

These steps will ensure:

* People using the NuGet package will get the warnings to update the new API
* The new API is part of FakeLib (deprecated)
* The new API is available as separate module
