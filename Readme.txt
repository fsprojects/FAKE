What is "FAKE - F# Make"?

The Google group can be found at http://groups.google.com/group/fsharpMake.
More information on http://bitbucket.org/forki/fake/wiki/Home.

Modern build automation systems are not limited to simply recompile programs
if source code has changed. They are supposed to get the latest sources from 
a source code management system, build test databases, run automatic tests, 
check guidelines, create documentation files, install setup projects
and much more. Some companies are even deploying virtual machines, 
which are created during a nightly build process. In order to simplify the 
writing of such build scripts and to provide reusability of common tasks most 
build automation systems are using a domain-specic language (DSL). 
These tools can be divided into tools using external DSLs with a custom syntax
like make, tools using external DSLs with an XML based syntax like MSBuild
or Apache Ant and tools using internal DSLs which are integrated in a host
language like Rake, which uses Ruby.

"FAKE - F# Make" is a build automation system, which is intended to combine the
advantages of the above mentioned tools but to provide a better tooling support.
Due to its integration in F#, all benets of the .NET Framework and 
functional programming can be used, including the extensive class library, 
powerful debuggers and integrated development environments like 
Visual Studio 2008 or SharpDevelop, which provide syntax highlighting 
and code completion.

The new language was designed to be succinct, typed, declarative, 
extensible and easy to use. For instance custom build tasks can be added
simply by referencing .NET assemblies and using the corresponding classes.

Main Features

    * Simple build infrastructure
    * Easy systax
    * Full power of .NET Framework
    * Predefined tasks:
    * Clean task
    * NUnit support
    * xUnit.net support
    * NCover support
    * FxCop support
    * ExecProcess task (To run tools via the command line)
    * MSBuild task (to compile *.csproj, *.fsproj projects or MSBuild scripts)
    * XMLRead task
    * VSS task (Get sources from Visual Source Safe)
    * XCopy task
    * Zip task
    * AssemblyInfo task
    * Simple TeamCity integration
    * FinalTarget feature (to release resources even if build fails)
    * Extensible platform (Write your own tasks)
    * Easy debugging
    * Intellisense support (when using Visual Studio)

Building FAKE

   You can always download the latest "FAKE - F# Make" builds from http://teamcity.codebetter.com/viewType.html?buildTypeId=bt114&tab=buildTypeStatusDiv by using a guest login.

   If you want to build "FAKE - F# Make" on you have to follow these steps:

   1. git clone git://github.com/forki/FAKE.git
   2. cd FAKE
   3. git submodule init
   4. git submodule update
   5. build.bat 