# Contributing to FAKE

This page should provide you with some basic information if you're thinking about contributing to FAKE.

 * This page can be edited by sending a pull request to the FAKE project on GitHub, so if you learn something when playing with FAKE please record your [findings here](https://github.com/fsharp/FAKE/blob/master/help/contributing.md)!

 * If you want to discuss a feature (a good idea!), or if you want to look at suggestions how you might contribute, check out the [Issue list](https://github.com/fsharp/FAKE/issues) on GitHub or send an email to the [FAKE mailing list](http://groups.google.com/group/fsharpMake).
   
 * Unless you explicitly state otherwise, any contribution intentionally 
submitted for inclusion in the Project shall be under the terms and 
conditions of the Apache 2.0 license. See License.txt for details.

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

* Windows users can install Visual Studio 2013 (the [Community Edition](https://www.visualstudio.com/products/visual-studio-community-vs)
is freely available for open-source projects).

* Linux and Mac users can read "[Guide - Cross-Platform Development with F#](http://fsharp.org/guides/mac-linux-cross-platform/)"
to find out the required tools.

* Alternately, you can use [Vagrant](https://www.vagrantup.com/) in-pair with [VirtualBox](https://www.virtualbox.org/)
to automatically deploy a preconfigured virtual machine. See the [Vagrant docs](vagrant.html) to get in touch with the tool.

### Programming

* Checkout the `master` branch.

* Run the build in order to check if everything works.
  * On Mono run `build.sh`
  * On Windows run `build.cmd`

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

#### Text editor / Code style

* Install the [EditorConfig](http://editorconfig.org/) extension in your text editor(s). List available [here](http://editorconfig.org/#download).

* Visual Studio users can also install the [CodeMaid](http://www.codemaid.net/) extension.

* Read the [F# component design guidelines](http://fsharp.org/specs/component-design-guidelines/).
