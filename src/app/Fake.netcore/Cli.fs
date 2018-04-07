/// New Command line interface for FAKE that utilises Argu.
[<RequireQualifiedAccessAttribute>]
module Cli

open System
open Fake.Core.CommandLineParsing

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
  --fsiargs <fsiargs> [*]  Arguments passed to the f# interactive.

Fake Build Options [build_opts]:
  -d, --debug           Debug the script.
  -n, --nocache         Disable fake cache for this run.
  --fsiargs <fsiargs> [*]  Arguments passed to the f# interactive.
  -f, --script <script.fsx>
                        The script to execute (defaults to `build.fsx`).
  """
