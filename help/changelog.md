## 1.72.0.0

* "RestorePackages" allows to restore nuget packages

## 1.70.0.0

* FAKE nuget package comes bundles with a fsi.exe
* Self build downloads latest FAKE master via nuget

## 1.66.1.0

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

## 1.64.1.0

* Fixed bug where FAKE didn't run the correct build script

## 1.64.0.0

* New conditional dependency operator =?>
* BREAKING CHANGE: Some AssemblyInfo task parameters are now option types. See type hints.

## 1.62.0.0

* New RegAsm task, allows to create TLBs from a dll.
* New MSI task, allows to install or uninstall msi files.
* StringHelper.NormalizeVersion fixed for WiX.

## 1.58.9.0

* Allow to choose specific nunit-console runner.

## 1.58.6.0

* Using nuget packages for mspec.
* FAKE tries to kill all MSBuild and FSI processes at the end of a build.

## 1.58.1.0

* Removed message system for build output. Back to simpler tracing.

## 1.58.0.0

* ReplaceAssemblyInfoVersions task allows to replace version info in AssemblyVersion-files
* New task ConvertFileToWindowsLineBreaks

## 1.56.10.0

* Allows to build .sln files

## 1.56.0.0

* Allows to publish symbols via nuget.exe
* Autotrim trailing .0 from version in order to fullfill nuget standards.

## 1.54.0.0

* If the publishment of a Nuget package fails, then FAKE will try it again.
* Added Changelog.markdown to FAKE deployment
* Added RequireExactly helper function in order to require a specific nuget dependency.
* NugetHelper.GetPackageVersion - Gets the version no. for a given package in the packages folder.
* EnvironmentHelper.getTargetPlatformDir - Gets the directory for the given target platform.

## 1.52.0.0

* Some smaller bugfixes
* New dependency syntax with ==> and <=>
* Tracing of StackTrace only if TargetHelper.PrintStackTraceOnError was set to true

## 1.50.0.0

* New task DeleteDirs allows to delete multiple directories.
* New parameter for NuGet dependencies.

## 1.48.0.0

* Bundled with docu.exe compiled against .Net 4.0.
* Fixed docu calls to run with full filenames.
* Added targetplatform, target and log switches for ILMerge task.
* Added Git.Information.getLastTag() which gets the last git tag by calling git describe.
* Added Git.Information.getCurrentHash() which gets the last current sha1.

## 1.46.0.0

* Fixed Nuget support and allows automatic push.

## 1.44.0.0

* Tracing of all external process starts.
* MSpec support.
