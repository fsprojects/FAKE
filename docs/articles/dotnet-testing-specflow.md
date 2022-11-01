# Make BDD with Gherkin and SpecFlow

[SpecFlow](http://www.specflow.org/) is used to define, manage and automatically execute human-readable acceptance tests in .NET projects. 
Writing easily understandable tests is a cornerstone of the BDD paradigm and also helps build up a living documentation of your system.

SpecFlow is open source and provided under a BSD license. As part of the Cucumber family, SpecFlow uses the official Gherkin parser 
and supports the .NET framework, Xamarin and Mono.

The package `Fake.DotNet.Testing.SpecFlow` is a bridge to the [*SpecFlow*](specflow.exe) CLI.

To see the available SpecFlow APIs in FAKE, please see the [`API-Reference`](/reference/fake-dotnet-testing-specflow.html) 
for the SpecFlow module.

## Minimal working example

Following FSI script shows a basic usage of SpecFlow module:

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Testing.SpecFlow //"

open Fake.Core
open Fake.DotNet.Testing

let specsProject = "IntegrationTests.csproj"

Target.create "Regenerate Test Classes" (fun _ ->
    specsProject |> SpecFlowNext.run id
)

Target.create "Create StepDefinition Report" (fun _ ->
    specsProject
    |>  SpecFlowNext.run (fun p ->
            { p with 
                SubCommand = StepDefinitionReport
                BinFolder = Some "bin/Debug"
                OutputFile = Some "StepDefinitionReport.html" })
)

Target.create "Default" Target.DoNothing

"Clean"
==> "Regenerate Test Classes"
==> "Build"
==> "Run Integration Tests"
==> "Create StepDefinition Report"
==> "Default"

Target.runOrDefault "Default"
```
