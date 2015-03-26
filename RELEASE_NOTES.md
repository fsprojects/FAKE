#### 3.26.1 - 26.03.2015
* REVERTING: SystemRoot also works on mono - https://github.com/fsharp/FAKE/pull/706 (see https://github.com/fsharp/FAKE/issues/715)

#### 3.26.0 - 25.03.2015
* Detect GitLab CI as build server - https://github.com/fsharp/FAKE/pull/712

#### 3.25.2 - 24.03.2015
* Look into PATH when scanning for NuGet.exe - https://github.com/fsharp/FAKE/pull/708
* SystemRoot also works on mono - https://github.com/fsharp/FAKE/pull/706
* Use EditorConfig - http://editorconfig.org/

#### 3.25.1 - 24.03.2015
* More AppVeyor properties added - https://github.com/fsharp/FAKE/pull/704

#### 3.25.0 - 23.03.2015
* Look into PATH when scanning for tools - https://github.com/fsharp/FAKE/pull/703

#### 3.24.0 - 22.03.2015
* BREAKING CHANGE: Better support for AssemblyMetadata in AssemblyInfoHelper - https://github.com/fsharp/FAKE/pull/694
* Added modules for building VB6 projects with SxS manifest - https://github.com/fsharp/FAKE/pull/697
* Use parameter quoting for Paket helpers

#### 3.23.0 - 12.03.2015
* BREAKING CHANGE: Adjusted Xamarin.iOS archive helper params - https://github.com/fsharp/FAKE/pull/693
* New operator </> allows to combine paths similar to @@ but with no trimming operations - https://github.com/fsharp/FAKE/pull/695

#### 3.22.0 - 12.03.2015
* Globbing allows to grab folders without a trailing slash
* Removed long time obsolete globbing functions

#### 3.21.0 - 11.03.2015
* FAKE allows to run parallel builds - http://fsharp.github.io/FAKE/parallel-build.html

#### 3.20.1 - 10.03.2015
* Proper source index - https://github.com/fsharp/FAKE/issues/678

#### 3.20.0 - 10.03.2015
* Always use FCS in FAKE and FSI in FAke.Deploy
* Modify VM size on a .csdef for Azure Cloud Services - https://github.com/fsharp/FAKE/pull/687
* Added ZipHelper.ZipOfIncludes - https://github.com/fsharp/FAKE/pull/686
* Added AppVeyorEnvironment.RepoTag & .RepoTagName - https://github.com/fsharp/FAKE/pull/685
* New tutorial about Azure Cloud Service - http://fsharp.github.io/FAKE/azurecloudservices.html
* Added basic support for creating Azure Cloud Services - http://fsharp.github.io/FAKE/apidocs/fake-azure-cloudservices.html
* Added metadata property for AssemblyInfoReplacementParams - https://github.com/fsharp/FAKE/pull/675

#### 3.18.0 - 04.03.2015
* Remvoved internal class generated in AssemblyInfo.Vb - https://github.com/fsharp/FAKE/pull/673
* Adding ability to control type library export (/tlb flag) of RegAsm - https://github.com/fsharp/FAKE/pull/668
* Adding ability to run nuget package restore on a visual studio solution - https://github.com/fsharp/FAKE/pull/662
* Add OwnerId, type docs, and better error handling for HockeyAppHelper - https://github.com/fsharp/FAKE/pull/661
* Don't report unit test failure twice to TeamCity - https://github.com/fsharp/FAKE/pull/659
* New tasks for `paket pack` and `paket push`- http://fsprojects.github.io/Paket/index.html
* Allow csproj being passed as a NuSpec file - https://github.com/fsharp/FAKE/pull/644 
* Helper for uploading mobile apps to HockeyApp - https://github.com/fsharp/FAKE/pull/656  
* SCPHelper does allow copying single files - https://github.com/fsharp/FAKE/issues/671
* BUGFIX: Paket helper should not submit the endpoint if no endpoint was given - https://github.com/fsharp/FAKE/issues/667
* BUGFIX: Paket helper should not override version for project packages - https://github.com/fsharp/FAKE/issues/666
* BUGFIX: Allow endpoint in push task - https://github.com/fsprojects/Paket/pull/652
* BUGFIX: Use correct apikey for paket push - https://github.com/fsharp/FAKE/pull/664

