namespace Docopt
#nowarn "62"
#light "off"

open FParsec
open System
open System.Text

module _Private =
  begin
    type 'a GList = System.Collections.Generic.List<'a>

    [<NoComparison>]
    type Ast =
      | Eps
      | Sop of char GList
      | Sqb of Ast
      | Req of Ast
      | Arg of string
      | Cmd of string
      | Xor of Ast * Ast
      | Xoq of Ast GList
      | Seq of Ast GList
      | Ell of Ast * bool
      | Kln of Ast
      with // consider experimenting with Seq for optimizations
        member xx.Sops = match xx with
        | Sop(sops) -> sops
        | _         -> null
        static member Reduce = function
        | Seq(seq) when seq.Count = 1 -> Ast.Reduce (seq.[0])
        | Seq(seq)         -> let sops = GList<char>() in
                              let newseq = GList<Ast>() in
                              Seq.iter (fun ast' -> match Ast.Reduce ast' with
                                        | Sop(sop) -> sops.AddRange(sop)
                                        | ast      -> newseq.Add(ast)) seq;
                              if sops.Count <> 0 then newseq.Add(Sop(sops));
                              newseq |> Seq |> Ast.Reduce
        | Ell(Req(ast), _) -> Ell(ast, false)
        | Ell(Sqb(ast), _) -> Kln(ast)
        | ast              -> ast
      end

    let isLetterOrDigit c' = isLetter(c') || isDigit(c')
    let opp = OperatorPrecedenceParser<_, unit, unit>()
    let pupperArg =
      let start c' = isUpper c' || isDigit c' in
      let cont c' = start c' || c' = '-' in
      identifier (IdentifierOptions(isAsciiIdStart=start,
                                    isAsciiIdContinue=cont,
                                    label="UPPER-CASE identifier"))
    let plowerArg =
      satisfyL (( = ) '<') "<lower-case> identifier"
      >>. many1SatisfyL (( <> ) '>') "any character except '>'"
      .>> skipChar '>'
      |>> (fun name' -> String.Concat("<", name', ">"))
    let parg = pupperArg <|> plowerArg
    let pano = skipString "[options]"
    let psop = skipChar '-'
               >>. many1SatisfyL ( isLetterOrDigit ) "Short option(s)"
    let psqb = between (skipChar '[' >>. spaces) (skipChar ']') opp.ExpressionParser
    let preq = between (skipChar '(' >>. spaces) (skipChar ')') opp.ExpressionParser
    let pcmd = many1Satisfy (fun c' -> isLetter(c') || isDigit(c') || c' = '-')
    let term = choice [|
                        pano >>% Eps;
                        psop |>> (GList<char> >> Sop);
                        psqb |>> Sqb;
                        preq |>> Req;
                        parg |>> Arg;
                        pcmd |>> Cmd;
                        |]
    let pxor = InfixOperator("|", spaces, 10, Associativity.Left,
                             fun x' y' -> Xor(x', y'))
    let pell = let makeEll = function
               | Seq(seq) as ast -> let cell = seq.[seq.Count - 1] in
                                    seq.[seq.Count - 1] <- Ell(cell, false);
                                    ast
               | ast             -> Ell(ast, false)
               in PostfixOperator("...", spaces, 20, false, makeEll)
    let _ = opp.TermParser <- sepEndBy1 term spaces1 |>> (GList<Ast> >> Seq)
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
    let pusageLine = spaces >>. opp.ExpressionParser |>> Ast.Reduce
  end
;;

open _Private

module Err = FParsec.Error
module Err =
  begin
    let unexpectedShort = string
                          >> ( + ) "short option -"
                          >> unexpected
    let unexpectedLong = ( + ) "long option --"
                         >> unexpected
    let expectedArg = ( + ) "argument "
                      >> expected
    let unexpectedArg = unexpected "argument "
    let ambiguousArg = ( + ) "ambiguous long option --"
                       >> otherError
  end

exception UsageException of string
exception ArgvException of string

type UsageParser(u':string, opts':Options) =
  class
    let parseAsync = function
    | ""   -> async { return Eps }
    | line -> async {
        let line = line.TrimStart() in
        let index = line.IndexOfAny([|' ';'\t'|]) in
        return if index = -1 then Eps
               else match run (spaces >>. opp.ExpressionParser)
                              (line.Substring(index)) with
                    | Success(ast, _, _) -> ast
                    | Failure(err, _, _) -> raise (UsageException(err))
      }
    let ast =
      u'.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
      |> Array.map parseAsync
      |> Async.Parallel
      |> Async.RunSynchronously
      |> fun asts -> GList<Ast>(asts)

    member __.Parse(argv':string array, args':Arguments.Dictionary) =
      let rec eval = function
      | _ -> None
      in args'
    member __.Ast = ast
  end
;;
