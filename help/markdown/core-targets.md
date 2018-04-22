# Running targets in "FAKE - F# Make"

**Note:  This documentation is for FAKE 5. The old documentation can be found [here](legacy-core-targets.html)! **

[API-Reference](apidocs/fake-core-target.html), [Operators](apidocs/fake-core-targetoperators.html)

## Command line interface for the target module

If you use the `Fake.Core.Target` module and call `Target.runOrDefault` or `Target.runOrList` in your build script you will have the following CLI options:

```help
Usage:
  fake-run --list
  fake-run --version
  fake-run --help | -h
  fake-run [target_opts] [target <target>] [--] [<targetargs>...]

Target Module Options [target_opts]:
    -t, --target <target>
                          Run the given target (ignored if positional argument 'target' is given)
    -e, --environmentvariable <keyval> [*]
                          Set an environment variable. Use 'key=val'. Consider using regular arguments, see https://fake.build/core-targets.html 
    -s, --singletarget    Run only the specified target.
    -p, --parallel <num>  Run parallel with the given number of tasks.
```

> please refer to the general [FAKE 5 runner command line interface](fake-commandline.html) or the [Fake.Core.CommandLineParsing documentation](core-commandlineparsing.html) for details.

This means you can - for example - run `fake run build.fsx --list`
or `fake build --list` to list your targets.

To run a target `MyTarget` you could use  `fake run build.fsx -t MyTarget` or `fake build target MyTarget` (or the other way around `fake run build.fsx target MyTarget`)

All parameters after `--` or `target <target>` are given to the target as paramters.

> Note that the ordering of the paramters matters! This means the following are invalid (which is different to pre FAKE 5 versions):
> - `fake run -t Target build.fsx` - because of ordering fake will assume `-t` to be the script name
> - `fake build -v` - It will not run FAKE in verbose mode but give the parameter `-v` to the target parameters. This is because there is no `-v` in the above CLI.
>
> In general you should use the command-line in a way to not be broken when new parameters are added.
> Use longer forms in your scripts and shorter forms on your shell!

## Running specific targets

FAKE has a special param "target" which can be used to run specific targets in a build. We assume the following build script (`build.fsx`):

```fsharp
#r "paket:
nuget Fake.Core.Target //"

open Fake.Core

// *** Define Targets ***
Target.create "Clean" (fun p ->
    // Access arguments given by command-line
    Trace.tracefn "Arguments: %A" p.Context.Arguments
    Trace.trace " --- Cleaning stuff --- "
)

Target.create "Build" (fun _ ->
    Trace.trace " --- Building the app --- "
)

Target.create "Deploy" (fun _ ->
    Trace.trace " --- Deploying app --- "
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
    ==> "Build"
    ==> "Deploy"

// *** Start Build ***
Target.runOrDefault "Deploy"
```

> Warning: Previous versions of FAKE 5 used `(fun () -> ...)` instead of `(fun _ -> ...)`.
> We decided to change the syntax here to introduce some parameters or other features at a later point.
> Using the current parameter object is not supported yet.

Now we have the following options:

- `fake run build.fsx -t "Build"` --> starts the *Build* target and runs the dependency *Clean*
- `fake run build.fsx -t "Build"` --single-target --> starts only the *Build* target and runs no dependencies
- `fake run build.fsx -s -t Build` --> starts only the *Build* target and runs no dependencies
- `fake run build.fsx` --> starts the Deploy target (and runs the dependencies *Clean* and *Build*)

## Targets with arguments

Example:

```fsharp
Target.create "MyTarget" (fun p ->
    // Access arguments given by command-line
    Trace.tracefn "Arguments: %A" p.Context.Arguments
)
```

Everything after the target will be interpreted as argument for the target:

- `fake run build.fsx target MyTarget --arg` --> `--arg` will be contained in `p.Context.Arguments`
- `fake build -t MyTarget --arg` --> `--arg` will be contained in `p.Context.Arguments`, because --arg is not a valid argument for the `Fake.Core.Target` (see command line spec above)

You can access the arguments from every target executed along the way.

## Final targets

Final targets can be used for TearDown functionality.
These targets will be executed even if the build fails but have to be activated via Target.ActivateFinal().

```fsharp
Target.createFinal "CloseSomePrograms" (fun _ ->
    // close stuff and release resources
    ()
)

// Activate Final target somewhere during build
Target.activateFinal "CloseSomePrograms"
```

## Build failure targets

Build failure targets can be used to execute tasks after a build failure.
These targets will be executed only after a build failure but have to be activated via `activateBuildFailure()`.

```fsharp
Target.createBuildFailure "ReportErrorViaMail" (fun _ ->
    // send mail about the failure
    ()
)

// Activate Build Failure Target somewhere during build
Target.activateBuildFailure "ReportErrorViaMail"
```

## Using FAKE's parallel option

Since multithreading is beneficial (especially for large projects) FAKE allows to specify the
number of threads used for traversing the dependency tree.
This option of course only affects independent targets whereas dependent targets will
still be exectued in order.

### Setting the number of threads

The number of threads used can be set using the environment variable ``parallel-jobs`` or using the `--parallel` parameter.
This can be achieved in various ways where the easiest one is to use the parameter:

``fake run *YourBuildScript* --parallel 8``

Note that the dependency tree will be traversed as usual whenever setting ``parallel-jobs`` to a value ``<= 1`` or omiting it entirely.

## Issues

- Running targets in parallel is of course only possible when the target-functions themselves are thread-safe.
- Parallel execution may also cause races on stdout and build-logs may therefore be quite obfuscated.
- Error detection may suffer since it's not possible to determine a first error when targets are running in parallel

Due to these limitations it is recommended to use the standard sequential build whenever checking for errors (CI, etc.)
However when a fast build is desired (and the project is e.g. known to build successfully) the parallel option might be helpful

## Example

When using this parallel option, Fake resolves the build dependency hierarchies from the described paths and builds independend paths as parallel if you have multiple CPUs available.
For example this dependency tree:

```fsharp
"Task 1"
    ==> "Task A2"
    ==> "Task 3"

"Task 1"
    ==> "Task B2"
    ==> "Task 3"

"Task C2"
    ==> "Task 3"

"Task 3"
    ==> "Task A4"

"Task 3"
    ==> "Task B4"
```

...would be treated as follows:

![](pics/parallel/ParallelExample.png)

This is in addition to that that MSBuild may use multiple threads when building one solution having multiple independent project-files.

# Soft dependencies

Typically you will define dependencies among your targets using the `==>` and `<==` operators, and these
dependencies define the order in which the targets are executed during a build.

You can also define soft dependencies among targets using the  `?=>` and `<=?` operators.  For example, you might
say that target B has a soft dependency on target A:

```fsharp
"A" ?=> "B"
// Or equivalently
"B" <=? "A"
```

With this soft dependency, running B will not require that A be run first. However it does mean that *if* A is run
(due to other dependencies) it must be run before B.

## Example

```fsharp
// *** Define Targets ***
Target.create "Clean" (fun _ ->
    Trace.trace " --- Cleaning stuff --- "
)

Target.create "Build" (fun _ ->
    Trace.trace " --- Building the app --- "
)

Target.create "Rebuild" Target.DoNothing

// *** Define Dependencies ***
"Build" ==> "Rebuild"
"Clean" ==> "Rebuild"
// Make sure "Clean" happens before "Build", if "Clean" is executed during a build.
"Clean" ?=> "Build"
```
