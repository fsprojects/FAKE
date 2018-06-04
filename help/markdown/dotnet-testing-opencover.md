# Analyze your code coverage with OpenCover

From the [project](https://github.com/OpenCover/opencover):
>"A code coverage tool for .NET 2 and above (WINDOWS OS only), support for 32 and 64 processes with both branch and sequence points."

It will analyze the code coverage during testing and generates an XML report which can be used to generates HTML pages or uploaded to online services like [coveralls](https://coveralls.io/) or [codecov](https://codecov.io/).

## Minimal working example

```fsharp
#r "paket:
nuget Fake.DotNet.Testing.OpenCover //"
open Fake.DotNet.Testing

OpenCover.run (fun p ->
    { p with
        TestRunnerExePath = "./Tools/NUnit/nunit-console.exe";
    })
    "project-file.nunit /config:Release /noshadow /xml:artifacts/nunit.xml /framework:net-4.0"
```

By default, the OpenCover module looks for the OpenCover Console in the OpenCover installation path '%LOCALAPPDATA%/Apps/OpenCover' directory. This can be overwritten using the `ExePath` property of the parameters.

## Version

```fsharp
#r "paket:
nuget Fake.DotNet.Testing.OpenCover //"
open Fake.DotNet.Testing

OpenCover.getVersion None
```

## Full example

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Testing.OpenCover //"
open Fake.Core
open Fake.DotNet.Testing
open Fake.DotNet.Testing.OpenCover

Target.create "OpenCover" (fun _ ->
    OpenCover.getVersion (fun p -> { p with ExePath = "./tools/OpenCover/tools/OpenCover.Console.exe" })

    OpenCover.run (fun p ->
    { p with
            ExePath = "./tools/OpenCover/tools/OpenCover.Console.exe"
            TestRunnerExePath = "./tools/xunit.runner.console/tools/xunit.console.exe";
            Output = "coverage.xml";
            Register = RegisterUser;
            Filter = "+[MyProject]*";
            ExcludeByAttribute = [ "*.ExcludeFromCodeCoverage*" ];
            ExcludeByFile = [ "Program.cs"; "Window.cs" ];
            ExcludeDirs = [ "Test1"; "Test2" ];
            HideSkipped = [File; Attribute];
            MergeOutput = true;
            ReturnTargetCode = Offset 5;
            SearchDirs = [ "c:\projects\common\bin\debug\dnx451" ];
            SkipAutoProps = true;
    })
    "MyProject.Tests.dll -noshadow"
)
```
