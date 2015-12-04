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
  | Sop of string
  | Lop of string
  | Cmd of string
  | Xor of Token * Token
  | Ell of Token
  | Req of Token
  | Opt of Token
  | Ano of unit
  | Dsh of unit
  | Ssh of unit

let opp = OperatorPrecedenceParser<_, unit, unit>()
let pcmd = many1Satisfy (fun c' -> isLetter c' || isDigit c' || c' = '-' || c' = '_')
let preq = between (pchar '(' >>. spaces) (pchar ')') opp.ExpressionParser
let popt = between (pchar '[' >>. spaces) (pchar ']') opp.ExpressionParser
let pano = skipString "[options]"
let pdsh = skipString "[--]"
let pssh = skipString "[-]"
let term = choice [|Parser.parg |>> Arg;
                    pcmd |>> Cmd;
                    preq |>> Req;
                    pano |>> Ano;
                    pdsh |>> Dsh;
                    pssh |>> Ssh;
                    popt |>> Opt|]
opp.TermParser <- term .>> spaces
opp.AddOperator(InfixOperator("|", spaces, 10, Associativity.Left, (fun l' r' -> Xor(l', r'))))
opp.AddOperator(PostfixOperator("...", spaces, 20, false, Ell))

let eval argv' ast' =
  let mutable i = 0 in
  let rec eval = function
    | _ -> ()
  in eval ast'
