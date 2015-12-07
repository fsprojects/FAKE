namespace Docopt
#nowarn "62"
#light "off"

open FParsec
open System

module USPH =
  begin
    let inline λ f a b = f (a, b)
    let inline Λ f (a, b) = f a b

    type Ast =
      | Arg of unit
      | Sop of string
      | Lop of string
      | Cmd of string
      | Xor of Ast * Ast
      | Ell of Ast
      | Req of Ast
      | Opt of Ast
      | Ano of unit
      | Dsh of unit
      | Ssh of unit
      | Seq of Ast list

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
    let _ = opp.TermParser <- many (term .>> spaces) |>> Seq
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
  end
;;

type UsageParser(u':string, opts':Options) =
  class
    let parseAsync line' = async {
        return match run USPH.opp.ExpressionParser line' with
          | Success(r, _, _) -> r
          | _                -> invalidArg null null
      }
    let ast = u'.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
              |> Seq.map parseAsync
              |> Async.Parallel
              |> Async.RunSynchronously
              |> Seq.reduce (USPH.λ USPH.Xor)
    member __.Parse(argv':string array) =
      ()
    member __.Ast = ast
  end
;;
