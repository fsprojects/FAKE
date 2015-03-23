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

 * If you want to build the documentation, simply run the build script [GitHub link](https://github.com/fsharp/FAKE/blob/master/build.fsx)) which builds the documentation.
 
## Creating pull requests

* fork the [FAKE repo on GitHub](https://github.com/fsharp/FAKE)
* add a new git remote in order to retrieve upstream changes
    git remote add upstream https://github.com/fsharp/FAKE.git   
* checkout the `master` branch
* run the build in order to check if everything works
  * on Mono run *build.sh*
  * on Windows run *build.cmd*
* create a new feature branch
    git checkout -b myfeature
* implement your bugfix/feature
* add a bit of documentation (see above)
* commit and push to your fork
* use GitHub's UI to create a pull request
    write "WIP" into the pull request description if it's not completely ready
* if you need to rebase you can do:
    git fetch upstream
    git rebase upstream/master
    git push origin myfeature -f
* the pull request will be updated automatically

## Code style

* Read the [F# component design guidelines](http://fsharp.org/specs/component-design-guidelines/)
* Install the [EditorConfig](http://editorconfig.org/) extension in your text editor(s). List available [here](http://editorconfig.org/#download).
* Visual Studio users can also install the [CodeMaid](http://www.codemaid.net/) extension.
