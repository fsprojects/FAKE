# FAKE Command Line

The `fake.exe` command line interface (CLI) is defined as follows:
```bash
Usage:
fake.exe [fake_opts] run [run_opts] [<script.fsx>] [--] [<scriptargs>...]
fake.exe [fake_opts] build [build_opts] [--] [<scriptargs>...]
fake.exe --version
fake.exe --help | -h

Fake Options [fake_opts]:
-v, --verbose [*]     Verbose (can be used multiple times)
                        Is ignored if -s is used.
                        * -v: Log verbose but only for FAKE
                        * -vv: Log verbose for Paket as well
-s, --silent          Be silent, use this option if you need to pipe your output into another tool or need some additional processing.

Fake Run Options [run_opts]:
-d, --debug           Debug the script.
-n, --nocache         Disable fake cache for this run.
-p, --partial-restore
                      Only restore the required group instead of a full restore,
                      can be set globally by setting the environment variable FAKE_PARTIAL_RESTORE to true.
--fsiargs <fsiargs> [*]  Arguments passed to the f# interactive.

Fake Build Options [build_opts]:
-d, --debug           Debug the script.
-n, --nocache         Disable fake cache for this run.
-p, --partial-restore
                      Only restore the required group instead of a full restore,
                      can be set globally by setting the environment variable FAKE_PARTIAL_RESTORE to true.
--fsiargs <fsiargs> [*]  Arguments passed to the f# interactive.
-f, --script <script.fsx>
                        The script to execute (defaults to `build.fsx`).
```
Please refer to the [Fake.Core.CommandLineParsing](core-commandlineparsing.html) documentation for a explanation of the synax.

For now fake only supports the `run` and `build` subcommands which are basically equivalent to the Fake as you know it, but more are planned in the future. In general you should use the `run` subcommand in scripting when you use parameters, because it is safer in regards to adding options without breaking. Use `build` to have a more dense workflow in the command line

## Disclaimer

Ordering of arguments does MATTER. This is a change in behavior from FAKE version 4 to 5.

Examples:

- `fake run -v build.fsx` - This will not run fake in verbose mode. Instead it will try to run the script named `-v`. (But we might support that in the future)
- `fake run build.fsx --fsiargs "--define:BOOTSTRAP"` - This will not run `build.fsx` and define BOOTSTRAP, because `--fsiargs` needs to be before the script-name.
- `fake build -s` - This will run fake in single-target mode and not in silent mode, you need to use `fake -s build` as described in the above usage description.

## `--verbose [-v]`

Print details of FAKE's activity. Note that `-v` was used for `--version` in previous versions of Fake.
Currently Fake supports 4 verbosity levels:

- a single `--silent` will prevent all output from the fake runner. This makes it easy to use a `.fsx` script for data processing or pipelining on the command-line
- None is regular fake information like performance-numbers, general informations and warnings as well as regular output from the script
- a single `--verbose` means verbose output from the fake runner
- two `--verbose --verbose` or `-vv` mean to set other projects (like paket) to verbose mode as well.

### `--version`

Prints FAKE version information.

### `--help`

Prints help information. In contract to the other options you can use --help everywhere.
For example `fake run --help` to get help about the `run` subcommand.

## Basic examples

**Specify build script only:** `fake.exe run mybuildscript.fsx`

**Specify target name only:** `fake.exe run build.fsx --target Clean` (runs the `Clean` target).

As `fake build` is a shortcut you could use:

**Specify target name only:** `fake.exe build -t Clean` (runs the `Clean` target).

### `<script.fsx>` or `--script <script.fsx>`

Required. The path to your `.fsx` build file. Note that for `fake run` the first "unknown" argument as parsed as the script name and all other parameters are interpreted as arguments for the script.

To support specially named files like `--fsiargs` you could use `fake build --script --fsiargs`

### `--fsiargs <fsiargs>`

Pass an single argument after this switch to FSI when running the build script.  See [F# Interactive Options](http://msdn.microsoft.com/en-us/library/dd233172.aspx) for the fsi CLI details.

This way you can use for example `#if MYFLAG` compiler directives in your script and use `--fsiargs --define:MYFLAG`

### `--help` or `-h`

Display CLI help.

## Running Targets

Please refer to the [Fake.Core.Target](core-targets.html) module documentation

For reference the CLI for the targets-module looks like this:

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
                          Set an environment variable. Use 'key=val'
    -s, --single-target    Run only the specified target.
    -p, --parallel <num>  Run parallel with the given number of tasks.
```

Basically this means you insert the options as `<scriptargs>` parameters at the end.
