# Running targets in "FAKE - F# Make"

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later. The old documentation can be found <a href="legacy-core-targets.html">here</a></p>
</div>

[API-Reference](apidocs/v5/fake-core-target.html), [Operators](apidocs/v5/fake-core-targetoperators.html)

## Command line interface for the target module

If you use the `Fake.Core.Target` module and call `Target.runOrDefault` or `Target.runOrList` in your build script you will have the following CLI options:

```bash
Usage:
  fake-run --list
  fake-run --version
  fake-run --help | -h
  fake-run [target_opts] [target <target>] [--] [<targetargs>...]

Target Module Options [target_opts]:
    -t, --target <target>
                          Run the given target (ignored if positional argument 'target' is given)
    -e, --environment-variable <keyval> [*]
                          Set an environment variable. Use 'key=val'. Consider using regular arguments, see https://fake.build/core-targets.html 
    -s, --single-target    Run only the specified target.
    -p, --parallel <num>  Run parallel with the given number of tasks.
```

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>Please refer to the general <a href="fake-commandline.html">FAKE 5 runner command line interface</a> or the <a href="core-commandlineparsing.html">Fake.Core.CommandLineParsing documentation</a></p> 
</div>

This means you can - for example - run `fake run build.fsx --list`
or `fake build --list` to list your targets.

To run a target `MyTarget` you could use  `fake run build.fsx -t MyTarget` or `fake build target MyTarget` (or the other way around `fake run build.fsx target MyTarget`)

All parameters after `--` or `target <target>` are given to the target as paramters. Note that this feature needs to be enabled by using `Target.runOrDefaultWithArguments` instead of `Target.runOrDefault`!

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>Note that the ordering of the parameters matters! This means the following are invalid (which is different to pre FAKE 5 versions):
    <ul>
        <li><code>fake run -t Target build.fs</code> - because of ordering fake will assume <code>-t</code> to be the script name </li>
        <li> <code>fake build -v</code> - It will not run FAKE in verbose mode but give the parameter <code>-v</code> to the target parameters. This is because there is no <code>-v</code> in the above CLI.</li>
    </ul>
    In general you should use the command-line in a way to not be broken when new parameters are added.
    Use longer forms in your scripts and shorter forms on your shell!</p>
</div>



## Running specific targets

FAKE has a special param "target" which can be used to run specific targets in a build. We assume the following build script (`build.fsx`):

```fsharp
#r "paket:
nuget Fake.Core.Target //"

open Fake.Core
Target.initEnvironment()

// *** Define Targets ***
Target.create "Clean" (fun p ->
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

<div class="alert alert-warning">
    <h5>WARNING</h5>
    <p>
    Previous versions of FAKE 5 used <code>(fun () -> ...)</code> instead of <code>(fun _ -> ...)</code>.
    We decided to change the syntax here to introduce some parameters or other features at a later point.
    Using the current parameter object is not supported yet.
    </p> 
</div>



Now we have the following options:

- `fake run build.fsx -t "Build"` --> starts the *Build* target and runs the dependency *Clean*
- `fake run build.fsx -t "Build"` --single-target --> starts only the *Build* target and runs no dependencies
- `fake run build.fsx -s -t Build` --> starts only the *Build* target and runs no dependencies
- `fake run build.fsx` --> starts the Deploy target (and runs the dependencies *Clean* and *Build*)

## Script with arguments

Example:

```fsharp
let args = Target.getArguments() // use this at the top of your script isntead of `Target.initEnvironment()`

// So some stuff depending on the args
match args with
| Some args ->
    Trace.tracefn "Arguments: %A" args
| None ->
    // This case happens when no execution is requested (for example `--list` for listing targets)
    // Even for empty arguments `Some [||]` is given, read docs for `Target.GetArguments()`
    // never execute any side-effects outside of targets when `None` is given. 
    // NOTE: IDE will only show targets defined in this code-path, so never define targets based on arguments or environment variables.
    ()

// Set your own variable/s based on the args
let myVerbose, myArg =
    match args with
    | Some args -> 
        // Or use Fake.Core.CommandLineParsing here: https://fake.build/core-commandlineparsing.html
        args |> Seq.contains "--myverbose",
        args |> Seq.contains "--arg"
    | None -> false

Target.create "Default" (fun _ ->
    if myArg then
        printfn "do something special" 
)


// Feature is opt-in in order to provide good error messages out of the box
// see https://github.com/fsharp/FAKE/issues/1896
Target.runOrDefaultWithArguments "Default"
```

Everything after the target will be interpreted as argument for the target:

- `fake run build.fsx target MyTarget --arg` --> `--arg` will be contained in `args`
- `fake build -t MyTarget --arg` --> `--arg` will be contained in `args`, because `--arg` is not a valid argument for the `Fake.Core.Target` (see command line spec above)

## Targets with arguments

Example:

```fsharp
Target.create "MyTarget" (fun p ->
    // Access arguments given by command-line
    Trace.tracefn "Arguments: %A" p.Context.Arguments
)

// Feature is opt-in in order to provide good error messages out of the box
// see https://github.com/fsharp/FAKE/issues/1896
Target.runOrDefaultWithArguments "Deploy"
```

Everything after the target will be interpreted as argument for the target:

- `fake run build.fsx target MyTarget --arg` --> `--arg` will be contained in `p.Context.Arguments`
- `fake build -t MyTarget --arg` --> `--arg` will be contained in `p.Context.Arguments`, because --arg is not a valid argument for the `Fake.Core.Target` (see command line spec above)

You can access the arguments from every target executed along the way.

## Setting build status

You can set the build status automatically using `Target.updateBuildStatus`. To do this you need to use the `Target.WithContext` functions to run a target and retrieve the context information:

Example: 

```fsharp
#r "paket:
nuget Fake.Core.Target //"

open Fake.Core
Target.initEnvironment()

// *** Define Targets ***
Target.create "Clean" (fun p ->
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
let ctx = Target.WithContext.runOrDefault "Deploy"
Target.updateBuildStatus ctx
Target.raiseIfError ctx // important to have proper exit code on build failures.
```

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
still be executed in order.

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

Target.create "Rebuild" ignore

// *** Define Dependencies ***
"Build" ==> "Rebuild"
"Clean" ==> "Rebuild"
// Make sure "Clean" happens before "Build", if "Clean" is executed during a build.
"Clean" ?=> "Build"
```
