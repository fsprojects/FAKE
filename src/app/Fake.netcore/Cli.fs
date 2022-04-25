/// New Command line interface for FAKE that utilises Argu.
[<RequireQualifiedAccessAttribute>]
module Cli

open System
open Fake.Core.CommandLineParsing

let fakeArgsHint =
  $"""
{Fake.Runtime.Environment.fakeVersionStr}

"""

let fakeUsage =
  """
Usage:
  fake [fake_opts] run [run_opts] [<script.fsx>] [--] [<scriptargs>...]
  fake [fake_opts] build [build_opts] [--] [<scriptargs>...]
  fake --version
  fake --help | -h

Note `fake build` is basically equivalent to calling `fake run` with a script named `build.fsx`.

Fake options [fake_opts]:
  -v, --verbose [*]        Verbose (can be used multiple times)
                           Is ignored if -s is used.
                           * -v: Log verbose but only for FAKE
                           * -vv: Log verbose for Paket as well
  -s, --silent             Be silent.
                           Use this option if you need to pipe your output into another tool or need some additional processing.

Fake Run options [run_opts]:
  -d, --debug              Debug the script.
  -n, --nocache            Disable Fake cache for this run.
  -p, --partial-restore
                           Only restore the required group instead of a full restore,
                           can be set globally by setting the environment variable FAKE_PARTIAL_RESTORE to true.
  --fsiargs <fsiargs> [*]  Arguments passed to the F# interactive.

Fake Build options [build_opts]:
  -d, --debug              Debug the script.
  -n, --nocache            Disable Fake cache for this run.
  -p, --partial-restore
                           Only restore the required group instead of a full restore,
                           can be set globally by setting the environment variable FAKE_PARTIAL_RESTORE to true.
  --fsiargs <fsiargs> [*]  Arguments passed to the F# interactive.
  -f, --script <script.fsx>
                           The script to execute (defaults to `build.fsx`).
"""

let fakeAdditionalHelp =
  """

 ----- SCRIPT ARGUMENTS SECTION -----

Remaining arguments following the previously defined options are provided to the script.

Each script might understand different arguments,
but since there are good chances you are using the 'Fake.Core.Target' package,
its command-line is documented below.

THIS SECTION ONLY APPLIES IF YOU USE THE 'Fake.Core.Target' PACKAGE IN YOUR SCRIPT!

You can use the following arguments in place of `<scriptargs>`:

(`fake-run` refers to the Fake command and arguments defined above)

Usage:
  fake-run --list
  fake-run --write-info <file>
  fake-run --version
  fake-run --help | -h
  fake-run [target_opts] [target <target>] [--] [<targetargs>...]

Target Module options [target_opts]:
    -t, --target <target>    Run the given target (ignored if a target is already provided with '[target <target>]')
    -e, --environment-variable <keyval> [*]
                             Set an environment variable. Use 'key=val'.
                             Consider using regular arguments, see https://fake.build/core-targets.html
    -s, --single-target      Run only the specified target.
    -p, --parallel <num>     Run parallel with the given number of tasks.

 ----- END OF SCRIPT ARGUMENTS SECTION -----

Warning:

Ordering of arguments does MATTER.
`fake -v run script.fsx` executes `script.fsx` in verbose mode.
`fake run -v script.fsx` will try to execute a script named '-v' and fail.

If a script argument/option conflicts with any of the options allowed before,
you need to separate script options with `--`.
The reverse is also true: to print all targets, you can use
`fake build --list` instead of `fake build -- --list`
because `--list` doesn't conflict with any of the [build_opts].

Basic examples:

Specify script file and execute default script action:
    fake run mybuildscript.fsx

Specify script file and run the Clean target:
    fake run build.fsx --target Clean
or shorter version using the default build.fsx script:
    fake build -t Clean
"""
