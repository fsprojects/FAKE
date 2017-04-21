# FAKE Command Line

**Note:  This documentation is for FAKE.exe version 5.0 or later. The old documentation can be found [here](legacy_commandline.html)**


The FAKE.exe command line interface (CLI) is defined as follows:`

```
USAGE: fake [--help] [--version] [--verbose] [<subcommand> [<options>]]
SUBCOMMANDS:

    run <options>         Runs a build script.

    Use 'fake <subcommand> --help' for additional information.

OPTIONS:

    --version             Prints the version.
    --verbose, -v         More verbose output.
    --help                display this list of options.
```

For now fake only supports the `run` subcommand which is basically equivalent to the Fake as you know it, but more are planned in the future.

## `--verbose [-v]`

Print details of FAKE's activity. Note that `-v` was used for `--version` in previous versions of Fake.

### `--version`

Print FAKE version information.

### `--help`

Prints help information. In contract to the other options you can use --help everywhere.
For example `fake run --help` to get help about the `run` subcommand.

## Subcommands

### Run

```
USAGE: fake run [--help] [--script <string>] [--target <string>] [--environmentvariable <string> <string>] [--debug] [--singletarget] [--nocache] [--fsiargs <string>]
OPTIONS:

    --script <string>     Specify the script to run. (--script is optional)
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

**Specify target name only:** `fake.exe run build.fsx --target clean` (runs the `clean` target).

#### `script`

Required. The path to your `.fsx` build file. Note the `--script` is optional, you can use it if you have specially crafted file names like `--debug`.

#### `target`

Optional.  The name of the build script target you wish to run.  This will any target you specified to run in the build script.  

#### `--environmentvariable [-e] <name:string> <value:string>`

Set environment variable name value pair. Supports multiple. 
                                                   
### `--fsiargs <string>`

Pass an single argument after this switch to FSI when running the build script.  See [F# Interactive Options](http://msdn.microsoft.com/en-us/library/dd233172.aspx) for the fsi CLI details.

### `--help [-h|/h|/help|/?]`

Display CLI help.
                                                                                                         


# Running FAKE targets from the command line

For this short sample we assume you have the latest version of FAKE in *./tools/*. Now consider the following small FAKE script:

	#r "FAKE/tools/FakeLib.dll"
	open Fake 
 
	Target "Clean" (fun () ->  trace " --- Cleaning stuff --- ")
 
	Target "Build" (fun () ->  trace " --- Building the app --- ")
 
	Target "Deploy" (fun () -> trace " --- Deploying app --- ")
 
 
	"Clean"
	  ==> "Build"
	  ==> "Deploy"
 
	RunTargetOrDefault "Deploy"

If you are on windows then create this small redirect script:

	[lang=batchfile]
	@echo off
	"tools\Fake.exe" "%1"
	exit /b %errorlevel%

On mono you can use:

	[lang=batchfile]
	#!/bin/bash
    mono ./tools/FAKE.exe "$@"

Now you can run FAKE targets easily from the command line:

![alt text](pics/commandline/cmd.png "Running FAKE from cmd")
