
module Fake.Core.CommandLineParsingTests

open System

open Docopt
open Docopt.Arguments
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
          printfn "  Asts: %A" doc.UsageParser.Asts
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
    try let _ = doc'.Parse(argv) |> Seq.map (fun kv -> kv.Key, kv.Value) |> Seq.toList in (box "<NO EXN>", false)
    with e -> (box e, e.GetType() = val')
  (sprintf "%A ->! %A" argv' msg), res
// END HELPER FUNCTIONS FOR ASSERTIONS

[<Tests>]
let tests = 
  testList "Fake.Core.CommandLineParsing.Tests" [

(*
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

      
      ()
    TestCaseHelper.Create("FAKE 5 usage (simple)", """
Usage:
  fake.exe file -- [<moreargs>...]

Options:
    """,
      "file -- wtf" ->= ["file", Flag;"<moreargs>", Argument "wtf";"--", Flag],
      "file -- --wtf --test" ->= ["--", Flag;"file", Flag;"<moreargs>", Arguments["--wtf"; "--test"]]
    )*)

    TestCaseHelper.Create("FAKE 5 usage", """
Usage:
  fake.exe run [-dn --fsiargs <args>] [<script>] [--] [<scriptarg>...]
  fake.exe build [-dnf --fsiargs <args>] [--] [<scriptarg>...]
  fake.exe -h | --help
  fake.exe --version

Options:
  --version                    Show version.
  -h, --help                    Show this screen.
  -d, --debug                  Debug the script (set a breakpoint at the start).
  -n, --nocache                Disable caching of the compiled script.
  -f, --script                 Specify the script to run.
  --fsiargs <args>             Arguments passed to the f# interactive / f# compiler.

Note: Further information can be retrieved via `fake run <script> -- --help`
""",
      "" ->! typeof<ArgvException>,
      "run"      ->= [("run", Command)],
      "build"      ->= [("build", Command)],
      "run build.fsx -- -s -e t1 t2"      ->= [("run", Command);("--", Flag);("<script>", Argument "build.fsx");("<scriptarg>", Arguments["-s";"-e";"t1";"t2"])],
      "run build.fsx -s -e t1 t2"      ->= [("run", Command);("<script>", Argument "build.fsx");("<scriptarg>", Arguments["-s";"-e";"t1";"t2"])],
      "run build.fsx -d -s -e t1 t2"      ->= [("run", Command);("--debug", Flag);("<script>", Argument "build.fsx");("<scriptarg>", Arguments["-s";"-e";"t1";"t2"])]
      //"run -d -- --help" ->! typeof<ArgvException>
    )
    TestCaseHelper.Create("Empty usage", """
Usage: prog

""",
      ""      ->= [],
      "--xxx" ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Basic short option", """
Usage: prog [options]

Options: -a  All.

""",
      ""   ->= [],
      "-a" ->= [("-a", Flag)],
      "-x" ->! typeof<ArgvException>
    )
    TestCaseHelper.Create("Basic long option", """
Usage: prog [options]

Options: --all  All.

""",
      ""      ->= [],
      "--all" ->= [("--all", Flag)],
      "--xxx" ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Synonymous short and long option, with truncation", """
Usage: prog [options]

Options: -v, --verbose  Verbose.

""",
      "--verbose" ->= [("-v", Flag);("--verbose", Flag)],
      "--ver"     ->= [("-v", Flag);("--verbose", Flag)],
      "-v"        ->= [("-v", Flag);("--verbose", Flag)]
    )

    TestCaseHelper.Create("Short option with argument", """
Usage: prog [options]

Options: -p PATH

""",
      "-p home/" ->= [("-p", Argument("home/"))],
      "-phome/"  ->= [("-p", Argument("home/"))],
      "-p"       ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Long option with argument", """
Usage: prog [options]

Options: --path <path>

""",
      "--path home/" ->= [("--path", Argument("home/"))],
      "--path=home/" ->= [("--path", Argument("home/"))],
      "--pa home/"   ->= [("--path", Argument("home/"))],
      "--pa=home/"   ->= [("--path", Argument("home/"))],
      "--path"       ->! typeof<ArgvException>
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

    TestCaseHelper.Create("Truncated long option disambiguation", """
Usage: prog [options]

Options: --version
         --verbose

""",
      "--version" ->= [("--version", Flag)],
      "--verbose" ->= [("--verbose", Flag)],
      "--ver"     ->! typeof<ArgvException>,
      "--verb"    ->= [("--verbose", Flag)]
    )

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
      "-a"    ->! typeof<ArgvException>,
      ""      ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Required short options in brackets", """
usage: prog (-a -b)

options: -a
         -b

""",
      "-a -b" ->= [("-a", Flag);("-b", Flag)],
      "-b -a" ->= [("-a", Flag);("-b", Flag)],
      "-a"    ->! typeof<ArgvException>,
      ""      ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Two options, one is optional","""
usage: prog [-a] -b

options: -a
 -b

""",
      "-a -b" ->= [("-a", Flag);("-b", Flag)],
      "-b -a" ->= [("-a", Flag);("-b", Flag)],
      "-a"    ->! typeof<ArgvException>,
      "-b"    ->= [("-b", Flag)],
      ""      ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Required in optional", """
usage: prog [(-a -b)]

options: -a
         -b

""",
      "-a -b" ->= [("-a", Flag);("-b", Flag)],
      "-b -a" ->= [("-a", Flag);("-b", Flag)],
      "-a"    ->! typeof<ArgvException>,
      "-b"    ->! typeof<ArgvException>,
      ""      ->= []
    )

    TestCaseHelper.Create("Exclusive or", """
usage: prog (-a|-b)

options: -a
         -b

""",
      "-a -b" ->! typeof<ArgvException>,
      ""      ->! typeof<ArgvException>,
      "-a"    ->= [("-a", Flag)],
      "-b"    ->= [("-b", Flag)]
    )

    TestCaseHelper.Create("Optional exclusive or", """
usage: prog [ -a | -b ]

options: -a
         -b

""",
      "-a -b" ->! typeof<ArgvException>,
      ""      ->= [],
      "-a"    ->= [("-a", Flag)],
      "-b"    ->= [("-b", Flag)]
    )

    TestCaseHelper.Create("Argument", """
usage: prog <arg>""",
      "10"    ->= [("<arg>", Argument("10"))],
      "10 20" ->! typeof<ArgvException>,
      ""      ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Optional argument", """
usage: prog [<arg>]""",
      "10"    ->= [("<arg>", Argument("10"))],
      "10 20" ->! typeof<ArgvException>,
      ""      ->= []
    )

    TestCaseHelper.Create("Multiple arguments", """
usage: prog <kind> <name> <type>""",
      "10 20 40" ->= [("<kind>", Argument("10"));("<name>", Argument("20"));("<type>", Argument("40"))],
      "10 20"    ->! typeof<ArgvException>,
      ""         ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Multiple arguments, two optional", """
usage: prog <kind> [<name> <type>]""",
      "10 20 40" ->= [("<kind>", Argument("10"));("<name>", Argument("20"));("<type>", Argument("40"))],
      "10 20"    ->= [("<kind>", Argument("10"));("<name>", Argument("20"))],
      ""         ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Multiple arguments xor'd in optional", """
usage: prog [<kind> | <name> <type>]""",
      "10 20 40" ->! typeof<ArgvException>,
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
      ""         ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Stacked argument", """
usage: prog [<name> <name>]""",
      "10 20" ->= [("<name>", Arguments(["20";"10"]))],
      "10"    ->= [("<name>", Argument("10"))],
      ""      ->= []
    )

    TestCaseHelper.Create("Same, but both arguments must be present", """
usage: prog [(<name> <name>)]""",
      "10 20" ->= [("<name>", Arguments(["20";"10"]))],
      "10"    ->! typeof<ArgvException>,
      ""      ->= []
    )

    TestCaseHelper.Create("Ellipsis (one or more (also, ALL-CAPS argument name))", """
usage: prog NAME...""",
      "10 20" ->= [("NAME", Arguments(["20";"10"]))],
      "10"    ->= [("NAME", Argument("10"))],
      ""      ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Optional in ellipsis", """
usage: prog [NAME]...""",
      "10 20" ->= [("NAME", Arguments(["20";"10"]))],
      "10"    ->= [("NAME", Argument("10"))],
      ""      ->= []
    )

    TestCaseHelper.Create("Ellipsis in optional", """
usage: prog [NAME...]""",
      "10 20" ->= [("NAME", Arguments(["20";"10"]))],
      "10"    ->= [("NAME", Argument("10"))],
      ""      ->= []
    )

    TestCaseHelper.Create("", """
usage: prog [NAME [NAME ...]]""",
      "10 20" ->= [("NAME", Arguments(["20";"10"]))],
      "10"    ->= [("NAME", Argument("10"))],
      ""      ->= []
    )

    TestCaseHelper.Create("Argument mismatch with option", """
usage: prog (NAME | --foo NAME)

options: --foo

""",
      "10"       ->= [("NAME", Argument("10"))],
      "--foo 10" ->= [("NAME", Argument("10"));("--foo", Flag)],
      "--foo=10" ->! typeof<ArgvException>
    )

    TestCaseHelper.Create("Multiple “options:” statements", """
usage: prog (NAME | --foo) [--bar | NAME]

options: --foo
options: --bar

""",
      "10"          ->= [("NAME", Argument("10"))],
      "10 20"       ->= [("NAME", Arguments(["20";"10"]))],
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
      ""        ->! typeof<ArgvException>,
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
      "-vvvv" ->! typeof<ArgvException>
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
      "go"       ->= [("go", Flag)],
      "go go"    ->= [("go", Flags(2))],
      "go go go" ->! typeof<ArgvException>
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
      "-aa" ->! typeof<ArgvException>
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
        "<direction>", Arguments(["right";"left"]);
        "--speed", Arguments(["9";"5"])
      ]
    )

    TestCaseHelper.Create("Required options should work with option shortcut", """
usage: prog [options] -a

options: -a

""",
      "-a" ->= [("-a", Flag)]
    )

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
      "--input a.txt --input=b.txt" ->= [("--input", Arguments(["b.txt";"a.txt"]))]
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