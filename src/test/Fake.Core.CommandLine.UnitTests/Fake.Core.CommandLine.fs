
module Fake.Core.CommandLineParsingTests

open System

open Fake.Core
open Fake.Core.CommandLineParsing
open System
open System.Diagnostics
open Expecto
open Expecto.Flip

let debug = true

// HELPER FUNCTIONS FOR TESTCASE-CREATION
type TestCaseHelper =
  static member Create(test':string, usage':string, [<ParamArray>]statements':(Docopt -> string * bool)[]) =
    testCase ("Test command-line parsing: " + test') <| fun _ ->
      printfn "Starting test '%s'" test'
      try
        if debug then
          let mutable doc = Docopt(usage')
          printf "%s\n{" test'
          Console.WriteLine(usage')
          //printfn "  Asts: %A" doc.UsageParser.Asts
        Array.iter (fun assertion' -> let doc = Docopt(usage')
                                      let msg, res = assertion' doc
                                      if debug then printfn "    %s . . . %A" msg res
                                      Expect.isTrue msg res) statements'
        if debug then Console.WriteLine("}\n")
      with e -> printfn ">>> ERROR: %A" e;
                reraise ()
let ( ->= ) (argv':string) val' (doc':Docopt) =
  let argv = argv'.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
  let args = doc'.Parse(argv) |> Seq.map (fun kv -> kv.Key, kv.Value) |> Seq.toList |> List.sort
  let expt = List.sort val'
  let res = args = expt
  if not res then
    printfn "Got args = %A" args
  (sprintf "%A ->= %A" argv' expt), res
let ( ->! ) (argv':string) val' (doc':Docopt) =
  let argv = argv'.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
  
  let msg, res =
    try let args = doc'.Parse(argv) |> Seq.map (fun kv -> kv.Key, kv.Value) |> Seq.toList |> List.sort 
        printfn "Got args = %A" args
        (box "<NO EXN>", false)
    with e -> (box e, e.GetType() = val')
  (sprintf "%A ->! %A" argv' msg), res
// END HELPER FUNCTIONS FOR ASSERTIONS

[<Tests>]
let tests = 
  testList "Fake.Core.CommandLineParsing.Tests" [

    testCase ("ArgumentPosition -> Next") <| fun _ ->
      let pos = ArgumentStreamPosition.ShortArgumentPartialPos(0, 1)
      
      pos.NextArg [|"-a"; "-r"; "-m"; "Hello"|]
      |> Expect.equal "Expected ArgumentPos to work" (ArgumentStreamPosition.ShortArgumentPartialPos(1, 1))
      pos.Next [|"-a"; "-r"; "-m"; "Hello"|]
      |> Expect.equal "Expected ArgumentPos to work" (ArgumentStreamPosition.ShortArgumentPartialPos(1, 1))

    TestCaseHelper.Create("Split Arguments should not be observable", """
Usage:
  test.exe -- [<moreargs>...]

Options:
    """,
      "-- -ald" ->= ["<moreargs>", Argument "-ald";"--", Flag]
    )

    TestCaseHelper.Create("Do not allow flags from another context", """
Usage:
  test.exe [fake_opts] run [run_opts] [--]

Fake Options [fake_opts]:
  -v, --verbose [*]     Verbose (can be used multiple times)
                        Is ignored if -s is used.
                        * -v: Log verbose but only for FAKE
                        * -vv: Log verbose for Paket as well

Fake Run Options [run_opts]:
  -d, --debug           Debug the script.
    """,
      "run -v" ->! typeof<DocoptException>,
      "run" ->= ["run", Flag],
      "run -d" ->= ["run", Flag;"-d", Flag;"--debug", Flag],
      "-v run -d" ->= ["--verbose", Flag; "-v", Flag; "run", Flag;"-d", Flag;"--debug", Flag],
      "-v run" ->= ["--verbose", Flag; "-v", Flag; "run", Flag]
      
    )

    TestCaseHelper.Create("FAKE 5 tests", """
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
  --fsiargs <args> [*]  Arguments passed to the f# interactive.

Fake Build Options [build_opts]:
  -d, --debug           Debug the script.
  -n, --nocache         Disable fake cache for this run.
  --fsiargs <args> [*]  Arguments passed to the f# interactive.
  -f, --script <script.fsx>
                        The script to execute (defaults to `build.fsx`).
    """,
      "run --fsiargs --define:BOOTSTRAP testbuild.fsx --target PrintColors"
        ->= ["run", Flag;"--fsiargs", Argument "--define:BOOTSTRAP";"<script.fsx>", Argument "testbuild.fsx"; "<scriptargs>", Arguments ["--target"; "PrintColors"]],
      "-vv run testbuild.fsx --fsiargs --define:BOOTSTRAP --target PrintColors"
        ->= ["-v", Flags 2; "--verbose", Flags 2;"run", Flag;"<script.fsx>", Argument "testbuild.fsx"; "<scriptargs>", Arguments ["--fsiargs";"--define:BOOTSTRAP";"--target"; "PrintColors"]]
    )
    testCase ("Test option section parser fake-run (targets)") <| fun _ ->
      printfn "Starting test '%s'" "Test option section parser fake-run (targets)"
      let testString = """
Usage:
  fake-run --list
  fake-run --version
  fake-run --help | -h
  fake-run [target_opts] [target <target>] [--] [<targetargs>...]

Target Module Options [target_opts]:
    -t, --target <target>
                          Run the given target (ignored if positional argument 'target' is given)
    -e, --environmentvariable <keyval> [*]
                          Set an environment variable. Use 'key=val'
    -s, --singletarget    Run only the specified target.
    -p, --parallel <num>  Run parallel with the given number of tasks.
          """
      let usage, optSections = DocHelper.cut testString

      let titles = optSections |> Seq.map (fun s -> s.Title) |> Seq.toList
      Expect.equal "Titles" ["target_opts"] titles

      let sectionsParsers =
        optSections
        |> Seq.map (fun oStrs -> oStrs.Title, SafeOptions(OptionsParser("?").Parse(oStrs.Lines)))
        |> dict
      let section = sectionsParsers.["target_opts"]
      let envVar = section.Find('e')
      Expect.isSome "Expected to find -e" envVar
      Expect.isTrue "Expected to have allowMultiple" envVar.Value.AllowMultiple
      Expect.isFalse "Expected to be not required" envVar.Value.IsRequired


    testCase ("Test option section parser fake") <| fun _ ->
      printfn "Starting test '%s'" "Test option section parser fake"
      let testString = """
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
  --fsiargs <args> [*]  Arguments passed to the f# interactive.

Fake Build Options [build_opts]:
  -d, --debug           Debug the script.
  -n, --nocache         Disable fake cache for this run.
  --fsiargs <args> [*]  Arguments passed to the f# interactive.
  -f, --script <script.fsx>
                        The script to execute (defaults to `build.fsx`).
          """
      let usage, optSections = DocHelper.cut testString

      let titles = optSections |> Seq.map (fun s -> s.Title) |> Seq.toList
      Expect.equal "Titles" ["fake_opts";"run_opts";"build_opts"] titles

      let sectionsParsers =
        optSections
        |> Seq.map (fun oStrs -> oStrs.Title, SafeOptions(OptionsParser("?").Parse(oStrs.Lines)))
        |> dict
      let section = sectionsParsers.["fake_opts"]
      let verbose = section.Find('v')
      Expect.isSome "Expected to find -v" verbose
      Expect.isTrue "Expected to have allowMultiple" verbose.Value.AllowMultiple
      Expect.isFalse "Expected to be not required" verbose.Value.IsRequired
      Expect.isFalse "Expected to have no argument" verbose.Value.HasArgument
      let section = sectionsParsers.["run_opts"]
      let fsiArgs = section.Find("fsiargs")
      Expect.isSome "Expected to find --fsiargs" fsiArgs
      Expect.isTrue "Expected to have allowMultiple" fsiArgs.Value.AllowMultiple
      Expect.isFalse "Expected to be not required" fsiArgs.Value.IsRequired
      Expect.isTrue "Expected to have argument" fsiArgs.Value.HasArgument

    TestCaseHelper.Create("FAKE 5 target CLI tests", """
Usage:
  fake-run --list
  fake-run --version
  fake-run --help | -h
  fake-run [target_opts] [target <target>] [--] [<targetargs>...]

Target Module Options [target_opts]:
    -t, --target <target>
                          Run the given target (ignored if positional argument 'target' is given)
    -e, --environmentvariable <keyval> [*]
                          Set an environment variable. Use 'key=val'
    -s, --singletarget    Run only the specified target.
    -p, --parallel <num>  Run parallel with the given number of tasks.
    """,
      "--target PrintColors"
        ->= ["--target", Argument "PrintColors"; "-t", Argument "PrintColors"],
      "--target PrintColors --help"
        ->= ["--target", Argument "PrintColors"; "-t", Argument "PrintColors"; "<targetargs>", Argument "--help"],
      "target PrintColors"
        ->= ["target", Flag; "<target>", Argument "PrintColors"],
      "-e key=val"
        ->= ["-e", Argument "key=val"; "--environmentvariable", Argument "key=val"],
      "-e key=val -e key2=val2"
        ->= ["-e", Arguments ["key=val"; "key2=val2"]; "--environmentvariable", Arguments ["key=val"; "key2=val2"]],
      "-e key=val -e key2=val2 --test"
        ->= ["-e", Arguments ["key=val"; "key2=val2"]; "--environmentvariable", Arguments ["key=val"; "key2=val2"]; "<targetargs>", Argument "--test"]
    )

    testCase ("Test Option parser") <| fun _ ->
      printfn "Starting test '%s'" "Test Option parser"
      let testString = """
Usage:
  fake.exe [fake_opts] run [run_opts] [<script.fsx>] [run_opts] -- [<scriptargs>...]
  fake.exe [fake_opts] build [build_opts] -- [<scriptargs>...]
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
  --fsiargs <args>      Arguments passed to the f# interactive.

Fake Build Options [build_opts]:
  -include [run_opts]   Includes all options from fake run
  -f <script.fsx>, --script <script.fsx>
                        The script to execute (defaults to `build.fsx`).
          """
      let usage, optSections = DocHelper.cut testString

      let titles = optSections |> Seq.map (fun s -> s.Title) |> Seq.toList
      Expect.equal "Titles" ["fake_opts"; "run_opts"; "build_opts"] titles

      let sectionsParsers =
        optSections
        |> Seq.map (fun oStrs -> oStrs.Title, SafeOptions(OptionsParser("?").Parse(oStrs.Lines)))
        |> dict

      // TODO implement&test '-include'
      ()

    TestCaseHelper.Create("FAKE 5 tests (twice run_opts)", """
Usage:
  fake.exe [fake_opts] run [run_opts] [<script.fsx>] [run_opts] [--] [<scriptargs>...]
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
  --fsiargs <args> [*]  Arguments passed to the f# interactive.

Fake Build Options [build_opts]:
  -d, --debug           Debug the script.
  -n, --nocache         Disable fake cache for this run.
  --fsiargs <args> [*]  Arguments passed to the f# interactive.
  -f, --script <script.fsx>
                        The script to execute (defaults to `build.fsx`).
    """,
      "run --fsiargs --define:BOOTSTRAP testbuild.fsx --target PrintColors"
        ->= ["run", Flag;"--fsiargs", Argument "--define:BOOTSTRAP";"<script.fsx>", Argument "testbuild.fsx"; "<scriptargs>", Arguments ["--target"; "PrintColors"]],
      "run testbuild.fsx --fsiargs --define:BOOTSTRAP --target PrintColors"
        ->= ["run", Flag;"--fsiargs", Argument "--define:BOOTSTRAP";"<script.fsx>", Argument "testbuild.fsx"; "<scriptargs>", Arguments ["--target"; "PrintColors"]]
    )


    TestCaseHelper.Create("FAKE 5 usage (simple)", """
Usage:
  fake.exe file -- [<moreargs>...]

Options:
    """,
      "file -- wtf" ->= ["file", Flag;"<moreargs>", Argument "wtf";"--", Flag],
      "file -- --wtf --test" ->= ["--", Flag;"file", Flag;"<moreargs>", Arguments["--wtf"; "--test"]]
    )

//    TestCaseHelper.Create("Paket use-cases", """
//Usage:
//  paket.exe [global_opts] show-installed-packages [show_installed_opts]
//  paket.exe [global_opts] show-groups
//  paket.exe [global_opts] find-packages [find_opts] <nuget> [find_opts]
//  paket.exe [global_opts] find-package-versions [find_opts] <nuget> [find_opts]
//  paket.exe [global_opts] pack [pack_opts] <package> [pack_opts]
//  paket.exe [global_opts] push [push_opts] <package> [push_opts]
//  paket.exe [global_opts] generate-include-scripts [loadscript_opts]
//  paket.exe [global_opts] generate-load-scripts [loadscript_opts]
//  paket.exe [global_opts] why [why_opts] <nuget> [why_opts]
//  paket.exe [global_opts] restriction <restriction>
//
//
//Global Options [global_opts]:
//  --from-bootstrapper          mark the command to be called from the bootstrapper
//  -s, --silent                 suppress console output
//  -v, --verbose                print detailed information to the console
//  --log-file                   print output to a file
//
//Why Options [why_opts]:
//  -g, --group <group>  specify dependency group [default: Main]
//  --details                    display detailed information with all paths, versions and framework restrictions
//
//GenerateLoadScript Options [loadscript_opts]:
//  -g, --group <group> [*]      groups to generate scripts for (default: all groups); may be repeated
//  -f, --framework <fw> [*]     framework identifier to generate scripts for, such as net45 or netstandard1.6; may be repeated
//  -f, --type <lang> [*]        language to generate scripts for; may be repeated
//
//
//Push Options [push_opts]:
//  --url <url>                  URL of the NuGet feed
//  --api-key <key>              API key for the URL (default: value of the NUGET_KEY environment variable)
//  --endpoint <endpoint>        API endpoint to push to [default: /api/v2/package]
//
//
//Pack Options [pack_opts]:
//  --build-config <configuration>
//                        build configuration that should be packaged (default: Release)
//  --build-platform <platform>
//                        build platform that should be packaged (default: check all known platform targets)
//  --version <version>   version of the package
//  --template <path>     pack a single paket.template file
//  --exclude <package ID>
//                        exclude paket.template file by package ID; may be repeated
//  --specific-version <package ID> <version>
//                        version number to use for package ID; may be repeated
//  --release-notes <text>
//                        release notes
//  --lock-dependencies   use version constraints from paket.lock instead of paket.dependencies
//  --minimum-from-lock-file
//                        use version constraints from paket.lock instead of paket.dependencies and add them as a minimum version; --lock-dependencies overrides this option
//  --pin-project-references
//                        pin dependencies generated from project references to exact versions (=) instead of using minimum versions (>=); with --lock-dependencies project references will be pinned even if this
//                        option is not specified
//  --symbols             create symbol and source packages in addition to library and content packages
//  --include-referenced-projects
//                        include symbols and source from referenced projects
//  --project-url <URL>   homepage URL for the package
//
//Pack Options [find_opts]:
//  --source <source URL> 
//                        specify source URL
//  --max <int>           limit maximum number of results
//
//Show installed packages [show_installed_opts]:
//  --all, -a             include transitive dependencies
//  --project, -p <path>  specify project to show dependencies for
// 
//    """,
//      "file -- wtf" ->= ["file", Flag;"<moreargs>", Argument "wtf";"--", Flag],
//      "file -- --wtf --test" ->= ["--", Flag;"file", Flag;"<moreargs>", Arguments["--wtf"; "--test"]]
//    )


    TestCaseHelper.Create("Empty usage", """
Usage: prog

""",
      ""      ->= [],
      "--xxx" ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Basic short option", """
Usage: prog [options]

Options: -a  All.

""",
      ""   ->= [],
      "-a" ->= [("-a", Flag)],
      "-x" ->! typeof<DocoptException>
    )
    TestCaseHelper.Create("Basic long option", """
Usage: prog [options]

Options: --all  All.

""",
      ""      ->= [],
      "--all" ->= [("--all", Flag)],
      "--xxx" ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Synonymous short and long option, with truncation", """
Usage: prog [options]

Options: -v, --verbose  Verbose.

""",
      "--verbose" ->= [("-v", Flag);("--verbose", Flag)],
      // Not supported
      //"--ver"     ->= [("-v", Flag);("--verbose", Flag)],
      "-v"        ->= [("-v", Flag);("--verbose", Flag)]
    )

    TestCaseHelper.Create("Short option with argument", """
Usage: prog [options]

Options: -p PATH

""",
      "-p home/" ->= [("-p", Argument("home/"))],
      "-phome/"  ->= [("-p", Argument("home/"))],
      "-p"       ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Long option with argument", """
Usage: prog [options]

Options: --path <path>

""",
      "--path home/" ->= [("--path", Argument("home/"))],
      "--path=home/" ->= [("--path", Argument("home/"))],
      // Not supported
      //"--pa home/"   ->= [("--path", Argument("home/"))],
      //"--pa=home/"   ->= [("--path", Argument("home/"))],
      "--path"       ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Synonymous short and long option with both arguments declared", """
Usage: prog [options]

Options: -p PATH, --path=<path>  Path to files.

""",
      "-proot" ->= [("-p", Argument("root"));("--path", Argument("root"))]
    )

    TestCaseHelper.Create("Synonymous short and long option with one argument declared", """
Usage: prog [options]

Options:    -p --path PATH  Path to files.

""",
      "-p root"     ->= [("-p", Argument("root"));("--path", Argument("root"))],
      "--path root" ->= [("-p", Argument("root"));("--path", Argument("root"))]
    )

    TestCaseHelper.Create("Short option with default", """
Usage: prog [options]

Options:
 -p PATH  Path to files [default: ./]

""",
      ""       ->= [("-p", Argument("./"))],
      "-phome" ->= [("-p", Argument("home"))]
    )

    TestCaseHelper.Create("Unusual formatting", """
UsAgE: prog [options]

OpTiOnS: --path=<files>  Path to files
                [dEfAuLt: /root]

""",
      ""            ->= [("--path", Argument("/root"))],
      "--path=home" ->= [("--path", Argument("home"))]
    )

    TestCaseHelper.Create("Multiple short options", """
usage: prog [options]

options:
    -a        Add
    -r        Remote
    -m <msg>  Message

""",
      "-a -r -m Hello" ->= [("-a", Flag);("-r", Flag);("-m", Argument("Hello"))],
      "-armyourass"    ->= [("-a", Flag);("-r", Flag);("-m", Argument("yourass"))],
      "-a -r"          ->= [("-a", Flag);("-r", Flag)]
    )

// Not supported
//    TestCaseHelper.Create("Truncated long option disambiguation", """
//Usage: prog [options]
//
//Options: --version
//         --verbose
//
//""",
//      "--version" ->= [("--version", Flag)],
//      "--verbose" ->= [("--verbose", Flag)],
//      "--ver"     ->! typeof<DocoptException>,
//      "--verb"    ->= [("--verbose", Flag)]
//    )

    TestCaseHelper.Create("Short options in square brackets", """
usage: prog [-a -r -m <msg>]

options:
 -a        Add
 -r        Remote
 -m <msg>  Message

""",
      "-armyourass" ->= [("-a", Flag);("-r", Flag);("-m", Argument("yourass"))]
    )

    TestCaseHelper.Create("Short option pack in square brackets", """
usage: prog [-armmsg]

options: -a        Add
         -r        Remote
         -m <msg>  Message

""",
      "-a -r -m Hello" ->= [("-a", Flag);("-r", Flag);("-m", Argument("Hello"))]
    )

    TestCaseHelper.Create("Required short options", """
usage: prog -a -b

options:
 -a
 -b

""",
      "-a -b" ->= [("-a", Flag);("-b", Flag)],
      "-b -a" ->= [("-a", Flag);("-b", Flag)],
      "-a"    ->! typeof<DocoptException>,
      ""      ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Required short options in brackets", """
usage: prog (-a -b)

options: -a
         -b

""",
      "-a -b" ->= [("-a", Flag);("-b", Flag)],
      "-b -a" ->= [("-a", Flag);("-b", Flag)],
      "-a"    ->! typeof<DocoptException>,
      ""      ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Two options, one is optional","""
usage: prog [-a] -b

options: -a
 -b

""",
      "-a -b" ->= [("-a", Flag);("-b", Flag)],
      // !!!!!!!!!!!!! Different from SPEC! 
      // order matters!
      //"-b -a" ->= [("-a", Flag);("-b", Flag)],
      "-b -a" ->! typeof<DocoptException>,
      "-a"    ->! typeof<DocoptException>,
      "-b"    ->= [("-b", Flag)],
      ""      ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Required in optional", """
usage: prog [(-a -b)]

options: -a
         -b

""",
      "-a -b" ->= [("-a", Flag);("-b", Flag)],
      "-b -a" ->= [("-a", Flag);("-b", Flag)],
      "-a"    ->! typeof<DocoptException>,
      "-b"    ->! typeof<DocoptException>,
      ""      ->= []
    )

    TestCaseHelper.Create("Exclusive or", """
usage: prog (-a|-b)

options: -a
         -b

""",
      "-a -b" ->! typeof<DocoptException>,
      ""      ->! typeof<DocoptException>,
      "-a"    ->= [("-a", Flag)],
      "-b"    ->= [("-b", Flag)]
    )

    TestCaseHelper.Create("Optional exclusive or", """
usage: prog [ -a | -b ]

options: -a
         -b

""",
      "-a -b" ->! typeof<DocoptException>,
      ""      ->= [],
      "-a"    ->= [("-a", Flag)],
      "-b"    ->= [("-b", Flag)]
    )

    TestCaseHelper.Create("Argument", """
usage: prog <arg>""",
      "10"    ->= [("<arg>", Argument("10"))],
      "10 20" ->! typeof<DocoptException>,
      ""      ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Optional argument", """
usage: prog [<arg>]""",
      "10"    ->= [("<arg>", Argument("10"))],
      "10 20" ->! typeof<DocoptException>,
      ""      ->= []
    )

    TestCaseHelper.Create("Multiple arguments", """
usage: prog <kind> <name> <type>""",
      "10 20 40" ->= [("<kind>", Argument("10"));("<name>", Argument("20"));("<type>", Argument("40"))],
      "10 20"    ->! typeof<DocoptException>,
      ""         ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Multiple arguments, two optional", """
usage: prog <kind> [<name> <type>]""",
      "10 20 40" ->= [("<kind>", Argument("10"));("<name>", Argument("20"));("<type>", Argument("40"))],
      
      // !!!!!!!!!!!!! Different from SPEC! 
      // "10 20"    ->= [("<kind>", Argument("10"));("<name>", Argument("20"))],
      "10 20"    ->! typeof<DocoptException>,
      ""         ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Multiple arguments xor'd in optional", """
usage: prog [<kind> | <name> <type>]""",
      "10 20 40" ->! typeof<DocoptException>,
      "20 40"    ->= [("<name>", Argument("20"));("<type>", Argument("40"))],
      ""         ->= []
    )

    TestCaseHelper.Create("Mixed xor, arguments and options", """
usage: prog (<kind> --all | <name>)

options:
 --all

""",
      "10 --all" ->= [("--all", Flag);("<kind>", Argument("10"))],
      "10"       ->= [("<name>", Argument("10"))],
      ""         ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Stacked argument", """
usage: prog [<name> <name>]""",
      "10 20" ->= [("<name>", Arguments(["10";"20"]))],
      
      // !!!!!!!!!!!!! Different from SPEC! 
      // Both needs to be given or none (different from [<name>] [<name>])
      //"10"    ->= [("<name>", Argument("10"))],
      "10"    ->! typeof<DocoptException>,
      ""      ->= []
    )

    TestCaseHelper.Create("Stacked argument (2)", """
usage: prog [<name>] [<name>]""",
      "10 20" ->= [("<name>", Arguments(["10";"20"]))],
      "10"    ->= [("<name>", Argument("10"))],
      ""      ->= []
    )

    TestCaseHelper.Create("Same, but both arguments must be present", """
usage: prog [(<name> <name>)]""",
      "10 20" ->= [("<name>", Arguments(["10";"20"]))],
      "10"    ->! typeof<DocoptException>,
      ""      ->= []
    )

    TestCaseHelper.Create("Ellipsis (one or more (also, ALL-CAPS argument name))", """
usage: prog NAME...""",
      "10 20" ->= [("NAME", Arguments(["10";"20"]))],
      "10"    ->= [("NAME", Argument("10"))],
      ""      ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Optional in ellipsis", """
usage: prog [NAME]...""",
      "10 20" ->= [("NAME", Arguments(["10";"20"]))],
      "10"    ->= [("NAME", Argument("10"))],
      ""      ->= []
    )

    TestCaseHelper.Create("Ellipsis in optional", """
usage: prog [NAME...]""",
      "10 20" ->= [("NAME", Arguments(["10";"20"]))],
      "10"    ->= [("NAME", Argument("10"))],
      ""      ->= []
    )

    TestCaseHelper.Create("multiple named arguments", """
usage: prog [NAME [NAME ...]]""",
      "10 20" ->= [("NAME", Arguments(["10";"20"]))],
      "10"    ->= [("NAME", Argument("10"))],
      ""      ->= []
    )

    TestCaseHelper.Create("Argument mismatch with option", """
usage: prog (NAME | --foo NAME)

options: --foo

""",
      "10"       ->= [("NAME", Argument("10"))],
      "--foo 10" ->= [("NAME", Argument("10"));("--foo", Flag)],
      
      // !!!!!!!!!!!!! Different from SPEC! 
      // We allow arguments that look like options when they are the only way to parse them
      //"--foo=10" ->! typeof<DocoptException>
      "--foo=10"       ->= [("NAME", Argument("--foo=10"))]

    )

    TestCaseHelper.Create("Multiple “options:” statements", """
usage: prog (NAME | --foo) [--bar | NAME]

options: --foo
options: --bar

""",
      "10"          ->= [("NAME", Argument("10"))],
      "10 20"       ->= [("NAME", Arguments(["10";"20"]))],
      // !!!!!!!!!!!!! Different from SPEC! 
      // For us ordering matters for parsing! (see new test below!)
      //"--foo --bar" ->= [("--foo", Flag);("--bar", Flag)]
      "--foo --bar" ->= [("NAME", Argument "--foo");("--bar", Flag)]
    )

    
    TestCaseHelper.Create("Multiple “options:” statements (2)", """
usage: prog (--foo | NAME) [--bar | NAME]

options: --foo
options: --bar

""",
      "10"          ->= [("NAME", Argument("10"))],
      "10 20"       ->= [("NAME", Arguments(["10";"20"]))],
      // see test above
      "--foo --bar" ->= [("--foo", Flag);("--bar", Flag)]
    )

    TestCaseHelper.Create("Big Bertha: command, multiple usage and --long=arg", """
Naval Fate.

Usage:
  prog ship new <name>...
  prog ship [<name>] move <x> <y> [--speed=<kn>]
  prog ship shoot <x> <y>
  prog mine (set|remove) <x> <y> [--moored|--drifting]
  prog -h | --help
  prog --version

Options:
  -h --help     Show this screen.
  --version     Show version.
  --speed=<kn>  Speed in knots [default: 10].
  --moored      Mored (anchored) mine.
  --drifting    Drifting mine.

""",
      "ship Guardian move 150 300 --speed=20" ->= [
        "--speed", Argument("20");
        "<name>", Argument("Guardian");
        "<x>", Argument("150");
        "<y>", Argument("300");
        "move", Flag;
        "ship", Flag;
      ]
    )

    TestCaseHelper.Create("No `options:` part 1", """
usage: prog --hello""",
      "--hello" ->= [("--hello", Flag)]
    )

    TestCaseHelper.Create("No `options:` part 2", """
usage: prog [--hello=<world>]""",
      ""             ->= [],
      "--hello wrld" ->= [("--hello", Argument("wrld"))]
    )

    TestCaseHelper.Create("No `options:` part 3", """
usage: prog [-o]""",
      "" ->= [],
      "-o" ->= [("-o", Flag)]
    )

    TestCaseHelper.Create("No `options:` part 4", """
usage: prog [-opr]""",
      "-op" ->= [("-o", Flag);("-p", Flag)]
    )

    TestCaseHelper.Create("1 flag", """
Usage: prog -v""",
      "-v" ->= [("-v", Flag)]
    )

    TestCaseHelper.Create("2 flags", """
Usage: prog [-v -v]""",
      ""    ->= [],
      "-v"  ->= [("-v", Flag)],
      "-vv" ->= [("-v", Flags(2))]
    )

    TestCaseHelper.Create("Many flags", """
Usage: prog -v ...""",
      ""        ->! typeof<DocoptException>,
      "-v"      ->= [("-v", Flag)],
      "-vv"     ->= [("-v", Flags(2))],
      "-vvvvvv" ->= [("-v", Flags(6))]
    )

    TestCaseHelper.Create("Many flags xor'd", """
Usage: prog [-v | -vv | -vvv]

This one is probably most readable user-friendly variant.

""",
      ""      ->= [],
      "-v"    ->= [("-v", Flag)],
      "-vv"   ->= [("-v", Flags(2))],
      "-vvvv" ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Counting long options", """
usage: prog [--ver --ver]""",
      "--ver --ver" ->= [("--ver", Flags(2))]
    )

    TestCaseHelper.Create("1 command", """
usage: prog [go]""",
      "go" ->= [("go", Flag)]
    )

    TestCaseHelper.Create("2 commands", """
usage: prog [go go]""",
      ""         ->= [],
      // !!!!!!!!!!!!! Different from SPEC! 
      // only group is optional!
      //"go"       ->= [("go", Flag)],
      "go" ->! typeof<DocoptException>,
      "go go"    ->= [("go", Flags(2))],
      "go go go" ->! typeof<DocoptException>
    )

    TestCaseHelper.Create("Many commands", """
usage: prog go...""",
      "go go go go go" ->= [("go", Flags(5))]
    )

    TestCaseHelper.Create("[options] does not include options from usage-pattern", """
usage: prog [options] [-a]

options: -a
         -b
""",
      "-a"  ->= [("-a", Flag)],
      // !!!!!!!!!!!! Different from SPEC!
      //"-aa" ->! typeof<DocoptException> // SPEC
      "-aa"  ->= [("-a", Flags 2)] // Not SPEC
    )

    TestCaseHelper.Create("[options] shortcut", """
Usage: prog [options] A
Options:
    -q  Be quiet
    -v  Be verbose.

""",
      "arg" ->= [("A", Argument("arg"))],
      "-v arg" ->= [("A", Argument("arg"));("-v", Flag)],
      "-q arg" ->= [("A", Argument("arg"));("-q", Flag)]
    )

    TestCaseHelper.Create("Single dash", """
usage: prog [-]""",
      "-" ->= [("-",  Flag)],
      ""  ->= []
    )


////
//// If argument is repeated, its value should always be a list
////
//
//let doc = Docopt("""usage: prog [NAME [NAME ...]]""")
//
//$ prog a b
//{"NAME": ["a", "b"]}
//
//$ prog
//{"NAME": []}
//
////
//// Option's argument defaults to null/None
////
//
//let doc = Docopt("""usage: prog [options]
//options:
// -a        Add
// -m <msg>  Message
//
//""")
//$ prog -a
//{"-m": null, "-a": true}

    TestCaseHelper.Create("Options without description (1)", """
usage: prog --hello""",
      "--hello" ->= [("--hello", Flag)]
    )

    TestCaseHelper.Create("Options without description (2)", """
usage: prog [--hello=<world>]""",
      ""             ->= [],
      "--hello wrld" ->= [("--hello", Argument("wrld"))]
    )

    TestCaseHelper.Create("Options without description (3)", """
usage: prog [-o]""",
      ""   ->= [],
      "-o" ->= [("-o", Flag)]
    )

    TestCaseHelper.Create("Options without description (4)", """
usage: prog [-opr]""",
      "-op" ->= [("-o", Flag);("-p", Flag)]
    )

    TestCaseHelper.Create("Options without description (5)", """
usage: git [-v | --verbose]""",
      "-v" ->= [("-v", Flag)]
    )

    TestCaseHelper.Create("Options without description (6)", """
usage: git remote [-v | --verbose]""",
      "remote -v" ->= [("remote", Flag);("-v", Flag)]
    )

    TestCaseHelper.Create("Empty usage pattern", """
usage: prog""",
      "" ->= []
    )

    TestCaseHelper.Create("Empty pattern then two arguments", """
usage: prog
       prog <a> <b>
""",
      "1 2" ->= [("<a>", Argument("1"));("<b>", Argument("2"))],
      ""    ->= []
    )

    TestCaseHelper.Create("Two arguments then empty pattern", """
usage: prog <a> <b>
       prog
""",
      "" ->= []
    )


    TestCaseHelper.Create("Option's argument should not capture default value from usage pattern (1)", """
usage: prog [--file=<f>]""",
      "" ->= []
    )

    TestCaseHelper.Create("Option's argument should not capture default value from usage pattern (2)", """
usage: prog [--file=<f>]

options: --file <a>

""",
      "" ->= []
    )

    TestCaseHelper.Create("Option's argument should not capture default value from usage pattern (3)", """
Usage: prog [-a <host:port>]

Options: -a, --address <host:port>  TCP address [default: localhost:6283].

""",
      "" ->= [("-a", Argument("localhost:6283"));("--address", Argument("localhost:6283"))]
    )

    ////
    //// If option with argument could be repeated,
    //// its arguments should be accumulated into a list
    ////
    //
    //let doc = Docopt("""usage: prog --long=<arg> ...""")
    //
    //$ prog --long one
    //{"--long": ["one"]}
    //
    //$ prog --long one --long two
    //{"--long": ["one", "two"]}


    TestCaseHelper.Create("multiple elements repeated at once", """
usage: prog (go <direction> --speed=<km/h>)...""",
      "go left --speed=5  go right --speed=9" ->= [
        "go", Flags(2);
        "<direction>", Arguments(["left"; "right"]);
        "--speed", Arguments(["5";"9"])
      ]
    )
      
    // !!!!!!!!!!!!! Different from SPEC! 
    // No, not supported
//    TestCaseHelper.Create("Required options should work with option shortcut", """
//usage: prog [options] -a
//
//options: -a
//
//""",
//      "-a" ->= [("-a", Flag)]
//    )

    ////
    //// If option could be repeated its defaults should be split into a list
    ////
    //
    //let doc = Docopt("""usage: prog [-o <o>]...
    //
    //options: -o <o>  [default: x]
    //
    //""")
    //$ prog -o this -o that
    //{"-o": ["this", "that"]}
    //
    //$ prog
    //{"-o": ["x"]}
    //
    //let doc = Docopt("""usage: prog [-o <o>]...
    //
    //options: -o <o>  [default: x y]
    //
    //""")
    //$ prog -o this
    //{"-o": ["this"]}
    //
    //$ prog
    //{"-o": ["x", "y"]}

    TestCaseHelper.Create("Test stacked option's argument", """
usage: prog -pPATH

options: -p PATH

""",
      "-pHOME" ->= [("-p", Argument("HOME"))]
    )

    TestCaseHelper.Create("Issue 56: Repeated mutually exclusive args give nested lists sometimes", """
Usage: foo (--xx=<x>|--yy=<y>)...""",
      "--xx=1 --yy=2" ->= [("--xx", Argument("1"));("--yy", Argument("2"))]
    )

    TestCaseHelper.Create("POSIXly correct indentation (1)", """
usage: prog [<input file>]""",
      "f.txt" ->= [("<input file>", Argument("f.txt"))]
    )

    TestCaseHelper.Create("POSIXly correct indentation (2)", """
usage: prog [--input=<file name>]...""",
      "--input a.txt --input=b.txt" ->= [("--input", Arguments(["a.txt";"b.txt"]))]
    )

    TestCaseHelper.Create("Issue 85: `[options]` shortcut with multiple subcommands", """
usage: prog good [options]
           prog fail [options]

options: --loglevel=N

""",
      "fail --loglevel 5" ->= [("--loglevel", Argument("5"));("fail", Flag)]
    )

    // Usage-section syntax

    TestCaseHelper.Create("Basic usage", """
usage:prog --foo""",
      "--foo" ->= [("--foo", Flag)]
    )

    TestCaseHelper.Create("Words before `usage:`", """
PROGRAM USAGE: prog --foo""",
      "--foo" ->= [("--foo", Flag)]
    )

    TestCaseHelper.Create("Words after 'usage' (1)", """
Usage: prog --foo
           prog --bar
NOT PART OF SECTION""",
      "--foo" ->= [("--foo", Flag)]
    )

    TestCaseHelper.Create("Words after 'usage' (2)", """
Usage:
 prog --foo
 prog --bar

NOT PART OF SECTION""",
      "--foo" ->= [("--foo", Flag)]
    )

    TestCaseHelper.Create("Words after 'usage' (3)", """
Usage:
 prog --foo
 prog --bar
NOT PART OF SECTION""",
      "--foo" ->= [("--foo", Flag)]
    )

    // Options-section syntax

    TestCaseHelper.Create("Options-section syntax", """
Usage: prog [options]

global options: --foo
local options: --baz
               --bar
other options:
 --egg
 --spam
-not-an-option-

""",
      "--baz --egg" ->= [("--baz", Flag);("--egg", Flag)]
    )
  ]