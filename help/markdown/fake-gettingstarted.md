# Getting started with FAKE - F# Make

**Note:  This documentation is for FAKE.exe version 5.0 or later. The old documentation can be found [here](legacy-gettingstarted.html)**

In this tutorial you will learn how to set up a complete build infrastructure with "FAKE - F# Make". This includes:

* how to install the latest FAKE version
* how to edit and run scripts
* how to automatically compile your C# or F# projects
* how to automatically resolve nuget dependencies
* how to automatically run NUnit tests on your projects
* how to zip the output to a deployment folder

## Install FAKE

"FAKE - F# Make" is completely written in F# and all build scripts will also be written in F#, but this doesn't imply that you have to learn programming in F#. In fact the "FAKE - F# Make" syntax is hopefully very easy to learn.

There are various ways to install FAKE 5

- Install the 'fake' or 'fake-netcore' package for you system (currenty chocolatey)
  Example `choco install fake -pre`

- Use it as dotnet tool: Add `<DotNetCliToolReference Include="dotnet-fake" Version="5.0.0*" />` to your dependencies and run `dotnet fake ...` instead of `fake ...`, see [this example](https://github.com/matthid/fake-bootstrap/blob/master/dotnet-fake.csproj)

- Bootstrap via shell script (fake.cmd/fake.sh),
  see this [example project](https://github.com/matthid/fake-bootstrap)
  
  > DISCLAIMER: These scripts have no versioning story. You either need to take care of versions yourself (and lock them) or your builds might break on major releases.


## Create and Edit scripts with Intellisense

Once `fake` is available you can start creating your script:

- Create a new file `myscript.fsx` with the following contents:

```fsharp
#r "paket:
nuget Fake.Core.Target prerelease"
#load "./.fake/myscript.fsx/intellisense.fsx"
```

> Note: `storage: none` is currently required because of a bug, but it will be added by default.

Where you can add all the [fake modules](fake-fake5-modules.html) you need.

- run the script to restore your dependencies and setup the intellisense support: `fake run myscript.fsx`.
  This might take some seconds depending on your internet connection

  > The warning `FS0988: Main module of program is empty: nothing will happen when it is run` indicates that you have not written any code into the script yet.

- now open the script in VS Code with ionide-fsharp extension or Visual Studio.

> Note: If you change your dependencies you need to delete `myscript.fsx.lock` and run the script again for intellisense to update.

> Note: Intellisense is shown for the full framework while the script is run as `netcoreapp20` therefore intellisense might show APIs which are not actually usable.

## Example - Compiling and building your .NET application

### Getting started

Now open the *build.fsx* in Visual Studio or any text editor. It should look like this:

	// include Fake lib
	#r @"packages/FAKE/tools/FakeLib.dll"
	open Fake

	// Default target
	Target "Default" (fun _ ->
		trace "Hello World from FAKE"
	)

	// start build
	RunTargetOrDefault "Default"


As you can see the code is really simple. The first line includes the FAKE library and is vital for all FAKE build scripts.

After this header the *Default* target is defined. A target definition contains two important parts. The first is the name of the target (here "Default") and the second is an action (here a simple trace of "Hello world").

The last line runs the "Default" target - which means it executes the defined action of the target.

### Cleaning the last build output

A typical first step in most build scenarios is to clean the output of the last build. We can achieve this by modifying the *build.fsx* to the following:

	// include Fake lib
	#r "packages/FAKE/tools/FakeLib.dll"
	open Fake

	// Properties
	let buildDir = "./build/"

	// Targets
	Target "Clean" (fun _ ->
		CleanDir buildDir
	)

	Target "Default" (fun _ ->
		trace "Hello World from FAKE"
	)

	// Dependencies
	"Clean"
	  ==> "Default"

	// start build
	RunTargetOrDefault "Default"

We introduced some new concepts in this snippet. At first we defined a global property called "buildDir" with the relative path of a temporary build folder.

In the *Clean* target we use the CleanDir task to clean up this build directory. This simply deletes all files in the folder or creates the directory if necessary.

In the dependencies section we say that the *Default* target has a dependency on the *Clean* target. In other words *Clean* is a prerequisite of *Default* and will be run before the execution of *Default*:

![alt text](pics/gettingstarted/afterclean.png "We introduced a Clean target")

### Compiling the application

In the next step we want to compile our C# libraries, which means we want to compile all csproj-files under */src/app* with MSBuild:

	// include Fake lib
	#r "packages/FAKE/tools/FakeLib.dll"
	open Fake

	// Properties
	let buildDir = "./build/"

	// Targets
	Target "Clean" (fun _ ->
		CleanDir buildDir
	)

	Target "BuildApp" (fun _ ->
		!! "src/app/**/*.csproj"
		  |> MSBuildRelease buildDir "Build"
		  |> Log "AppBuild-Output: "
	)

	Target "Default" (fun _ ->
		trace "Hello World from FAKE"
	)

	// Dependencies
	"Clean"
	  ==> "BuildApp"
	  ==> "Default"

	// start build
	RunTargetOrDefault "Default"

We defined a new build target named "BuildApp" which compiles all csproj-files with the MSBuild task and the build output will be copied to buildDir.

In order to find the right project files FAKE scans the folder *src/app/* and all subfolders with the given pattern. Therefore a similar FileSet definition like in NAnt or MSBuild (see [project page](https://github.com/fsharp/FAKE) for details) is used.

In addition the target dependencies are extended again. Now *Default* is dependent on *BuildApp* and *BuildApp* needs *Clean* as a prerequisite.

This means the execution order is: Clean ==> BuildApp ==> Default.

![alt text](pics/gettingstarted/aftercompile.png "We introduced a Build target")

### Compiling test projects

Now our main application will be built automatically and it's time to build the test project. We use the same concepts as before:

	// include Fake lib
	#r "packages/FAKE/tools/FakeLib.dll"
	open Fake

	// Properties
	let buildDir = "./build/"
	let testDir  = "./test/"

	// Targets
	Target "Clean" (fun _ ->
		CleanDirs [buildDir; testDir]
	)

	Target "BuildApp" (fun _ ->
	   !! "src/app/**/*.csproj"
		 |> MSBuildRelease buildDir "Build"
		 |> Log "AppBuild-Output: "
	)

	Target "BuildTest" (fun _ ->
		!! "src/test/**/*.csproj"
		  |> MSBuildDebug testDir "Build"
		  |> Log "TestBuild-Output: "
	)

	Target "Default" (fun _ ->
		trace "Hello World from FAKE"
	)

	// Dependencies
	"Clean"
	  ==> "BuildApp"
	  ==> "BuildTest"
	  ==> "Default"

	// start build
	RunTargetOrDefault "Default"

This time we defined a new target "BuildTest" which compiles all C# projects below *src/test/* in Debug mode and we put the target into our build order.

If we run build.bat again we get an error like this:

![alt text](pics/gettingstarted/compileerror.png "Compile error")

The problem is that we didn't download the NUnit package from nuget. So let's fix this in the build script:

	// include Fake lib
	#r "packages/FAKE/tools/FakeLib.dll"
	open Fake

	RestorePackages()
	// ...

With this simple command FAKE will use nuget.exe to install all the package dependencies.

You may experience this tutorial not quite working with the newest package versions. In this case you can edit the [`paket.dependencies` file](http://fsprojects.github.io/Paket/dependencies-file.html) to something like this:

    source https://nuget.org/api/v2

    nuget FAKE

    http https://dist.nuget.org/win-x86-commandline/latest/nuget.exe NuGet/nuget.exe

    nuget NUnit ~> 2.5.10

Again run Paket from the command line:

    $ .paket/paket.exe install

This will fetch nuget.exe from nuget.org and also download an early version of NUnit that contains the NUnit runner. The edit to [`paket.dependencies`](http://fsprojects.github.io/Paket/dependencies-file.html) does not replace the RestorePackages() step. The NUnit.Test.CalculatorLib test project references the NUnit version 2.6.2 library, so we need that version too.

### Running the tests with NUnit

Now all our projects will be compiled and we can use FAKE's NUnit task in order to let NUnit test our assembly:

	// include Fake lib
	#r "packages/FAKE/tools/FakeLib.dll"
	open Fake

	RestorePackages()

	// Properties
	let buildDir = "./build/"
	let testDir  = "./test/"

	// Targets
	Target "Clean" (fun _ ->
		CleanDirs [buildDir; testDir]
	)

	Target "BuildApp" (fun _ ->
	   !! "src/app/**/*.csproj"
		 |> MSBuildRelease buildDir "Build"
		 |> Log "AppBuild-Output: "
	)

	Target "BuildTest" (fun _ ->
		!! "src/test/**/*.csproj"
		  |> MSBuildDebug testDir "Build"
		  |> Log "TestBuild-Output: "
	)

	Target "Test" (fun _ ->
		!! (testDir + "/NUnit.Test.*.dll")
		  |> NUnit (fun p ->
			  {p with
				 DisableShadowCopy = true;
				 OutputFile = testDir + "TestResults.xml" })
	)

	Target "Default" (fun _ ->
		trace "Hello World from FAKE"
	)

	// Dependencies
	"Clean"
	  ==> "BuildApp"
	  ==> "BuildTest"
	  ==> "Test"
	  ==> "Default"

	// start build
	RunTargetOrDefault "Default"

Our new *Test* target scans the test directory for test assemblies and runs them with the NUnit runner. FAKE automatically tries to locate the runner in one of your subfolders. See the [NUnit task documentation](apidocs/fake-nunitsequential.html) if you need to specify the tool path explicitly.

The mysterious part **(fun p -> ...)** simply overrides the default parameters of the NUnit task and allows to specify concrete parameters.

![alt text](pics/gettingstarted/alltestsgreen.png "All tests green")

Alternatively you could also run the tests in parallel using the [NUnitParallel](apidocs/fake-nunitparallel.html) task:

	Target "Test" (fun _ ->
		!! (testDir + "/NUnit.Test.*.dll")
		  |> NUnitParallel (fun p ->
			  {p with
				 DisableShadowCopy = true;
				 OutputFile = testDir + "TestResults.xml" })
	)



## What's next?

- look at the [quick start guide](fake-dotnetcore.html) which has the same information in a more dense form.
- Take a look at the various fake modules which basically have most of your use-cases covered.
- Add fake build scripts to your projects and let us know.
- Automate stuff with FAKE and use standalone scripts.
- Write your own modules and let us know.
- Contribute :)
