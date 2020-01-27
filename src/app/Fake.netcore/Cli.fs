/// New Command line interface for FAKE that utilises Argu.
[<RequireQualifiedAccessAttribute>]
module Cli

open System
open Fake.Core.CommandLineParsing

let fakeArgsHint =
  """
General:

  The Fake command line is divided into runtime and script arguments.
  Runtime arguments control compilation and processing of the script,
  while script arguments are specific for the script or provided by
  a NuGet package.
  In most use cases you use the "Fake.Core.Target"-Package and therefore
  inherit the corresponding command line interface. While these arguments
  are not strictly part of the runtime we still show both below to
  make it easier for newcomers.

 -- RUNTIME ARGUMENTS SECTION --

"""

let fakeUsage =
  """
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
"""

let fakeAdditionalHelp =
  """

 -- SCRIPT ARGUMENTS SECTION --

THIS SECTION ONLY APPLIES IF YOU USE THE 'Fake.Core.Target' PACKAGE!
You can use the following arguments in place of `<scriptargs>`:

Usage:
  fake-run --list
  fake-run --write-info <file>
  fake-run --version
  fake-run --help | -h
  fake-run [target_opts] [target <target>] [--] [<targetargs>...]

Target Module Options [target_opts]:
    -t, --target <target>
                          Run the given target (ignored if positional
argument 'target' is given)
    -e, --environment-variable <keyval> [*]
                          Set an environment variable. Use 'key=val'.
Consider using regular arguments, see https://fake.build/core-targets.html
    -s, --single-target    Run only the specified target.
    -p, --parallel <num>  Run parallel with the given number of tasks.

Example:

To use verbose mode (from [fake_opts]) and print all
targets use "fake -v build -- --list". Because "--list" 
doesn't conflict with any of the [build_opts], you can use 
"fake -v build --list"
"""
