# Make BDD with Gherkin and SpecFlow

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later. The old documentation can be found <a href="apidocs/v4/fake-specflowhelper.html">here</a></p>
</div>

Use [SpecFlow] to define, manage and automatically execute human-readable acceptance tests in .NET projects. Writing easily understandable tests is a cornerstone of the BDD paradigm and also helps build up a living documentation of your system.

SpecFlow is open source and provided under a BSD license. As part of the Cucumber family, SpecFlow uses the official Gherkin parser and supports the .NET framework, Xamarin and Mono.

The package Fake.DotNet.Testing.SpecFlow is a bridge to the [SpecFlow] CLI (specflow.exe).

**If you use SpecFlow in a version >= 2.4, then please use the SpecFlowNext module**

[API-Reference](https://fake.build/apidocs/v5/fake-dotnet-testing-specflow.html)

## Minimal working example (v2.4+)

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

## Minimal working example (pre v2.4)

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Testing.SpecFlow //"

open Fake.Core
open Fake.DotNet.Testing

let specsProject = "IntegrationTests.csproj"

Target.create "Regenerate Test Classes" (fun _ ->
    SpecFlow.run (fun p ->
        { p with ProjectFile = specsProject })
)

Target.create "Create StepDefinition Report" (fun _ ->
    SpecFlow.run (fun p ->
        { p with SubCommand = "stepdefinitionreport"
                 ProjectFile = specsProject
                 BinFolder = "bin/Debug"
                 OutputFile = "StepDefinitionReport.html" })
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

[SpecFlow]: http://specflow.org
