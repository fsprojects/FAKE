# Getting started with FAKE - F# Make

**Note:  This documentation is for FAKE.exe version 5.0 or later. The old documentation can be found [here](legacy-gettingstarted.html)**

In this tutorial you will learn how to set up a complete build infrastructure with "FAKE - F# Make". This includes:

* how to install the latest FAKE version
* how to automatically compile your C# or F# projects
* how to automatically resolve nuget dependencies
* how to automatically run NUnit tests on your projects
* how to zip the output to a deployment folder

## Install FAKE

"FAKE - F# Make" is completely written in F# and all build scripts will also be written in F#, but this doesn't imply that you have to learn programming in F#. In fact the "FAKE - F# Make" syntax is hopefully very easy to learn.

There are various ways to install FAKE 5

- Install the 'fake' or 'fake-netcore' package for you system (currenty chocolatey)
  Example `choco install fake -pre`
- Use it as dotnet tool (soon)
- Bootstrap via shell script (build.cmd/build.sh) (documented soon)
  > DISCLAIMER: These scripts have no versioning story. You either need to take care of versions yourself (and lock them) or your builds might break on major releases.


## TBD.