#I __SOURCE_DIRECTORY__
#r "../packages/FParsec-Big-Data-Edition.1.0.2/lib/net45/FParsecCS.dll"
#r "../packages/FParsec-Big-Data-Edition.1.0.2/lib/net45/FParsec.dll"
#r "System.Core.dll"
#r "System.dll"
#r "System.Numerics.dll"
#load "../Docopt/Token.fs"
      "../Docopt/Options.fs"
      "../Docopt/Parser.fs"

open FParsec
open Docopt

type Token =
  | Arg of Token.Argument
  | Xor of Token * Token
  | Ell of Token

let opp = OperatorPrecedenceParser<_, unit, unit>()
let term = Parser.parg |>> Arg
opp.TermParser <- term .>> spaces
opp.AddOperator(InfixOperator("|", spaces, 10, Associativity.Left, (fun l' r' -> Xor(l', r'))))
opp.AddOperator(PostfixOperator("...", spaces, 20, false, Ell))
