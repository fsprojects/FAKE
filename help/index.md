# FAKE - F# Make

Modern build automation systems are not limited to simply recompile programs if 
source code has changed. They are supposed to get the latest sources from a source 
code management system, build test databases, run automatic tests, check guidelines, 
create documentation files, install setup projects and much more. Some companies are 
even deploying virtual machines, which are created during a nightly build process. In 
order to simplify the writing of such build scripts and to provide reusability of common 
tasks most build automation systems are using a domain-specific language (DSL). These tools 
can be divided into tools using external DSLs with a custom syntax like make, tools using 
external DSLs with an XML based syntax like MSBuild or Apache Ant and tools using internal 
DSLs which are integrated in a host language like Rake, which uses Ruby.

"FAKE - F# Make" is a build automation system. Due to its integration 
in F#, all benefits of the .NET Framework and functional programming can be used, including 
the extensive class library, powerful debuggers and integrated development environments like 
Visual Studio or MonoDevelop, which provide syntax highlighting and code completion.

The new DSL was designed to be succinct, typed, declarative, extensible and easy to use. 
For instance custom build tasks can be added simply by referencing .NET assemblies and using 
the corresponding classes.

* See the [overview](overview.html) for more details.
* See the [getting started guide](gettingstarted.html) for a full sample.
* See the [api docs](api/index.htm) if you are interested in special functions.

## How to contribute code

* Login in github (you need an account)
* Fork the main repository from [Github](https://github.com/fsharp/FAKE)
* Push your changes to your fork
* Send a pull request