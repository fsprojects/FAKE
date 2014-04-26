#### 2.15.4 - 24.4.2014
* Fix issues in the ProcessHelper - https://github.com/fsharp/FAKE/pull/412 and https://github.com/fsharp/FAKE/pull/411

#### 2.15.2 - 24.4.2014
* Allow to register BuildFailureTargets - https://github.com/fsharp/FAKE/issues/407
* UnionConverter no longer needed for Json.Net

#### 2.14.13 - 24.04.2014
* Handle problems with ProgramFilesX86 on mono - https://github.com/tpetricek/FsLab/pull/32

#### 2.14.12 - 24.04.2014
* Change the MSBuild 12.0 path settings according to https://github.com/tpetricek/FsLab/pull/32

#### 2.14.1 - 23.04.2014
* Silent mode for MSIHelper - https://github.com/fsharp/FAKE/issues/400

#### 2.14.0 - 22.04.2014
* Support for OpenCover - https://github.com/fsharp/FAKE/pull/398
* Support for ReportsGenerator - https://github.com/fsharp/FAKE/pull/399

#### 2.13.4 - 14.04.2014
* Adding AppVeyor environment variables 
* New BulkReplaceAssemblyInfoVersions task - https://github.com/fsharp/FAKE/pull/394
* Fixed default nuspec file
* "Getting started" tutorial uses better folder structure

#### 2.13.3 - 09.04.2014
* Allows explicit file specification on the NuGetParams Type

#### 2.13.2 - 07.04.2014
* Fix TypeScript output dir
* Add better docs for the TypeScript compiler.

#### 2.13.1 - 05.04.2014
* Don't call the TypeScript compiler more than once
* New parameters for TypeScript

#### 2.13.0 - 04.04.2014
* Enumerate the files lazily in the File|Directory active pattern
* Using Nuget 2.8.1

#### 2.13.0-alpha2 - 03.04.2014
* Added TypeScript 1.0 support

#### 2.13.0-alpha1 - 02.04.2014
* Added TypeScript support

#### 2.12.2 - 31.03.2014
* Fixed ProcessTestRunner

#### 2.12.1-alpha3 - 31.03.2014
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