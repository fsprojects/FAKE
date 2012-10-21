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

"FAKE - F# Make" is a build automation system, which is intended to combine the advantages 
of the above mentioned tools but to provide a better tooling support. Due to its integration 
in F#, all benefits of the .NET Framework and functional programming can be used, including 
the extensive class library, powerful debuggers and integrated development environments like 
Visual Studio 2008 or SharpDevelop, which provide syntax highlighting and code completion.

The new language was designed to be succinct, typed, declarative, extensible and easy to use. 
For instance custom build tasks can be added simply by referencing .NET assemblies and using 
the corresponding classes.

See the [project home page](http://fsharp.github.com/FAKE/) for full details and documentation.

