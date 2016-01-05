open Docopt
open Docopt.Arguments
open System
open System.Diagnostics

// HELPER FUNCTIONS FOR ASSERTIONS
type Assert =
  static member Seq(usage':string, [<ParamArray>]statements':(Docopt -> string * bool)[]) =
    let mutable doc = Docopt(usage') in
    printf "{\n  Testing %A\n" usage'
    printfn "  Asts: %A" doc.UsageParser.Asts
    printfn "  Dict: %A\n" doc.DefaultDictionary
    Array.iter (fun assertion' -> let doc = Docopt(usage')
                                  let msg, res = assertion' doc in
                                  printfn "    %s . . . %A" msg res
                                  Debug.Assert(res, msg)) statements'
    Console.WriteLine("}\n")
let ( ->= ) (argv':string) val' (doc':Docopt) =
  let argv = argv'.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) in
  let args = doc'.Parse(argv) in
  let res = (List.sort (args.AsList())) = List.sort val' in
  if not res then printfn "Got args = %A" args
  (sprintf "%A ->= %A" argv' val'), res
let ( ->! ) (argv':string) val' (doc':Docopt) =
  let argv = argv'.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) in
  let msg, res =
    try let _ = doc'.Parse(argv).AsList() in (box "<NO EXN>", false)
    with e -> (box e, e.GetType() = val') in
  (sprintf "%A ->! %A" argv' msg), res
// END HELPER FUNCTIONS FOR ASSERTIONS
(*
(* Empty usage *)
Assert.Seq("""
Usage: prog

""",
  ""      ->= [],
  "--xxx" ->! typeof<ArgvException>
)

(* Basic short option *)
Assert.Seq("""
Usage: prog [options]

Options: -a  All.

""",
  ""   ->= [("-a", Flag(false))],
  "-a" ->= [("-a", Flag(true))],
  "-x" ->! typeof<ArgvException>
)

(* Basic long option *)
Assert.Seq("""Usage: prog [options]

Options: --all  All.

""",
  ""      ->= [("--all", Flag(false))],
  "--all" ->= [("--all", Flag(true))],
  "--xxx" ->! typeof<ArgvException>
)

(* Synonymous short and long option, with truncation *)
Assert.Seq("""Usage: prog [options]

Options: -v, --verbose  Verbose.

""",
  "--verbose" ->= [("-v", Flag(true));("--verbose", Flag(true))],
  "--ver"     ->= [("-v", Flag(true));("--verbose", Flag(true))],
  "-v"        ->= [("-v", Flag(true));("--verbose", Flag(true))]
)

(* Short option with argument *)
Assert.Seq("""Usage: prog [options]

Options: -p PATH

""",
  "-p home/" ->= [("-p", Argument("home/"))],
  "-phome/"  ->= [("-p", Argument("home/"))],
  "-p"       ->! typeof<ArgvException>
)

(* Long option with argument *)
Assert.Seq("""Usage: prog [options]

Options: --path <path>

""",
  "--path home/" ->= [("--path", Argument("home/"))],
  "--path=home/" ->= [("--path", Argument("home/"))],
  "--pa home/"   ->= [("--path", Argument("home/"))],
  "--pa=home/"   ->= [("--path", Argument("home/"))],
  "--path"       ->! typeof<ArgvException>
)

(* Synonymous short and long option with both arguments declared *)
Assert.Seq("""Usage: prog [options]

Options: -p PATH, --path=<path>  Path to files.

""",
  "-proot" ->= [("-p", Argument("root"));("--path", Argument("root"))]
)

(* Synonymous short and long option with one argument declared *)
Assert.Seq("""Usage: prog [options]

Options:    -p --path PATH  Path to files.

""",
  "-p root"     ->= [("-p", Argument("root"));("--path", Argument("root"))],
  "--path root" ->= [("-p", Argument("root"));("--path", Argument("root"))]
)

(* Short option with default *)
Assert.Seq("""Usage: prog [options]

Options:
 -p PATH  Path to files [default: ./]

""",
  ""       ->= [("-p", Default("./"))],
  "-phome" ->= [("-p", Argument("home"))]
)

(* Unusual formatting *)
Assert.Seq("""UsAgE: prog [options]

OpTiOnS: --path=<files>  Path to files
                [dEfAuLt: /root]

""",
  ""            ->= [("--path", Default("/root"))],
  "--path=home" ->= [("--path", Argument("home"))]
)

(* Multiple short options *)
Assert.Seq("""usage: prog [options]

options:
    -a        Add
    -r        Remote
    -m <msg>  Message

""",
  "-a -r -m Hello" ->= [("-a", Flag(true));("-r", Flag(true));("-m", Argument("Hello"))],
  "-armyourass"    ->= [("-a", Flag(true));("-r", Flag(true));("-m", Argument("yourass"))],
  "-a -r"          ->= [("-a", Flag(true));("-r", Flag(true));("-m", Flag(false))]
)

(* Truncated long option disambiguation *)
Assert.Seq("""Usage: prog [options]

Options: --version
         --verbose

""",
  "--version" ->= [("--version", Flag(true));("--verbose", Flag(false))],
  "--verbose" ->= [("--version", Flag(false));("--verbose", Flag(true))],
  "--ver"     ->! typeof<ArgvException>,
  "--verb"    ->= [("--version", Flag(false));("--verbose", Flag(true))]
)

(* Short options in square brackets *)
Assert.Seq("""usage: prog [-a -r -m <msg>]

options:
 -a        Add
 -r        Remote
 -m <msg>  Message

""",
  "-armyourass" ->= [("-a", Flag(true));("-r", Flag(true));("-m", Argument("yourass"))]
)

(* Short option pack in square brackets *)
Assert.Seq("""usage: prog [-armmsg]

options: -a        Add
         -r        Remote
         -m <msg>  Message

""",
  "-a -r -m Hello" ->= [("-a", Flag(true));("-r", Flag(true));("-m", Argument("Hello"))]
)
*)
Assert.Seq("""usage: prog -a -b

options:
 -a
 -b

""",
  "-a -b" ->= [("-a", Flag(true));("-b", Flag(true))],
  "-b -a" ->= [("-a", Flag(true));("-b", Flag(true))],
  "-a"    ->! typeof<ArgvException>,
  ""      ->! typeof<ArgvException>
)

(*
let doc = Docopt("""usage: prog (-a -b)

options: -a
         -b

""")
$ prog -a -b
{"-a": true, "-b": true}

$ prog -b -a
{"-a": true, "-b": true}

$ prog -a
"user-error"

$ prog
"user-error"


let doc = Docopt("""usage: prog [-a] -b

options: -a
 -b

""")
$ prog -a -b
{"-a": true, "-b": true}

$ prog -b -a
{"-a": true, "-b": true}

$ prog -a
"user-error"

$ prog -b
{"-a": false, "-b": true}

$ prog
"user-error"


let doc = Docopt("""usage: prog [(-a -b)]

options: -a
         -b

""")
$ prog -a -b
{"-a": true, "-b": true}

$ prog -b -a
{"-a": true, "-b": true}

$ prog -a
"user-error"

$ prog -b
"user-error"

$ prog
{"-a": false, "-b": false}


let doc = Docopt("""usage: prog (-a|-b)

options: -a
         -b

""")
$ prog -a -b
"user-error"

$ prog
"user-error"

$ prog -a
{"-a": true, "-b": false}

$ prog -b
{"-a": false, "-b": true}


let doc = Docopt("""usage: prog [ -a | -b ]

options: -a
         -b

""")
$ prog -a -b
"user-error"

$ prog
{"-a": false, "-b": false}

$ prog -a
{"-a": true, "-b": false}

$ prog -b
{"-a": false, "-b": true}


let doc = Docopt("""usage: prog <arg>""")
$ prog 10
{"<arg>": "10"}

$ prog 10 20
"user-error"

$ prog
"user-error"


let doc = Docopt("""usage: prog [<arg>]""")
$ prog 10
{"<arg>": "10"}

$ prog 10 20
"user-error"

$ prog
{"<arg>": null}


let doc = Docopt("""usage: prog <kind> <name> <type>""")
$ prog 10 20 40
{"<kind>": "10", "<name>": "20", "<type>": "40"}

$ prog 10 20
"user-error"

$ prog
"user-error"


let doc = Docopt("""usage: prog <kind> [<name> <type>]""")
$ prog 10 20 40
{"<kind>": "10", "<name>": "20", "<type>": "40"}

$ prog 10 20
{"<kind>": "10", "<name>": "20", "<type>": null}

$ prog
"user-error"


let doc = Docopt("""usage: prog [<kind> | <name> <type>]""")
$ prog 10 20 40
"user-error"

$ prog 20 40
{"<kind>": null, "<name>": "20", "<type>": "40"}

$ prog
{"<kind>": null, "<name>": null, "<type>": null}


let doc = Docopt("""usage: prog (<kind> --all | <name>)

options:
 --all

""")
$ prog 10 --all
{"<kind>": "10", "--all": true, "<name>": null}

$ prog 10
{"<kind>": null, "--all": false, "<name>": "10"}

$ prog
"user-error"


let doc = Docopt("""usage: prog [<name> <name>]""")
$ prog 10 20
{"<name>": ["10", "20"]}

$ prog 10
{"<name>": ["10"]}

$ prog
{"<name>": []}


let doc = Docopt("""usage: prog [(<name> <name>)]""")
$ prog 10 20
{"<name>": ["10", "20"]}

$ prog 10
"user-error"

$ prog
{"<name>": []}


let doc = Docopt("""usage: prog NAME...""")
$ prog 10 20
{"NAME": ["10", "20"]}

$ prog 10
{"NAME": ["10"]}

$ prog
"user-error"


let doc = Docopt("""usage: prog [NAME]...""")
$ prog 10 20
{"NAME": ["10", "20"]}

$ prog 10
{"NAME": ["10"]}

$ prog
{"NAME": []}


let doc = Docopt("""usage: prog [NAME...]""")
$ prog 10 20
{"NAME": ["10", "20"]}

$ prog 10
{"NAME": ["10"]}

$ prog
{"NAME": []}


let doc = Docopt("""usage: prog [NAME [NAME ...]]""")
$ prog 10 20
{"NAME": ["10", "20"]}

$ prog 10
{"NAME": ["10"]}

$ prog
{"NAME": []}


let doc = Docopt("""usage: prog (NAME | --foo NAME)

options: --foo

""")
$ prog 10
{"NAME": "10", "--foo": false}

$ prog --foo 10
{"NAME": "10", "--foo": true}

$ prog --foo=10
"user-error"


let doc = Docopt("""usage: prog (NAME | --foo) [--bar | NAME]

options: --foo
options: --bar

""")
$ prog 10
{"NAME": ["10"], "--foo": false, "--bar": false}

$ prog 10 20
{"NAME": ["10", "20"], "--foo": false, "--bar": false}

$ prog --foo --bar
{"NAME": [], "--foo": true, "--bar": true}


let doc = Docopt("""Naval Fate.

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

""")
$ prog ship Guardian move 150 300 --speed=20
{"--drifting": false,
 "--help": false,
 "--moored": false,
 "--speed": "20",
 "--version": false,
 "<name>": ["Guardian"],
 "<x>": "150",
 "<y>": "300",
 "mine": false,
 "move": true,
 "new": false,
 "remove": false,
 "set": false,
 "ship": true,
 "shoot": false}


let doc = Docopt("""usage: prog --hello""")
$ prog --hello
{"--hello": true}


let doc = Docopt("""usage: prog [--hello=<world>]""")
$ prog
{"--hello": null}

$ prog --hello wrld
{"--hello": "wrld"}


let doc = Docopt("""usage: prog [-o]""")
$ prog
{"-o": false}

$ prog -o
{"-o": true}


let doc = Docopt("""usage: prog [-opr]""")
$ prog -op
{"-o": true, "-p": true, "-r": false}


let doc = Docopt("""usage: prog --aabb | --aa""")
$ prog --aa
{"--aabb": false, "--aa": true}

$ prog --a
"user-error"  # not a unique prefix

//
// Counting number of flags
//

let doc = Docopt("""Usage: prog -v""")
$ prog -v
{"-v": true}


let doc = Docopt("""Usage: prog [-v -v]""")
$ prog
{"-v": 0}

$ prog -v
{"-v": 1}

$ prog -vv
{"-v": 2}


let doc = Docopt("""Usage: prog -v ...""")
$ prog
"user-error"

$ prog -v
{"-v": 1}

$ prog -vv
{"-v": 2}

$ prog -vvvvvv
{"-v": 6}


let doc = Docopt("""Usage: prog [-v | -vv | -vvv]

This one is probably most readable user-friednly variant.

""")
$ prog
{"-v": 0}

$ prog -v
{"-v": 1}

$ prog -vv
{"-v": 2}

$ prog -vvvv
"user-error"


let doc = Docopt("""usage: prog [--ver --ver]""")
$ prog --ver --ver
{"--ver": 2}


//
// Counting commands
//

let doc = Docopt("""usage: prog [go]""")
$ prog go
{"go": true}


let doc = Docopt("""usage: prog [go go]""")
$ prog
{"go": 0}

$ prog go
{"go": 1}

$ prog go go
{"go": 2}

$ prog go go go
"user-error"

let doc = Docopt("""usage: prog go...""")
$ prog go go go go go
{"go": 5}

//
// [options] does not include options from usage-pattern
//
let doc = Docopt("""usage: prog [options] [-a]

options: -a
         -b
""")
$ prog -a
{"-a": true, "-b": false}

$ prog -aa
"user-error"

//
// Test [options] shourtcut
//

let doc = Docopt("""Usage: prog [options] A
Options:
    -q  Be quiet
    -v  Be verbose.

""")
$ prog arg
{"A": "arg", "-v": false, "-q": false}

$ prog -v arg
{"A": "arg", "-v": true, "-q": false}

$ prog -q arg
{"A": "arg", "-v": false, "-q": true}

//
// Test single dash
//

let doc = Docopt("""usage: prog [-]""")

$ prog -
{"-": true}

$ prog
{"-": false}

//
// If argument is repeated, its value should always be a list
//

let doc = Docopt("""usage: prog [NAME [NAME ...]]""")

$ prog a b
{"NAME": ["a", "b"]}

$ prog
{"NAME": []}

//
// Option's argument defaults to null/None
//

let doc = Docopt("""usage: prog [options]
options:
 -a        Add
 -m <msg>  Message

""")
$ prog -a
{"-m": null, "-a": true}

//
// Test options without description
//

let doc = Docopt("""usage: prog --hello""")
$ prog --hello
{"--hello": true}

let doc = Docopt("""usage: prog [--hello=<world>]""")
$ prog
{"--hello": null}

$ prog --hello wrld
{"--hello": "wrld"}

let doc = Docopt("""usage: prog [-o]""")
$ prog
{"-o": false}

$ prog -o
{"-o": true}

let doc = Docopt("""usage: prog [-opr]""")
$ prog -op
{"-o": true, "-p": true, "-r": false}

let doc = Docopt("""usage: git [-v | --verbose]""")
$ prog -v
{"-v": true, "--verbose": false}

let doc = Docopt("""usage: git remote [-v | --verbose]""")
$ prog remote -v
{"remote": true, "-v": true, "--verbose": false}

//
// Test empty usage pattern
//

let doc = Docopt("""usage: prog""")
$ prog
{}

let doc = Docopt("""usage: prog
           prog <a> <b>
""")
$ prog 1 2
{"<a>": "1", "<b>": "2"}

$ prog
{"<a>": null, "<b>": null}

let doc = Docopt("""usage: prog <a> <b>
           prog
""")
$ prog
{"<a>": null, "<b>": null}

//
// Option's argument should not capture default value from usage pattern
//

let doc = Docopt("""usage: prog [--file=<f>]""")
$ prog
{"--file": null}

let doc = Docopt("""usage: prog [--file=<f>]

options: --file <a>

""")
$ prog
{"--file": null}

let doc = Docopt("""Usage: prog [-a <host:port>]

Options: -a, --address <host:port>  TCP address [default: localhost:6283].

""")
$ prog
{"--address": "localhost:6283"}

//
// If option with argument could be repeated,
// its arguments should be accumulated into a list
//

let doc = Docopt("""usage: prog --long=<arg> ...""")

$ prog --long one
{"--long": ["one"]}

$ prog --long one --long two
{"--long": ["one", "two"]}

//
// Test multiple elements repeated at once
//

let doc = Docopt("""usage: prog (go <direction> --speed=<km/h>)...""")
$ prog  go left --speed=5  go right --speed=9
{"go": 2, "<direction>": ["left", "right"], "--speed": ["5", "9"]}

//
// Required options should work with option shortcut
//

let doc = Docopt("""usage: prog [options] -a

options: -a

""")
$ prog -a
{"-a": true}

//
// If option could be repeated its defaults should be split into a list
//

let doc = Docopt("""usage: prog [-o <o>]...

options: -o <o>  [default: x]

""")
$ prog -o this -o that
{"-o": ["this", "that"]}

$ prog
{"-o": ["x"]}

let doc = Docopt("""usage: prog [-o <o>]...

options: -o <o>  [default: x y]

""")
$ prog -o this
{"-o": ["this"]}

$ prog
{"-o": ["x", "y"]}

//
// Test stacked option's argument
//

let doc = Docopt("""usage: prog -pPATH

options: -p PATH

""")
$ prog -pHOME
{"-p": "HOME"}

//
// Issue 56: Repeated mutually exclusive args give nested lists sometimes
//

let doc = Docopt("""Usage: foo (--xx=x|--yy=y)...""")
$ prog --xx=1 --yy=2
{"--xx": ["1"], "--yy": ["2"]}

//
// POSIXly correct tokenization
//

let doc = Docopt("""usage: prog [<input file>]""")
$ prog f.txt
{"<input file>": "f.txt"}

let doc = Docopt("""usage: prog [--input=<file name>]...""")
$ prog --input a.txt --input=b.txt
{"--input": ["a.txt", "b.txt"]}

//
// Issue 85: `[options]` shourtcut with multiple subcommands
//

let doc = Docopt("""usage: prog good [options]
           prog fail [options]

options: --loglevel=N

""")
$ prog fail --loglevel 5
{"--loglevel": "5", "fail": true, "good": false}

//
// Usage-section syntax
//

let doc = Docopt("""usage:prog --foo""")
$ prog --foo
{"--foo": true}

let doc = Docopt("""PROGRAM USAGE: prog --foo""")
$ prog --foo
{"--foo": true}

let doc = Docopt("""Usage: prog --foo
           prog --bar
NOT PART OF SECTION""")
$ prog --foo
{"--foo": true, "--bar": false}

let doc = Docopt("""Usage:
 prog --foo
 prog --bar

NOT PART OF SECTION""")
$ prog --foo
{"--foo": true, "--bar": false}

let doc = Docopt("""Usage:
 prog --foo
 prog --bar
NOT PART OF SECTION""")
$ prog --foo
{"--foo": true, "--bar": false}

//
// Options-section syntax
//

let doc = Docopt("""Usage: prog [options]

global options: --foo
local options: --baz
               --bar
other options:
 --egg
 --spam
-not-an-option-

""")
$ prog --baz --egg
{"--foo": false, "--baz": true, "--bar": false, "--egg": true, "--spam": false}

*)