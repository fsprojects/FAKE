# FAKE - F# Make - A DSL for build tasks

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation needs te be updated, please help!</p>
</div>

"FAKE - F# Make" is a build automation system with capabilities which are similar to **make** and **rake**.
It is using an easy domain-specific language (DSL) so that you can start using it without learning F#.
If you need more than the default functionality you can either write F# or simply reference .NET assemblies.

### Simple Example

    #r "tools/FAKE/tools/FakeLib.dll" // include Fake lib
	open Fake


	Target "Test" (fun _ ->
		trace "Testing stuff..."
	)

	Target "Deploy" (fun _ ->
		trace "Heavy deploy action"
	)

	"Test"            // define the dependencies
	   ==> "Deploy"

	Run "Deploy"

This build script has two targets. The "Deploy" target has exactly one dependency, namely the "Test" target. Invoking the "Deploy" target (line 16) will cause FAKE to invoke the "Test" target as well.

## Who is using FAKE?

Some of our users are:

* [msu solutions GmbH](http://www.msu-solutions.de/)
* [E.On Global Commodities UK](http://www.eon.com/en/about-us/structure/company-finder/e-dot-on-global-commodities.html)
* [Octokit.NET](https://github.com/octokit/octokit.net/) by GitHub
* [Tachyus](http://tachyus.com/)
* [Akka.net](https://github.com/akkadotnet/akka.net)
* [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service)
* [Visual F# Power Tools](https://github.com/fsprojects/VisualFSharpPowerTools)
* [Deedle](https://github.com/BlueMountainCapital/Deedle) by BlueMountainCapital
* [FSharp.Data](https://github.com/fsharp/FSharp.Data)
* [CHECK24 Vergleichsportal GmbH](http://www.check24.de)

You can find more users [here](users.html).

## How to get FAKE

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      FAKE is available <a href="https://nuget.org/packages/FAKE">on NuGet</a>.
      To install the tool, run the following command in the <a href="http://docs.nuget.org/docs/start-here/using-the-package-manager-console">Package Manager Console</a>:
      <pre>PM> Install-Package FAKE</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

* [Release Notes](RELEASE_NOTES.html)
* [![NuGet Status](https://img.shields.io/nuget/v/FAKE.svg?style=flat)](https://www.nuget.org/packages/FAKE/)

# Using FAKE

If you want to learn about FAKE you should read the ["Getting started with FAKE"](gettingstarted.html) tutorial.

## Targets

Targets are the main unit of work in a "FAKE - F# Make" script. Targets have a name and an action (given as a code block).

	// The clean target cleans the build and deploy folders
	Target "Clean" (fun _ ->
		CleanDirs ["./build/"; "./deploy/"]
	)

### Build target order

You can specify the build order using the ==> operator:

	// "FAKE - F# Make" will run these targets in the order Clean, BuildApp, Default
	"Clean"
	  ==> "BuildApp"
	  ==> "Default"

If one target should only be run on a specific condition you can use the =?> operator:

	"Clean"
	  ==> "BuildApp"
	  =?> ("Test",hasBuildParam "xUnitTest")  // only if FAKE was called with parameter xUnitTest
	  ==> "Default"

### Running targets

You can execute targets with the "RunTargetOrDefault"-function (for more details see [Running specific targets](specifictargets.html)):

	RunTargetOrDefault "Default"

## FileSets

"FAKE - F# Make" uses similar include and exclude patterns as NAnt and MSBuild.

### File includes

	// Includes all *.csproj files under /src/app by using the !! operator
	!! "src/app/**/*.csproj"

	// Includes all *.csproj files under /src/app and /test with the ++ operator
	!! "src/app/**/*.csproj"
	  ++ "test/**/*.csproj"

### File excludes

	// Includes all files under /src/app but excludes *.zip files
	!! "src/app/**/*.*"
	  -- "*.zip"

## UnitTests

### NUnit

	// define test dlls
	let testDlls = !! (testDir + "/Test.*.dll")

	Target "NUnitTest" (fun _ ->
		testDlls
			|> NUnit (fun p ->
				{p with
					DisableShadowCopy = true;
					OutputFile = testDir + "TestResults.xml"})
    )

### MSpec
	// define test dlls
	let testDlls = !! (testDir + "/Test.*.dll")

	Target "MSpecTest" (fun _ ->
		testDlls
			|> MSpec (fun p ->
				{p with
					ExcludeTags = ["LongRunning"]
					HtmlOutputDir = testOutputDir})
    )

### xUnit.net

	// define test dlls
	let testDlls = !! (testDir + "/Test.*.dll")

	Target "xUnitTest" (fun _ ->
        testDlls
            |> xUnit2 (fun p ->
                {p with
                    ShadowCopy = false;
                    HtmlOutput = true;
                    XmlOutput = true;
                    OutputDir = testDir })
    )

### Expecto

    // define test executables
	let testExecutables = !! (testDir + "/Test.*.exe")

    Target "expectoTest" (fun _ ->
        testExecutables
        |> Expecto (fun p ->
            { p with
                Debug = true
                Parallel = true
                // use only one of the following parameters
                Filter = "TestPrefix"
                FilterTestCase = "TestCaseNameSubstring"
                FilterTestList = "TestListNameSubstring"
                Run = ["Test1"; "Test2"]
                ListTests = false
            })
    )


## Sample script

This sample script

* Assumes "FAKE - F# Make" is located at ./tools/FAKE
* Assumes NUnit is located at ./tools/NUnit
* Cleans the build and deploy paths
* Builds all C# projects below src/app/ and puts the output to ./build
* Builds all NUnit test projects below src/test/ and puts the output to ./build
* Uses NUnit to test the generated Test.*.dll's
* Zips all generated files to deploy/MyProject-0.1.zip

You can read the [getting started guide](gettingstarted.html) to build such a script.

    // include Fake libs
    #r "tools/FAKE/FakeLib.dll"

    open Fake

    // Directories
    let buildDir  = "./build/"
    let testDir   = "./test/"
    let deployDir = "./deploy/"

    // tools
    let fxCopRoot = "./Tools/FxCop/FxCopCmd.exe"

    // Filesets
    let appReferences  =
        !! "src/app/**/*.csproj"
          ++ "src/app/**/*.fsproj"

    let testReferences = !! "src/test/**/*.csproj"

    // version info
    let version = "0.2"  // or retrieve from CI server

    // Targets
    Target "Clean" (fun _ ->
        CleanDirs [buildDir; testDir; deployDir]
    )

    Target "BuildApp" (fun _ ->
		CreateCSharpAssemblyInfo "./src/app/Calculator/Properties/AssemblyInfo.cs"
			[Attribute.Title "Calculator Command line tool"
			 Attribute.Description "Sample project for FAKE - F# MAKE"
			 Attribute.Guid "A539B42C-CB9F-4a23-8E57-AF4E7CEE5BAA"
			 Attribute.Product "Calculator"
			 Attribute.Version version
			 Attribute.FileVersion version]

		CreateCSharpAssemblyInfo "./src/app/CalculatorLib/Properties/AssemblyInfo.cs"
			[Attribute.Title "Calculator library"
			 Attribute.Description "Sample project for FAKE - F# MAKE"
			 Attribute.Guid "EE5621DB-B86B-44eb-987F-9C94BCC98441"
			 Attribute.Product "Calculator"
			 Attribute.Version version
			 Attribute.FileVersion version]

        // compile all projects below src/app/
        MSBuildRelease buildDir "Build" appReferences
            |> Log "AppBuild-Output: "
    )

    Target "BuildTest" (fun _ ->
        MSBuildDebug testDir "Build" testReferences
            |> Log "TestBuild-Output: "
    )

    Target "NUnitTest" (fun _ ->
        !! (testDir + "/NUnit.Test.*.dll")
            |> NUnit (fun p ->
                {p with
                    DisableShadowCopy = true;
                    OutputFile = testDir + "TestResults.xml"})
    )

    Target "xUnitTest" (fun _ ->
        !! (testDir + "/xUnit.Test.*.dll")
            |> xUnit2 (fun p ->
                {p with
                    ShadowCopy = false;
                    HtmlOutput = true;
                    XmlOutput = true;
                    OutputDir = testDir })
    )

    Target "FxCop" (fun _ ->
        !! (buildDir + "/**/*.dll")
            ++ (buildDir + "/**/*.exe")
            |> FxCop (fun p ->
                {p with
                    ReportFileName = testDir + "FXCopResults.xml";
                    ToolPath = fxCopRoot})
    )

    Target "Deploy" (fun _ ->
        !! (buildDir + "/**/*.*")
            -- "*.zip"
            |> Zip buildDir (deployDir + "Calculator." + version + ".zip")
    )

    // Build order
	"Clean"
      ==> "BuildApp"
      ==> "BuildTest"
      ==> "FxCop"
      ==> "NUnitTest"
      =?> ("xUnitTest",hasBuildParam "xUnitTest")  // only if FAKE was called with parameter xUnitTest
      ==> "Deploy"

    // start build
    RunTargetOrDefault "Deploy"
