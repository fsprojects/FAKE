# FAKE Command Line

**Note:  This documentation is for FAKE.exe version 5.0 or later. The old documentation can be found [here](legacy-commandline.html)**


The FAKE.exe command line interface (CLI) is defined as follows:`

```
USAGE: fake [--help] [--version] [--verbose] [<subcommand> [<options>]]

SUBCOMMANDS:

    run <options>         Runs a script.
    build <options>       Build a target via build.fsx script.

    Use 'fake <subcommand> --help' for additional information.

OPTIONS:

    --version             Prints the version.
    --verbose, -v         More verbose output. Can be used more than once.
    --help                display this list of options.
```

For now fake only supports the `run` and `build` subcommands which are basically equivalent to the Fake as you know it, but more are planned in the future.

## `--verbose [-v]`

Print details of FAKE's activity. Note that `-v` was used for `--version` in previous versions of Fake.
Currently Fake supports 3 verbosity levels:

* None is warnings only and regular output from the script
* a single `--verbose` means verbose output from Fake
* two `--verbose --verbose` mean to set other projects (like paket) to verbose mode as well.

### `--version`

Print FAKE version information.

### `--help`

Prints help information. In contract to the other options you can use --help everywhere.
For example `fake run --help` to get help about the `run` subcommand.

## Subcommands

### Run

```
USAGE: fake run [--help] [--script <string>] [--target <string>]
                [--environmentvariable <string> <string>] [--debug]
                [--singletarget] [--nocache] [--fsiargs <string>]

OPTIONS:

    --script, -f <string> Specify the script to run. (--script or -f is
                          optional)
    --target, -t <string> The target to run.
    --environmentvariable, -e <string> <string>
                          Set an environment variable.
    --debug, -d           Debug the script (set a breakpoint at the start).
    --singletarget, -s    Run only the specified target.
    --nocache, -n         Disable caching of the compiled script.
    --fsiargs <string>    Arguments passed to the f# interactive.
    --help                display this list of options.
```

The run command is basically to start scripts or build-scripts therefore the `--script` is optional and you can just write `fake run build.fsx`.

#### Basic examples

**Specify build script only:** `fake.exe run mybuildscript.fsx`

**Specify target name only:** `fake.exe run build.fsx --target Clean` (runs the `Clean` target).

#### `script`

Required. The path to your `.fsx` build file. Note the `--script` is optional, you can use it if you have specially crafted file names like `--debug`.

#### `target`

Optional.  The name of the build script target you wish to run.  This will any target you specified to run in the build script.  

#### `--environmentvariable [-e] <name:string> <value:string>`

Set environment variable name value pair. Supports multiple.

#### `--fsiargs <string>`

Pass an single argument after this switch to FSI when running the build script.  See [F# Interactive Options](http://msdn.microsoft.com/en-us/library/dd233172.aspx) for the fsi CLI details.

#### `--help [-h|/h|/help|/?]`

Display CLI help.

### Build

```
USAGE: fake build [--help] [--target <string>] [--script <string>]
                  [--environmentvariable <string> <string>] [--debug]
                  [--singletarget] [--nocache] [--fsiargs <string>]

OPTIONS:

    --target, -t <string> The target to run (--target or -t is optional when
                          running 'build').
    --script, -f <string> Specify the script to run.
    --environmentvariable, -e <string> <string>
                          Set an environment variable.
    --debug, -d           Debug the script (set a breakpoint at the start).
    --singletarget, -s    Run only the specified target.
    --nocache, -n         Disable caching of the compiled script.
    --fsiargs <string>    Arguments passed to the f# interactive.
    --help                display this list of options.
```

Very similar to `run`. The difference is that with `build` the `--target` is optional and `build.fsx` is assumed where in `run` the `--script` is optional.
Examples:

* `fake build` to run the default target and `build.fsx`
* `fake build Build -s` to run the `Build` target without dependencies
* `fake build Build` to run the `Build` target with all dependencies

The recommendation is to use `fake build` if you have a single `build.fsx` and `fake run` for scripting (or multiple `.fsx` files). 

# Running FAKE targets from the command line

For this short sample we assume you have the latest version of FAKE installed and available in PATH (see [the getting started guide](gettingstarted.html)). Now consider the following small FAKE script:

```fsharp
#r "paket:
nuget Fake.Core.Target //"
open Fake.Core

Target.Create "Clean" (fun _ -> Trace.trace " --- Cleaning stuff --- ")

Target.Create "Build" (fun _ -> Trace.trace " --- Building the app --- ")

Target.Create "Deploy" (fun _ -> Trace.trace " --- Deploying app --- ")

open Fake.Core.TargetOperators
"Clean"
    ==> "Build"
    ==> "Deploy"

Target.RunOrDefault "Deploy"
```

Now you can just execute

* `fake run build.fsx` to run the default target (`Deploy`)
* `fake build` same as above
* `fake run build.fsx -s -t Build` to run the `Build` target without dependencies
* `fake build Build -s` same as above
* `fake run build.fsx -t Build` to run the `Build` target with the `Clean` dependency
* `fake build Build` same as above
