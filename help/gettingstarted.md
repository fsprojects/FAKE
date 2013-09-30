# Getting started with FAKE - F# Make

In this tutorial you will learn how to set up a complete build infrastructure with "FAKE - F# Make". This includes:

* how to install the latest FAKE version
* how to automatically compile your C# or F# projects
* how to automatically resolve nuget dependencies
* how to automatically run NUnit tests on your projects
* how to zip the output to a deployment folder

## Install F#

"FAKE - F# Make" is completely written in F# and all build scripts will also be written in F#, but this doesn't imply that you have to learn programming in F#. In fact the "FAKE - F# Make" syntax is hopefully very easy to learn.

## Download Calculator Sample

Now download the latest [FAKE-Calculator.zip](https://github.com/fsharp/FAKE/archive/Calculator.zip) from the [FAKE project site](https://github.com/fsharp/FAKE). This sample includes 3 tiny projects and has basically the following structure:

* src\app
	* Calculator (command line)
	* CalculatorLib (class library)
* src\test
	* Test.CalculatorLib
* tools
	* NUnit
	* FxCop
* build.bat
* build.fsx
* completeBuild.bat
* completeBuild.fsx
* Calculator.sln

## Getting "FAKE - F# Make" started

In the root of the project you will find a build.bat file:

    [lang=batchfile]
	@echo off
	cls
	"tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"
	"tools\FAKE\tools\Fake.exe" build.fsx
	pause

Read the [full acticle](http://www.navision-blog.de/2009/04/01/getting-started-with-fake-a-f-sharp-make-tool/).