#### 3.17.0 - 12.02.2015
* Revert to fsi in Fake.Deploy - https://github.com/fsharp/FAKE/pull/653    
* Added MergeByHash option for OpenCover - https://github.com/fsharp/FAKE/pull/650
* New functions to replace text in one or more files using regular expressions - https://github.com/fsharp/FAKE/pull/649
* BUGFIX: Fix SpecFlow MSTest integration - https://github.com/fsharp/FAKE/pull/652
* BUGFIX: Fix TeamCity integration - https://github.com/fsharp/FAKE/pull/651

#### 3.15.0 - 07.02.2015
* New VSTest module for working with VSTest.Console - https://github.com/fsharp/FAKE/pull/648
* Add Verbose to argument list for NuGet update - https://github.com/fsharp/FAKE/pull/645
* BUGFIX: Fix jarsigner executing on Windows environment - https://github.com/fsharp/FAKE/pull/640
* Adding UploadTestResultsXml function to the AppVeyor module - https://github.com/fsharp/FAKE/pull/636
* Adding the NoDefaultExcludes NugGet parameter - https://github.com/fsharp/FAKE/pull/637
* Adding `SpecificMachines` option to OctoTools - https://github.com/fsharp/FAKE/pull/631
* Allow to run gacutil on mono
* Ignore unknown project references in MSBuild task - https://github.com/fsharp/FAKE/pull/630

#### 3.14.0 - 14.01.2015
* BUGFIX: Added a reset step before starting a deployment - https://github.com/fsharp/FAKE/pull/621
* Report fatal git errors to command line

#### 3.13.0 - 03.01.2015
* New FAKE.Lib nuget package which contains the FakeLib - https://github.com/fsharp/FAKE/pull/607
* New AppVeyor properties - https://github.com/fsharp/FAKE/pull/605
* Use FSharp.Core from NuGet - https://github.com/fsharp/FAKE/pull/602
* Build and deploy Azure web jobs - https://github.com/fsharp/FAKE/pull/613

#### 3.11.0 - 03.12.2014
* Dual-license under Apache 2 and MS-PL, with Apache as default - https://github.com/fsharp/FAKE/pull/598
* BUGFIX: FSC compilation fix - https://github.com/fsharp/FAKE/pull/601
* BUGFIX: Unescape special MSBuild characters - https://github.com/fsharp/FAKE/pull/600

#### 3.10.0 - 27.11.2014
* Support for MSBuild 14.0 - https://github.com/fsharp/FAKE/pull/595
* New C# compiler helper - https://github.com/fsharp/FAKE/pull/592/files
* Added support for NUnit Fixture parameter - https://github.com/fsharp/FAKE/pull/591
* OpenSourcing some DynamicsNAV helpers from gitnav
* BUGFIX: Fix 64bit mode
* BUGFIX: Dynamics NAV helper - "Ignored" tests should report the message

#### 3.9.0 - 07.11.2014
* Create a new package with a x64 version - https://github.com/fsharp/FAKE/pull/582
* Added a Xamarin.iOS Archiving helper - https://github.com/fsharp/FAKE/pull/581
* DynamicsNAV helper should use the correct ServiveTier

