# FAKE - F# Make

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

"FAKE - F# Make" is a build automation system. Due to its integration 
in F#, all benefits of the .NET Framework and functional programming can be used, including 
the extensive class library, powerful debuggers and integrated development environments like 
Visual Studio or MonoDevelop, which provide syntax highlighting and code completion. 

The new DSL was designed to be succinct, typed, declarative, extensible and easy to use. 
For instance custom build tasks can be added simply by referencing .NET assemblies and using the corresponding classes.

## Who is using FAKE?

Some of our users are:

* [msu solutions GmbH](http://www.msu-solutions.de/)
* [fsharpx](https://github.com/fsharp/fsharpx)
* [FSharp.Data](https://github.com/fsharp/FSharp.Data)
* [FSharp.Charting](https://github.com/fsharp/FSharp.Charting)
* [FSharp.DataFrame by BlueMountainCapital](https://github.com/BlueMountainCapital/FSharp.DataFrame)
* [Portable.Licensing](https://github.com/dnauck/Portable.Licensing)

## How to get FAKE

You can download the latest builds from [http://teamcity.codebetter.com](http://teamcity.codebetter.com). You don't need to register, a guest login is ok.

* [Latest stable build](http://teamcity.codebetter.com/viewLog.html?buildId=lastSuccessful&buildTypeId=bt335&tab=artifacts&guest=1)
* [Latest development build](http://teamcity.codebetter.com/viewLog.html?buildId=lastSuccessful&buildTypeId=bt166&tab=artifacts&guest=1)
* [Changelog](changelog.html)

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      FAKE is also available <a href="https://nuget.org/packages/FAKE">on NuGet</a>.
      To install the tool, run the following command in the <a href="http://docs.nuget.org/docs/start-here/using-the-package-manager-console">Package Manager Console</a>:
      <pre>PM> Install-Package Fake</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

## Mailing list

The "FAKE - F# Make" mailing list can be found at [http://groups.google.com/group/fsharpMake](http://groups.google.com/group/fsharpMake).

# Using FAKE

## Targets

Targets are the main unit of work in a "FAKE - F# Make" script. 
Targets have a name and an action (given as a code block).

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
	  =?> ("Test",hasBuildParam "test")  // runs the Test target only if FAKE was called with parameter test
	  ==> "Default"

It's also possible to specify the dependencies for targets:

    // Target Default is dependent from target Clean and BuildApp
    // "FAKE - F# Make" will ensure to run these targets before Default
    "Default"  <== ["Clean"; "BuildApp"]

### Running targets

You can execute targets with the "run"-command:

	// Executes Default target
	Run "Default"

### Final targets

Final target can be used for TearDown functionality. 
These targets will be executed even if the build fails but have to be activated via ActivateFinalTarget().

	// FinalTarget will be excuted even if build fails
	FinalTarget "CloseSomePrograms" (fun _ ->
		// close stuff and release resources
	)

	// Activate FinalTarget somewhere during build
	ActivateFinalTarget "CloseSomePrograms"

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

The same FileSet by using the !! operator:

    // Includes all *.csproj files under /src/app and scans them lazy
    let apps = !! "src/app/**/*.csproj"

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
	let testDlls = !! (testDir + @"/Test.*.dll")
	 
	Target "NUnitTest" (fun _ ->
		testDlls
			|> NUnit (fun p -> 
				{p with 
					ToolPath = nunitPath; 
					DisableShadowCopy = true; 
					OutputFile = testDir + "TestResults.xml"})
    )
							 
### MSpec
	// define test dlls
	let testDlls = !! (testDir + @"/Test.*.dll")
	 
	Target "MSpecTest" (fun _ ->
		testDlls
			|> MSpec (fun p -> 
				{p with 
					ExcludeTags = ["LongRunning"]
					HtmlOutputDir = testOutputDir						  
					ToolPath = ".\toools\MSpec\mspec.exe"})
    )

### xUnit.net

	// define test dlls
	let testDlls = !! (testDir + @"/Test.*.dll")

	Target "xUnitTest" (fun _ ->
        testDlls
            |> xUnit (fun p -> 
                {p with 
                    ShadowCopy = false;
                    HtmlOutput = true;
                    XmlOutput = true;
                    OutputDir = testDir })
    )

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

    // include Fake libs
    #I "tools\FAKE"
    #r "FakeLib.dll"
    
    open Fake
    
    // Directories
    let buildDir  = @".\build\"
    let testDir   = @".\test\"
    let deployDir = @".\deploy\"
    
    // tools
    let nunitPath = @".\Tools\NUnit"
    let fxCopRoot = @".\Tools\FxCop\FxCopCmd.exe"
    
    // Filesets
    let appReferences  = 
        !+ @"src\app\**\*.csproj" 
          ++ @"src\app\**\*.fsproj" 
            |> Scan
    
    let testReferences = !! @"src\test\**\*.csproj"
        
    // version info
    let version = "0.2"  // or retrieve from CI server
    
    // Targets
    Target "Clean" (fun _ -> 
        CleanDirs [buildDir; testDir; deployDir]
    )
    
    Target "BuildApp" (fun _ ->
        AssemblyInfo 
            (fun p -> 
            {p with
                CodeLanguage = CSharp;
                AssemblyVersion = version;
                AssemblyTitle = "Calculator Command line tool";
                AssemblyDescription = "Sample project for FAKE - F# MAKE";
                Guid = "A539B42C-CB9F-4a23-8E57-AF4E7CEE5BAA";
                OutputFileName = @".\src\app\Calculator\Properties\AssemblyInfo.cs"})
                  
        AssemblyInfo 
            (fun p -> 
            {p with
                CodeLanguage = CSharp;
                AssemblyVersion = version;
                AssemblyTitle = "Calculator library";
                AssemblyDescription = "Sample project for FAKE - F# MAKE";
                Guid = "EE5621DB-B86B-44eb-987F-9C94BCC98441";
                OutputFileName = @".\src\app\CalculatorLib\Properties\AssemblyInfo.cs"})          
          
        // compile all projects below src\app\
        MSBuildRelease buildDir "Build" appReferences
            |> Log "AppBuild-Output: "
    )
    
    Target "BuildTest" (fun _ ->
        MSBuildDebug testDir "Build" testReferences
            |> Log "TestBuild-Output: "
    )
    
    Target "NUnitTest" (fun _ ->  
        !! (testDir + @"\NUnit.Test.*.dll")
            |> NUnit (fun p -> 
                {p with 
                    ToolPath = nunitPath; 
                    DisableShadowCopy = true; 
                    OutputFile = testDir + @"TestResults.xml"})
    )
    
    Target "xUnitTest" (fun _ ->  
        !! (testDir + @"\xUnit.Test.*.dll")
            |> xUnit (fun p -> 
                {p with 
                    ShadowCopy = false;
                    HtmlOutput = true;
                    XmlOutput = true;
                    OutputDir = testDir })
    )
    
    Target "FxCop" (fun _ ->
        !+ (buildDir + @"\**\*.dll") 
            ++ (buildDir + @"\**\*.exe") 
            |> Scan  
            |> FxCop (fun p -> 
                {p with                     
                    ReportFileName = testDir + "FXCopResults.xml";
                    ToolPath = fxCopRoot})
    )
    
    Target "Deploy" (fun _ ->
        !+ (buildDir + "\**\*.*") 
            -- "*.zip" 
            |> Scan
            |> Zip buildDir (deployDir + "Calculator." + version + ".zip")
    )
    
    // Build order
	"Clean"
      ==> "BuildApp" <=> "BuildTest"
      ==> "FxCop"
      ==> "NUnitTest"
      =?> ("xUnitTest",hasBuildParam "xUnitTest")  // runs the target only if FAKE was called with parameter xUnitTest
      ==> "Deploy"
     
    // start build
    Run "Deploy"
