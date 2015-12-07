namespace Docopt
#nowarn "62"
#light "off"

open FParsec
open System

module internal USPH =
  begin
    let inline λ f a b = f (a, b)
    let inline Λ f (a, b) = f a b

    type Token =
      | Arg of unit
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
    let pupperArg:IOPT.Parser<unit> =
      let start c' = isUpper c' || isDigit c' in
      let cont c' = start c' || c' = '-' in
      identifier (IdentifierOptions(isAsciiIdStart=start,
                                    isAsciiIdContinue=cont,
                                    label="UPPER-CASE identifier")) >>% ()
    let plowerArg:IOPT.Parser<unit> =
      satisfyL (( = ) '<') "<lower-case> identifier"
      >>. skipMany1SatisfyL (( <> ) '>') "any character except '>'"
      .>> pchar '>'
    let parg:IOPT.Parser<unit> =
      pupperArg <|> plowerArg
    let pcmd = many1Satisfy (fun c' -> isLetter c' || isDigit c'
                                       || c' = '-' || c' = '_')
    let preq = between (pchar '(' >>. spaces) (pchar ')') opp.ExpressionParser
    let popt = between (pchar '[' >>. spaces) (pchar ']') opp.ExpressionParser
    let pano = skipString "[options]"
    let pdsh = skipString "[--]"
    let pssh = skipString "[-]"
    let term = choice [|parg |>> Arg;
                        pcmd |>> Cmd;
                        preq |>> Req;
                        pano |>> Ano;
                        pdsh |>> Dsh;
                        pssh |>> Ssh;
                        popt |>> Opt|]
    let pxor = InfixOperator("|", spaces, 10, Associativity.Left, λ Xor)
    let pell = PostfixOperator("...", spaces, 20, false, Ell)
    let _ = opp.TermParser <- term .>> spaces
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
  end
;;

type UsageParser(u':string, opts':Options) =
  class
    member __.Parse(argv':string array) =
      ()
  end
;;
