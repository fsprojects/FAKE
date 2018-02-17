# Run the 'dotnet' sdk command line tool

The `dotnet` command line tool can build and publish projects.

## Minimal working example

```fsharp
#r "paket:
nuget Fake.DotNet.Cli //"
open Fake.DotNet

Cli.DotNet id "build" ""
Cli.DotNet id "build" "myproject.fsproj"
Cli.DotNet id "build" "mysolution.sln"
```

More [API Documentation](apidocs/fake-dotnet-cli.html)
