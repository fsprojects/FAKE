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
      | Arg of string    // Argument
      | Sop of string    // Short option pack
      | Lop of string    // Long option
      | Cmd of string    // Command
      | Xor of Ast * Ast // Mutual exclusion (| operator)
      | Ell of Ast       // One or more (... operator)
      | Req of Ast       // Required (between parenthesis)
      | Opt of Ast       // Optional (between square brackets)
      | Ano              // Any options `[options]`
      | Dsh              // Double dash `[--]` (Bowser team is the best)
      | Ssh              // Single dash `[-]`
      | Seq of Ast list  // Sequence

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
      .>> pchar '>'
      |>> (fun name' -> String.Concat("<", name', ">"))
    let parg = pupperArg <|> plowerArg
    let plop = 
      let predicate c' = isLetter(c') || isDigit(c') || c' = '-' || c' = '_'
      in skipString "--" >>. many1Satisfy predicate
    let pcmd = many1Satisfy (fun c' -> c' <> ' ' && c' <> '\t')
    let preq = between (pchar '(' >>. spaces) (pchar ')') opp.ExpressionParser
    let popt = between (pchar '[' >>. spaces) (pchar ']') opp.ExpressionParser
    let pano = skipString "[options]"
    let pdsh = skipString "[--]"
    let pssh = skipString "[-]"
    let term = choice [|parg |>> Arg;
                        plop |>> Lop;
                        pcmd |>> Cmd;
                        preq |>> Req;
                        pano >>% Ano;
                        pdsh >>% Dsh;
                        pssh >>% Ssh;
                        popt |>> Opt|]
    let pxor = InfixOperator("|", spaces, 10, Associativity.Left, λ Xor)
    let pell = PostfixOperator("...", spaces, 20, false, Ell)
    let _ = opp.TermParser <- many (term .>> spaces)
                              |>> function [ast] -> ast | asts -> Seq asts
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
  end
;;

open USPH
module Err = FParsec.Error

exception UsageException of string
exception ArgvException of string

type UsageParser(u':string, opts':Options) =
  class
    let parseAsync (line':string) = async {
        let line' = line'.TrimStart() in
        let line' = line'.Substring(line'.IndexOfAny([|' ';'\t'|])) in
        return match run opp.ExpressionParser line' with
          | Success(res, _, _) -> res
          | Failure(err, _, _) -> raise (UsageException(err))
      }
    let ast = u'.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
              |> Array.map parseAsync
              |> Async.Parallel
              |> Async.RunSynchronously
              |> Array.reduce (λ Xor)
    let i = ref 0
    let len = ref 0
    let argv = ref<_ array> null
    let rec eval = function
      | Arg(arg) -> farg arg
      | Sop(sop) -> None
      | Lop(lop) -> flop lop
      | Cmd(cmd) -> fcmd cmd
      | Xor(l,r) -> fxor l r
      | Ell(ast) -> fell ast
      | Req(ast) -> freq ast
      | Opt(ast) -> fopt ast
      | Ano      -> None
      | Dsh      -> None
      | Ssh      -> None
      | Seq(seq) -> fseq seq
    and farg arg' =
      if !i = !len
      then Some(Err.expected(String.Concat("Argument: `", arg', "`")))
      else (incr i; None)
    and flop lop' =
      let cell:string = (!argv).[!i] in
      if cell.Length <= 2 || cell.[0] <> '-' || cell.[1] <> '-'
         || not (cell.EndsWith(lop'))
      then Some(Err.expected(String.Concat("Long option: --", lop')))
      else (incr i; None)
    and fcmd cmd' =
      if (!argv).[!i] = cmd'
      then (incr i; None)
      else Some(Err.expectedString cmd')
    and fxor l' r' =
      if (eval l').IsNone then None else eval r'
    and fell ast' =
      try match eval ast' with
            | None -> while (eval ast').IsNone do () done; None
            | err  -> err
      with :? IndexOutOfRangeException -> None
    and freq ast' =
      eval ast'
    and fopt ast' =
      let _ = eval ast' in None
    and fseq seq' =
      let e = ref None in
      let pred ast' = match eval ast' with
        | None -> false
        | err  -> e := err; true
      in if List.exists pred seq'
      then !e
      else None

    member __.Parse(argv':string array, args':Args) =
      i := 0;
      len := argv'.Length;
      argv := argv';
      match eval ast with
        | _ when !i <> !len -> ArgvException("") |> raise
        | None              -> args'
        | Some(err)         -> let pos = FParsec.Position("", 0L, 0L, 0L) in
                               Err.ParserError(pos, null, err).ToString()
                               |> ArgvException
                               |> raise
    member __.Ast = ast
  end
;;
