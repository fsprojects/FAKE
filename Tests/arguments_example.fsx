#r @"../bin/FParsecCS.dll"
#r @"../bin/FParsec.dll"
#r @"../bin/Docopt.dll"

open Docopt
open System

[<Literal>]
let doc = """Usage: arguments_example.fsx [-vqrh] [FILE] ...
          arguments_example.fsx (--left | --right) CORRECTION FILE

Process FILE and optionally apply correction to either left-hand side or
right-hand side.

Arguments:
  FILE        optional input file
  CORRECTION  correction angle, needs FILE, --left or --right to be present

Options:
  -h --help
  -v       verbose mode
  -q       quiet mode
  -r       make report
  --left   use left-hand side
  --right  use right-hand side

"""

let argv = fsi.CommandLineArgs.[1..]
let args = Docopt(doc, argv).Parse()
do printfn "%A" args
