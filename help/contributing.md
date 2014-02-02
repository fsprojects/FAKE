Contributing to FAKE
=======================

This page should provide you with some basic information if you're thinking about
contributing to FAKE.

 * This page can be edited by sending a pull request to the FAKE project on GitHub, so
   if you learn something when playing with FAKE please record your
   [findings here](https://github.com/fsharp/FAKE/blob/develop/help/contributing.md)!

 * If you want to discuss a feature (a good idea!), or if you want to look at 
   suggestions how you might contribute, check out the
   [Issue list](https://github.com/fsharp/FAKE/issues) on GitHub or send
   an email to the [FAKE mailing list](http://groups.google.com/group/fsharpMake).

## Features vs. BugFixes

If you find a bug and want to contribute a fix then please send a pull request to the [master](https://github.com/fsharp/FAKE/tree/master) branch.
Please use the [develop](https://github.com/fsharp/FAKE/tree/develop) branch for all other pull requests (features, documentation, ...).

## Documentation

The documentation for FAKE is automatically generated using the 
[F# Formatting](https://github.com/tpetricek/FSharp.Formatting) library. It turns 
`*.md` (Markdown with embedded code snippets) and `*.fsx` files (F# script file with 
embedded Markdown documentation) to a nice HTML documentation.

 * The code for all the documents can be found in the `help` directory
   [on GitHub](https://github.com/fsharp/FAKE/tree/develop/help). If you 
   find a bug or add a new feature, make sure you document it!

 * If you want to build the documentation, simply run the build script
   ([GitHub link](https://github.com/fsharp/FAKE/blob/develop/build.fsx)) which
   builds the documentation.