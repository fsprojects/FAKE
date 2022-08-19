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
        Runtime = Some "win7-x86"
    } |> withWorkDir "./test" ) "mysolution.sln"
```

More [API Documentation](apidocs/v5/fake-dotnet-dotnet.html)

## SDK tools (local, global, clireference)

Some dotnet SDK based tools support project or path based installation. These tools have a `ToolType` parameter in addition to the `ToolPath` or `ExePath` parameters.

> Note: If your tool doesn't have this parameter please send a pull request to add it. See the [`ReportGenerator.fs` file changes in PR 2399](https://github.com/fsharp/FAKE/pull/2399/files#diff-6bd782ab06dfa727e4e35ce4bbaae43c) on an example what needs to be changed. Basically `CreateProcess.withFramework` is replaced with `CreateProcess.withToolType`.

You can use the parameter similar to this (in this example to start the reportgenerator as local tool with `dotnet reportgenerator`):

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