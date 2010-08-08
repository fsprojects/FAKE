# What is "FAKE - F# Make"?

## Mailing list

The "FAKE - F# Make" mailing list can be found at [http://groups.google.com/group/fsharpMake](http://groups.google.com/group/fsharpMake).

## Introduction

Modern build automation systems are not limited to simply recompile programs if source code has changed. 
They are supposed to get the latest sources from a source code management system, build test databases, 
run automatic tests, check guidelines, create documentation files, install setup projects and much more. 
Some companies are even deploying virtual machines, which are created during a nightly build process. 
In order to simplify the writing of such build scripts and to provide reusability of common tasks 
most build automation systems are using a domain-specific language (DSL). 
These tools can be divided into tools using external DSLs with a custom syntax like **make**, 
tools using external DSLs with an XML based syntax like **MSBuild** or **Apache Ant** and 
tools using internal DSLs which are integrated in a host language like **Rake**, which uses Ruby.

## FAKE - An integrated DSL

"FAKE - F# Make" is a build automation system, which is intended to combine the advantages 
of the above mentioned tools but to provide a better tooling support. 
Due to its integration in F#, all benefits of the .NET Framework and functional programming can be used, 
including the extensive class library, powerful debuggers and integrated development environments 
like Visual Studio 2008 or SharpDevelop, which provide syntax highlighting and code completion. 

The new language was designed to be succinct, typed, declarative, extensible and easy to use. 
For instance custom build tasks can be added simply by referencing .NET assemblies and using the corresponding classes. 

## Articles

* [Getting started with "FAKE - F# Make"](http://www.navision-blog.de/2009/04/01/getting-started-with-fake-a-f-sharp-make-tool): This tutorial shows you how to build the CalculatorSample project.
* [Adding FxCop to a "FAKE" build script](http://www.navision-blog.de/2009/04/02/adding-fxcop-to-a-fake-build-script)
* [Debugging "FAKE - F# Make" build scripts](http://www.navision-blog.de/2009/04/05/debugging-fake-f-make-build-scripts)
* [Modifying AssemblyInfo and Version via "FAKE - F# Make"](http://www.navision-blog.de/2009/04/04/modifying-assemblyinfo-and-version-via-fake-f-make)
* [Writing custom tasks for "FAKE - F# Make"](http://www.navision-blog.de/2009/04/14/writing-custom-tasks-for-fake-f-make)
* [Integrating a "FAKE - F# Make" build script into TeamCity](http://www.navision-blog.de/2009/04/15/integrate-a-fake-f-make-build-script-into-teamcity)
* [Integrating a "FAKE - F# Make" build script into CruiseControl.NET](http://www.navision-blog.de/2009/10/14/integrating-a-fake-f-make-build-script-into-cruisecontrol-net)

## Main Features

* Simple build infrastructure
* Easy systax
* Full power of .NET Framework
* Simple [TeamCity](http://www.jetbrains.com/teamcity) integration (see [Integrating a "FAKE - F# Make" build script into TeamCity](http://www.navision-blog.de/2009/04/15/integrate-a-fake-f-make-build-script-into-teamcity))
* Simple CruiseControl.NET integration (see [Integrating a "FAKE - F# Make" build script into CruiseControl.NET](http://www.navision-blog.de/2009/10/14/integrating-a-fake-f-make-build-script-into-cruisecontrol-net))
* FinalTarget feature (to release resources even if build fails)
* Extensible platform - [Write your own tasks](http://www.navision-blog.de/2009/04/14/writing-custom-tasks-for-fake-f-make)
* Easy debugging
* Intellisense support (when using Visual Studio)

## Predefined tasks

* Clean task
* [NUnit](http://www.nunit.org) support
* [xUnit.net](http://www.codeplex.com/xunit) support
* NCover support
* FxCop support
* ExecProcess task (To run tools via the command line)
* MSBuild task (to compile *.csproj and *.fsproj projects or run MSBuild scripts)
* XMLRead task
* VSS task (Get sources from Visual Source Safe)
* XCopy task
* Zip task
* [git](http://git-scm.com/) tasks
* AssemblyInfo task

# Using FAKE

## Targets

Targets are the main unit of work in a "FAKE - F# Make" script. 
Targets have a name (usually given as a symbol or a string) and a action (given as a code block).

	// The clean target cleans the build and deploy folders
	Target? Clean <- 
		fun _ -> 
			CleanDir "./build/"
			CleanDir "./deploy/"

### Dependencies

You can define prerequisites for tasks:

	// Target Default is dependent from target Clean and BuildApp
	// "FAKE - F# Make" will run these targets before Default
	"Default"  <== ["Clean"; "BuildApp"]

There is also an alternative syntax for dependencies:

	// Target Default is dependent from target Clean and BuildApp
	// "FAKE - F# Make" will run these targets before Default
	For? Default <-
		Dependency? Clean
		  |> And? BuildApp

### Running targets

You can execute targets with the "run"-command:

	// Executes Default target
	Run? Default

### Final targets

Final target can be used for TearDown functionality. 
These targets will be executed even if the build fails but have to be activated via ActivateFinalTarget().

	// FinalTarget will be excuted even if build fails
	FinalTarget? CloseSomePrograms <-
		fun _ ->
			// close stuff and release resources


	// Activate FinalTarget somewhere during build
	ActivateFinalTarget? CloseSomePrograms

## FileSets

"FAKE - F# Make" uses similar include and exclude patterns as NAnt and MSBuild. 

### File includes

	// Includes all *.csproj files under /src/app by using the !+ operator
	!+ "src/app/**/*.csproj"

	// Includes all *.csproj files under /src/app and /test with the ++ operator
	!+ "src/app/**/*.csproj"
	  ++ "test/**/*.csproj"

### File excludes

	// Includes all files under /src/app but excludes *.zip files
	!+ "src/app/**/*.*"
	  -- "*.zip"

### Scan vs. ScanImmediately

"FAKE - F# Make" provides two scan methods: Scan() and ScanImmediately().

Scan is a lazy method and evaluates the FileSet as late as possible ("on-demand").
If the FileSet is used twice, it will be reevaluated.

The following code defines a lazy FileSet:

	// Includes all *.csproj files under /src/app and scans them lazy
	let apps = 
	  !+ "src/app/**/*.csproj"
		|> Scan

ScanImmediately() scans the FileSet immediatly at time of its definition
and memoizes it. 

	// Includes all files under /src/app but excludes *.zip files
	//	  eager scan ==> All files memoized at the time of this definition
	let files = 
	  !+ "src/app/**/*.csproj"
		-- "*.zip"
		|> ScanImmediately

## UnitTests

### NUnit

	// define test dlls
	let testDlls = !+ (testDir + @"/Test.*.dll") |> Scan
	 
	Target? NUnitTest <-
		fun _ -> 
			testDlls
			  |> NUnit (fun p -> 
						  {p with 
							 ToolPath = nunitPath; 
							 DisableShadowCopy = true; 
							 OutputFile = testDir + "TestResults.xml"})   

### xUnit.net

	// define test dlls
	let testDlls = !+ (testDir + @"/Test.*.dll") |> Scan

	Target? xUnitTest <-
		fun _ -> 
			testDlls
			  |> xUnit (fun p ->
						  {p with
							  ShadowCopy = false;
							  HtmlPrefix = testDir})

## Sample script

This sample script
  
* Assumes "FAKE - F# Make" is located at ./tools/FAKE
* Assumes NUnit is located at ./tools/NUnit  
* Cleans the build and deploy paths
* Builds all C# projects below src/app/ and puts the output to .\build
* Builds all NUnit test projects below src/test/ and puts the output to .\build
* Uses NUnit to test the generated Test.*.dll's
* Zips all generated files to deploy\MyProject-0.1.zip
  
You can read [Getting started with FAKE](http://www.navision-blog.de/2009/04/01/getting-started-with-fake-a-f-sharp-make-tool) to build such a script.

	// Include FAKE libraries
	#I @"tools\FAKE"
	#r "FakeLib.dll"
	open Fake 

	// properties 
	let projectName = "MyProject"
	let version     = "0.1" 
	let buildDir    = "./build/"
	let deployDir   = "./deploy/"
	let nunitPath   = "./tools/NUnit/bin"
	let nunitOutput = buildDir + "TestResults.xml"
	let zipFileName = deployDir + sprintf "%s-%s.zip" projectName version

	// files
	let appReferences  = !+ "src/app/**/*.csproj"  |> Scan
	let testReferences = !+ "src/test/**/*.csproj" |> Scan
	let testDlls = !+ (buildDir + "*.Test.dll") |> Scan
	let filesToZip = 
		!+ (buildDir + "/**/*.*") 
		  -- "*.zip"
		  |> Scan

	// Targets

	Target? Clean <- 
		fun _ -> 
		  CleanDir buildDir
		  CleanDir deployDir

	Target? BuildApp <-
		fun _ ->          
			appReferences
			  |> MSBuildRelease buildDir "Build"
			  |> Log "AppBuild-Output: "

	Target? BuildTest <-
		fun _ -> 
			testReferences
			  |> MSBuildDebug buildDir "Build"
			  |> Log "TestBuild-Output: "

	Target? Test <-
		fun _ ->  
			testDlls
			  |> NUnit (fun p -> 
						  {p with 
							 ToolPath = nunitPath; 
							 DisableShadowCopy = true; 
							 OutputFile = nunitOutput})

	Target? BuildZip <-
		fun _ -> Zip buildDir zipFileName filesToZip

	Target? Default <- DoNothing

	// Dependencies
	For? BuildApp <- Dependency? Clean

	For? Test <-
		Dependency? BuildApp
		  |> And? BuildTest
		  
	For? BuildZip <- Dependency? Test
	For? Default <- Dependency? BuildZip

	// start build
	Run? Default