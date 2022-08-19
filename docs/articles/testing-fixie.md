# Running Unit Tests using Fixie

FAKE can be used to run unit tests using the Fixie testing framework. Your can use it with FAKE in three 
steps: Create a test project, add Fixie to it, and add a FAKE script to call Fixie.

You can read up more on Fixie getting started guide [*here*](https://github.com/fixie/fixie/wiki).

## Sample usage

Create a FAKE script in the root directory of your test project and add the following sample Fixie target to it:

```fsharp
open Fake.Core
open Fake.Testing

Target.create "Fixie" (fun _ ->
Fixie.Fixie (fun p -> { p with CustomArguments = ["custom","1"; "test","2"] })
)

Target.runOrDefault "Fixie"
```

## Arguments

You can provide the following arguments:

* `Configuration` - The configuration for the build. When this option is omitted, the default configuration is Debug.
* `NoBuild` - Skip building the test project prior to running it.
* `Framework` - Only run test assemblies targeting a specific framework.
* `Report` - Write test results to the specified path, using the xUnit XML format.
* `CustomArguments` - Arbitrary arguments made available to custom discovery/execution classes.
