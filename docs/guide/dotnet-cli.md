# Run the 'dotnet' sdk command line tool

The `dotnet` command line tool can build and publish projects.

## Minimal working example

```fsharp
#r "paket:
nuget Fake.DotNet.Cli //"
open Fake.DotNet

// Lazily install DotNet SDK in the correct version if not available
let install = lazy DotNet.install DotNet.Versions.Release_2_1_4

// Alternative: Read from global json
let install = lazy DotNet.install DotNet.Versions.FromGlobalJson

// Define general properties across various commands (with arguments)
let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

// Set general properties without arguments
let inline dotnetSimple arg = DotNet.Options.lift install.Value arg

// Use defined properties on "DotNet.Exec"
DotNet.exec (withWorkDir "./test") "build" ""
DotNet.exec dotnetSimple "build" "myproject.fsproj"
DotNet.exec dotnetSimple "build" "mysolution.sln"

// Use defined properties on more generalized functions like "DotNet.Restore"
DotNet.restore dotnetSimple "mysolution.sln"

// Define more general properties in addition to the general ones
DotNet.restore (fun args ->
    { args with
        NoCache = true
    } |> dotnetSimple) "mysolution.sln"

// Define more general properties in addition to the general ones, with arugments
DotNet.restore (fun args ->
    { args with
        Runtime = Some "win-x86"
    } |> withWorkDir "./test" ) "mysolution.sln"
```

More on [*API Documentation*](/reference/fake-dotnet-dotnet.html)

## Run tests

`DotNet.test` runs `dotnet test` with the options of the VSTest runner, which `dotnet test` uses by default:

```fsharp
DotNet.test
    (fun options ->
        { options with
            NoBuild = true
            Logger = Some "trx" })
    "tests/MyProject.Tests/MyProject.Tests.fsproj"
```

### Tests on Microsoft.Testing.Platform

Since the .NET SDK 10, `dotnet test` has a second runner for test applications that are built on
[*Microsoft.Testing.Platform*](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro),
for example MSTest, NUnit, xUnit.net v3 or TUnit projects that use that platform. The `global.json` file selects it:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

With that runner `dotnet test` has
[*another command line*](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp): the project is named by
`--project` instead of being the first argument, and the options of VSTest such as `--logger` or `--collect` do not
exist. `DotNet.test` fails with that runner, because it passes the project as the first argument and adds MSBuild
switches that the runner hands over to the test applications. Use `DotNet.testMTP` instead:

```fsharp
DotNet.testMTP
    (fun options ->
        { options with
            NoBuild = true
            Configuration = Some DotNet.BuildConfiguration.Release
            ResultsDirectory = Some "TestResults"
            TrxReport = Some(DotNet.MTPTrxReportOptions.Create())
            CodeCoverage =
                Some
                    { DotNet.MTPCodeCoverageOptions.Create() with
                        OutputFormat = Some DotNet.MTPCoverageFormat.Cobertura } })
    (DotNet.MTPTestTarget.Project "tests/MyProject.Tests/MyProject.Tests.fsproj")
```

This runs

```shell
dotnet test --project tests/MyProject.Tests/MyProject.Tests.fsproj --configuration Release --no-build --results-directory TestResults -- --report-trx --coverage --coverage-output-format cobertura
```

- The second argument selects what is tested: `DotNet.MTPTestTarget.Project`, `DotNet.MTPTestTarget.Solution`,
  `DotNet.MTPTestTarget.TestModules` for test applications that are already built, or
  `DotNet.MTPTestTarget.CurrentDirectory` for the project or solution of the working directory.
- The options behind the `--` are passed to the test applications. The TRX report, the code coverage, the crash dump
  and the hang dump are extensions of the platform: a test application that does not reference the package of the
  extension rejects its options, and the test run fails. The API documentation of `DotNet.MTPTrxReportOptions`,
  `DotNet.MTPCodeCoverageOptions`, `DotNet.MTPCrashDumpOptions` and `DotNet.MTPHangDumpOptions` names the package.
- Options that have no field go to `ApplicationArguments` when they belong to a test framework or to an extension,
  for example `ApplicationArguments = [ "--filter-class"; "MyNamespace.MyTests" ]` for xUnit.net v3, and to
  `Common.CustomParams` when they belong to `dotnet test`.
- Of the MSBuild parameters only properties can be passed (`Properties`), because with that runner `dotnet test`
  hands other MSBuild switches such as `/nodeReuse:False` over to the test applications, which reject them. FAKE
  does not attach its binary log to this command either: build the projects with `DotNet.build` first if you rely
  on FAKE to report the warnings and errors of the build to the build server.

## SDK tools (local, global, cli-reference)

Some dotnet SDK based tools support project or path based installation. These tools have a `ToolType` parameter in addition to 
the `ToolPath` or `ExePath` parameters.

> Note: If your tool doesn't have this parameter please send a pull request to add it. See the 
> [*`ReportGenerator.fs` file changes in PR 2399*](https://github.com/fsharp/FAKE/pull/2399/files#diff-6bd782ab06dfa727e4e35ce4bbaae43c) 
> on an example what needs to be changed. Basically `CreateProcess.withFramework` is replaced with `CreateProcess.withToolType`.

You can use the parameter similar to this (in this example to start the report-generator as local tool with `dotnet reportgenerator`):

```fsharp
let install = lazy DotNet.install DotNet.Versions.FromGlobalJson
Target.create "Generate Reports" (fun _ ->
  let parameters p = { p with ToolType = ToolType.CreateLocalTool(install.Value) }
  !! "**/opencover.xml"
  |> ReportGenerator.generateReports parameters
)
```

Here are the possible options:

- `ToolType.CreateFullFramework()`: Start as dotnet global tool (`<tool>.exe`, `mono` prefix on unix). This is the default and how fake behaved historically for most tools.
- `ToolType.CreateFrameworkDependentDeployment(install.Value)`: Start as framework dependendt deployment (`dotnet <tool>.dll`, no `mono` prefix on unix)
- `ToolType.CreateGlobalTool()`: Start as dotnet global tool (`<tool>.exe`, no `mono` prefix on unix)
- `ToolType.CreateLocalTool(install.Value)`: Start as dotnet local tool (`dotnet <tool>`)
- `ToolType.CreateCLIToolReference(install.Value)`: Start as dotnet cli tool reference (`dotnet <tool>`)

To set a different tool command (first argument of `dotnet`) `DotNet.Option`, for example because you use your own package with a different tool name. You can use:

```fsharp
    { p with
        ToolType =
            ToolType.CreateLocalTool(install.Value)
            |> ToolType.withDefaultToolCommandName "alternative"  }
```

This will call `dotnet alternative <arguments>`