#### 3.8.0 - 30.10.2014
* [xUnit 2](http://xunit.github.io/) support - https://github.com/fsharp/FAKE/pull/575
* New RegistryKey helpers for a 64bit System - https://github.com/fsharp/FAKE/pull/580
* New XDTHelper - https://github.com/fsharp/FAKE/pull/556
* Version NAV 800 added - https://github.com/fsharp/FAKE/pull/576
* Feature/list targets in command line - http://fsharp.github.io/FAKE/specifictargets.html
* Use priority list for nuget.exe selection - https://github.com/fsharp/FAKE/issues/572
* BUGFIX: RoundhouseHelper was setting an incorrect switch for CommandTimoutAdmin - https://github.com/fsharp/FAKE/pull/566

#### 3.7.0 - 16.10.2014
* BUGFIX: --single-target didn't work
* NDepend support - https://github.com/fsharp/FAKE/pull/564

#### 3.6.0 - 14.10.2014
* FAKE got a new logo - https://github.com/fsharp/FAKE/pull/553
* Use Paket to handle dependencies - http://fsprojects.github.io/Paket/
* Single target mode --single-target - http://fsharp.github.io/FAKE/specifictargets.html
* New recursive copy functions - https://github.com/fsharp/FAKE/pull/559
* NuGetPack allows to manipulate nuspec files - https://github.com/fsharp/FAKE/pull/554
* Support for MSpec --xml parameter - https://github.com/fsharp/FAKE/pull/545
* Make GetPackageVersion work with Paket - http://fsprojects.github.io/Paket/
* Added missing schemaName parameter for Roundhouse helper - https://github.com/fsharp/FAKE/pull/551
* Roundhouse Cleanup - https://github.com/fsharp/FAKE/pull/550
* Update FSharp.Compiler.Service to 0.0.62
* BUGFIX: If site exists then the site will be modified by IISHelper with the given parameters - https://github.com/fsharp/FAKE/pull/548
* BUGFIX: Messages in FSC task to stderr stream can break the build - https://github.com/fsharp/FAKE/pull/546
* BUGFIX: Use AppVeyor's build version instead of the build number - https://github.com/fsharp/FAKE/pull/560

#### 3.5.0 - 19.09.2014
* Added new SignToolHelper - https://github.com/fsharp/FAKE/pull/535
* Look first in default path for a tool - https://github.com/fsharp/FAKE/pull/542
* Add support for MSBuild Distributed Loggers - https://github.com/fsharp/FAKE/pull/536
* Don't fail on nuget path scanning - https://github.com/fsharp/FAKE/pull/537

#### 3.4.0 - 28.08.2014
* New Xamarin.iOS and Xamarin.Android helpers - https://github.com/fsharp/FAKE/pull/527

#### 3.3.0 - 25.08.2014
* Using JSON.NET 6.0.4
* FAKE.Deploy switched to FCS - https://github.com/fsharp/FAKE/pull/519 
* FAKE.Deploy WorkDirectory fix - https://github.com/fsharp/FAKE/pull/520
* HipChat notification helper - https://github.com/fsharp/FAKE/pull/523
* Don't crash during tool discovery
* NuGet: support fallback framework groups - https://github.com/fsharp/FAKE/pull/514
* New pushd/popd command in FileUtils - https://github.com/fsharp/FAKE/pull/513
* New AppVeyor properties
* FSC - support of compilation for different versions of F#
* Provide env var access to --fsiargs build script args so works on FAKE
* Adding NGen Install task
* Allow to use gacutil
* Allow to use ngen.exe
* Allow to use all sn.exe features
* Adding DisableVerification for StrongNames
* Adding helpers which allow to strong name assemblies
* Allow to use empty MSBuild targets
* Adding setProcessEnvironVar and clearProcessEnvironVar
* Try to reference local nuspec in order to fix https://github.com/fsprojects/FSharp.TypeProviders.StarterPack/pull/33
* Better log messages to fix https://github.com/fsprojects/FSharp.TypeProviders.StarterPack/pull/33
* Fix fsiargs and -d options - https://github.com/fsharp/FAKE/pull/498 https://github.com/fsharp/FAKE/pull/500
* Change RemoveDuplicateFiles & FixMissingFiles to only save on change - https://github.com/fsharp/FAKE/pull/499

#### 3.2.0 - 07.07.2014
* BREAKING CHANGE: API for CreateAssemblyInfoWithConfig was set back to original version
  This resets the breaking change introduced in https://github.com/fsharp/FAKE/pull/471
* Automatic tool search for SpecFlowHelper - https://github.com/fsharp/FAKE/pull/496
* GuardedAwaitObservable was made public by accident - this was fixed
* Add support for remote service admin - https://github.com/fsharp/FAKE/pull/492

#### 3.1.0 - 04.07.2014
* New FSC helper allows to call F# compiler directly from FAKE - https://github.com/fsharp/FAKE/pull/485
* "CustomDictionary" support for FxCop - https://github.com/fsharp/FAKE/pull/489
* Check if file exists before delete in AssemblyInfoFile
* Use FSharp.Compiler.Service 0.0.58
* Report all targets if a target error occurs
* Use FSharp.Compiler.Service with better FSharp.Core resolution - https://github.com/fsharp/FSharp.Compiler.Service/issues/156
* Don't break in MSBuildHelper
* Put FSharp.Core.optdata and FSharp.Core.sigdata into nuget package
* Fixed TargetTracing
* Fixed SourceLinking of FAKE
* Added new exception trap for Fsi creation
* -br in command line will run debugger in F# scripts - https://github.com/fsharp/FAKE/pull/483
* Null check in NuGet helper - https://github.com/fsharp/FAKE/pull/482

#### 3.0.0 - 27.06.2014
* Use FSharp.Compiler.Service 0.0.57 instead of fsi.exe
* Better error message for registry access
* Fall back to 32bit registry keys if 64bit cannot be found
* Improved SqlServer Disconnect error message
* Log "kill all processes" only when needed
* Try to run as x86 due to Dynamics NAV problems
* Allow to use /gac for FxCop
* Make NuGet description fit into single line
* Use Nuget.Core 2.8.2
* Fix NUnitProcessModel.SeparateProcessModel - https://github.com/fsharp/FAKE/pull/474
* Improved CLI documentation - https://github.com/fsharp/FAKE/pull/472
* Added Visual Basic support to AssemblyFileInfo task and make Namespace optional in config - https://github.com/fsharp/FAKE/pull/471
* Added support for OctoTools ignoreExisting flag - https://github.com/fsharp/FAKE/pull/470
* OctoTools samples fixed - https://github.com/fsharp/FAKE/pull/468 https://github.com/fsharp/FAKE/pull/469
* Added support for FxCop /ignoregeneratedcode parameter - https://github.com/fsharp/FAKE/pull/467
* CreateAssemblyInfo works with nonexisting directories - https://github.com/fsharp/FAKE/pull/466

#### 2.18.0 - 11.06.2014
* New (backwards compat) CLI for FAKE that includes FSI cmd args passing - https://github.com/fsharp/FAKE/pull/455
* New updateApplicationSetting method - https://github.com/fsharp/FAKE/pull/462
* Support for msbuild /noconlog - https://github.com/fsharp/FAKE/pull/463
* RoundhouseHelper - https://github.com/fsharp/FAKE/pull/456
* Pass optional arguments to deployment scripts
* Support building source packages without project file
* Display messages when deploy fails
* Fix formatting in FAKE.Deploy docs
* Fix memory usage in FAKE.Deploy
* Increase WebClient's request timeout to 20 minutes - https://github.com/fsharp/FAKE/pull/442
* Mainly Layout fixes and disabling authenticate in FAKE.Deploy https://github.com/fsharp/FAKE/pull/441
* Deploy PDBs via nuget https://github.com/fsharp/FAKE/issues/435
* Release Notes parser should not drop asterisk at end of lines
* Corrected location of @files@ in nuspec sample
* Allow to report tests to AppVeyor
* fix appveyor msbuild logger
* Don't add Teamcity logger if not needed

#### 2.17.0 - 23.05.2014
* Fake.Deploy agent requires user authentication
* Remove AutoOpen von AppVeyor
* fix order of arguments in call to CopyFile
* Support MSTest test settings - https://github.com/fsharp/FAKE/pull/428
* If the NAV error file contains no compile errors return the length

#### 2.16.0 - 21.05.2014
* Promoted the master branch as default branch and removed develop branch
* Remove AutoOpen from TaskRunnerHelper
* Adding Metadata to AsssemblyInfo
* Analyze the Dynamics NAV log file and report the real error count
* Allow to retrieve version no. from assemblies
* Fix issue with symbol packages in NugetHelper
* Fix issues in the ProcessHelper - https://github.com/fsharp/FAKE/pull/412 and https://github.com/fsharp/FAKE/pull/411
* Allow to register BuildFailureTargets - https://github.com/fsharp/FAKE/issues/407
* UnionConverter no longer needed for Json.Net

#### 2.15.0 - 24.04.2014
* Handle problems with ProgramFilesX86 on mono - https://github.com/tpetricek/FsLab/pull/32
* Change the MSBuild 12.0 path settings according to https://github.com/tpetricek/FsLab/pull/32
* Silent mode for MSIHelper - https://github.com/fsharp/FAKE/issues/400

#### 2.14.0 - 22.04.2014
* Support for OpenCover - https://github.com/fsharp/FAKE/pull/398
* Support for ReportsGenerator - https://github.com/fsharp/FAKE/pull/399
* Adding AppVeyor environment variables 
* New BulkReplaceAssemblyInfoVersions task - https://github.com/fsharp/FAKE/pull/394
* Fixed default nuspec file
* "Getting started" tutorial uses better folder structure
* Allows explicit file specification on the NuGetParams Type
* Fix TypeScript output dir
* Add better docs for the TypeScript compiler.
* Don't call the TypeScript compiler more than once
* New parameters for TypeScript

#### 2.13.0 - 04.04.2014
* Enumerate the files lazily in the File|Directory active pattern
* Using Nuget 2.8.1
* Added TypeScript 1.0 support
* Added TypeScript support
* Fixed ProcessTestRunner
* Fixed mono build on Travis

#### 2.12.0 - 31.03.2014
* Add getDependencies to NugetHelper
* SourceLink support
* NancyFx instead of ASP.NET MVC for Fake.Deploy
* Allows to execute processes as unit tests.
* Adding SourceLinks
* Move release management back to the local machine (using this document)
* Allow to run MsTest test in isolation
* Fixed Nuget.packSymbols
* Fixed bug in SemVer parser
* New title property in Nuspec parameters
* Added option to disabled FAKE's automatic process killing
* Better AppyVeyor integration
* Added ability to define custom MSBuild loggers
* Fix for getting the branch name with Git >= 1.9
* Added functions to write and delete from registry
* NUnit NoThread, Domain and StopOnError parameters
* Add support for VS2013 MSTest
* Lots of small fixes

#### 2.2
* Created new packages on nuget:
	* Fake.Deploy - allows to use FAKE scripts in deployment.
	* Fake.Experimental - new stuff where we aren't sure if we want to support it.
	* Fake.Gallio - contains the Gallio runner support.
	* Fake.SQL - Contains tasks for SQL Server.
	* Fake.Core - All the basic features and FAKE.exe.
* Created documentation and tutorials - see http://fsharp.github.io/FAKE/
* New tasks:
	* Added ReleaseNotes parser
	* Added Dynamics NAV helper
	* Added support for MSTest and fixie
	* Parallel NUnit task
	* New AssemblyInfoFile task
	* Support for Octopus Deploy
	* Support for MAGE
	* Suppport for Xamarin's xpkg
	* Many other new tasks
* Fake.Boot
* New Globbing system
* Tons of bug fixes
* Bundles F# 3.0 compiler and FSI.

#### 1.72.0.0

* "RestorePackages" allows to restore nuget packages

#### 1.70.0.0

* FAKE nuget package comes bundles with a fsi.exe
* Self build downloads latest FAKE master via nuget

#### 1.66.1.0

* Fixed bug where FAKE.Deploy didn't run the deploy scripts where used as a windows service
* It's possible to add file loggers for MSBuild
* Fixed path resolution for fsi on *nix
* BREAKING CHANGE: Removed version normalization from NuGet package creation
* Fixes for NUNit compatibility on mono 
* Fixes in ProcessHelper for mono compatibility
* Fixes in the mono build
* Improved error reporting in Fake.exe
* Added a SpecFlow helper
* Fixed some issues in file helper routines when working with no existing directory chain

#### 1.64.1.0

* Fixed bug where FAKE didn't run the correct build script

#### 1.64.0.0

* New conditional dependency operator =?>
* BREAKING CHANGE: Some AssemblyInfo task parameters are now option types. See type hints.

#### 1.62.0.0

* New RegAsm task, allows to create TLBs from a dll.
* New MSI task, allows to install or uninstall msi files.
* StringHelper.NormalizeVersion fixed for WiX.

#### 1.58.9.0

* Allow to choose specific nunit-console runner.

#### 1.58.6.0

* Using nuget packages for mspec.
* FAKE tries to kill all MSBuild and FSI processes at the end of a build.

#### 1.58.1.0

* Removed message system for build output. Back to simpler tracing.

#### 1.58.0.0

* ReplaceAssemblyInfoVersions task allows to replace version info in AssemblyVersion-files
* New task ConvertFileToWindowsLineBreaks

#### 1.56.10.0

* Allows to build .sln files

#### 1.56.0.0

* Allows to publish symbols via nuget.exe
* Autotrim trailing .0 from version in order to fullfill nuget standards.

#### 1.54.0.0

* If the publishment of a Nuget package fails, then FAKE will try it again.
* Added Changelog.markdown to FAKE deployment
* Added RequireExactly helper function in order to require a specific nuget dependency.
* NugetHelper.GetPackageVersion - Gets the version no. for a given package in the packages folder.
* EnvironmentHelper.getTargetPlatformDir - Gets the directory for the given target platform.

#### 1.52.0.0

* Some smaller bugfixes
* New dependency syntax with ==> and <=>
* Tracing of StackTrace only if TargetHelper.PrintStackTraceOnError was set to true

#### 1.50.0.0

* New task DeleteDirs allows to delete multiple directories.
* New parameter for NuGet dependencies.

#### 1.48.0.0

* Bundled with docu.exe compiled against .Net 4.0.
* Fixed docu calls to run with full filenames.
* Added targetplatform, target and log switches for ILMerge task.
* Added Git.Information.getLastTag() which gets the last git tag by calling git describe.
* Added Git.Information.getCurrentHash() which gets the last current sha1.

#### 1.46.0.0

* Fixed Nuget support and allows automatic push.

#### 1.44.0.0

* Tracing of all external process starts.
* MSpec support.
