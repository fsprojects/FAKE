# NuGet package restore

**Note:  This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation needs te be updated, please help! **

If you are using a source control system like [git](http://git-scm.com/) you probably don't want to store all binary dependencies in it. 
With FAKE you can use [NuGet](http://nuget.codeplex.com/) to download all dependent packages during the build.

## Setting the stage for NuGet

In order to download the packages during the build we need to add NuGet.exe to our repository. 
You can download the "NuGet.exe Command Line Tool" from the [release page](https://github.com/NuGet/Home/releases).

## Restore packages from the build script

Modify your build script and add **RestorePackages()** near the beginning of the script.
This will use the default parameters to retrieve all NuGet packages specified in *"./\*\*/packages.config"* files.

If you need to use different parameters please use the [RestorePackage](apidocs/fake-restorepackagehelper.html) task directly.

## Download latest version of FAKE via NuGet

If you don't want to store FAKE.exe and its components in your repository, you can use a batch file which downloads it before the build:

	[lang=batchfile]
	@echo off
	cls
	"tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion"
	"tools\FAKE\tools\Fake.exe" build.fsx
	pause
