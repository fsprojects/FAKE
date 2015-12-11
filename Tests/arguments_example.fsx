#I __SOURCE_DIRECTORY__
#r "../packages/FParsec-Big-Data-Edition.1.0.2/lib/net45/FParsecCS.dll"
#r "../packages/FParsec-Big-Data-Edition.1.0.2/lib/net45/FParsec.dll"
//#r "../bin/Docopt.dll"
#r "System.Core.dll"
#r "System.dll"
#r "System.Numerics.dll"
#load "../Docopt/AssemblyInfo.fs"
      "../Docopt/Token.fs"
      "../Docopt/Options.fs"
      "../Docopt/OptionsParser.fs"
      "../Docopt/Args.fs"
      "../Docopt/UsageParser.fs"
      "../Docopt/Docopt.fs"

open Docopt
open System

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

try
  let d = Docopt(doc) in
  let argv = "--left corr file.txt".Split([|' '|], StringSplitOptions.RemoveEmptyEntries) in
  //let args = d.Parse(argv) in
  printfn "Pusage:\n%A" d.Pusage.Ast
  ()
with
  | UsageException(err) -> printfn "Error: %s" err
