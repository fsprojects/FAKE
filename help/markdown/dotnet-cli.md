# Run the 'dotnet' sdk command line tool

The `dotnet` command line tool can build and publish projects.

## Minimal working example

```fsharp
#r "paket:
nuget Fake.DotNet.Cli //"
open Fake.DotNet

// Lazily install DotNet SDK in the correct version if not available
let install = lazy DotNet.Install DotNet.Release_2_1_4

// Define general properties across various commands (with arguments)
let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

// Set general properties without arguments
let inline dotnetSimple arg = DotNet.Options.lift install.Value arg

// Use defined properties on "DotNet.Exec"
DotNet.Exec (withWorkDir "./test") "build" ""
DotNet.Exec dotnetSimple "build" "myproject.fsproj"
DotNet.Exec dotnetSimple "build" "mysolution.sln"

// Use defined properties on more generalized functions like "DotNet.Restore"
DotNet.Restore dotnetSimple "mysolution.sln"

// Define more general properties in addition to the general ones
DotNet.Restore (fun args ->
    { args with
        NoCache = true
    } |> dotnetSimple) "mysolution.sln"

// Define more general properties in addition to the general ones, with arugments
DotNet.Restore (fun args ->
    { args with
        Runtime = Some "win7-x86"
    } |> withWorkDir "./test" ) "mysolution.sln"
```

More [API Documentation](apidocs/fake-dotnet-cli.html)
