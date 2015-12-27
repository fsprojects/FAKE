namespace Docopt
#nowarn "62"
#light "off"

open FParsec
open System
open System.Text

module USPH =
  begin
    [<NoComparison>]
    type Opt =
      {
        Sop : string;
        Lop : string list;
        Ano : bool;
      }
      with
        static member Default = { Sop=""; Lop=[]; Ano=false; }
      end

    [<NoComparison>]
    type Ast =
      | Arg of string      // Argument
      | Cmd of string      // Command
      | Xor of (Ast * Ast) // Mutual exclusion (| operator)
      | Ell of Ast         // One or more (... operator)
      | Req of Ast         // Required (between parenthesis)
      | Sqb of Ast         // Optional (between square brackets)
      | Seq of Ast array   // Sequence of Ast's
      | Eps                // Epsilon parser

    let isLetterOrDigit c' = isLetter(c') || isDigit(c')
    let opp = OperatorPrecedenceParser<_, unit, Opt>()
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
    let psop = let sopt = ref<string> null in skipChar '-'
               >>. many1SatisfyL ( isLetterOrDigit ) "Short option(s)"
               |>> (( := ) sopt)
               >>. updateUserState (fun o' -> { o' with Sop=o'.Sop + (!sopt) })
    let pano = skipString "[options]"
               >>. updateUserState (fun o' -> { o' with Ano=true })
    let psqb = between (pchar '[' >>. spaces) (pchar ']') opp.ExpressionParser
    let preq = between (pchar '(' >>. spaces) (pchar ')') opp.ExpressionParser
    let parg = pupperArg <|> plowerArg
    let pcmd = many1Satisfy (fun c' -> isLetter(c') || isDigit(c'))
    let term = choice [|
                        psop >>% Eps;
                        pano >>% Eps;
                        psqb |>> Sqb;
                        preq |>> Req;
                        parg |>> Arg;
                        pcmd |>> Cmd;
                        |]
    let pxor = InfixOperator("|", spaces, 10, Associativity.Left,
                             fun x' y' -> Xor(x', y'))
    let pell = let makeEll = function
                 | Seq(seq) as ast -> let cell = seq.[seq.Length - 1] in
                                      (seq.[seq.Length - 1] <- Ell(cell); ast)
                 | ast             -> Ell(ast)
               in PostfixOperator("...", spaces, 20, false, makeEll)
    let _ = opp.TermParser <- sepEndBy1 term spaces1
                              |>> function
                                    | []    -> Eps
                                    | [ast] -> ast
                                    | asts  -> Seq(List.toArray asts)
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
    let pusageLine = spaces >>. opp.ExpressionParser
  end
;;

open USPH
module Err = FParsec.Error

exception UsageException of string
exception ArgvException of string

type UsageParser(u':string, opts':Options) =
  class
    let parseAsync = function
    | ""   -> async { return (Eps, Opt.Default) }
    | line -> async {
        let line = line.TrimStart() in
        let index = line.IndexOfAny([|' ';'\t'|]) in
        return if index = -1 then (Eps, Opt.Default)
               else match runParserOnString pusageLine Opt.Default "" (line.Substring(index)) with
                    | Success(ast, opt, _) -> (ast, opt)
                    | Failure(err, _, _)   -> raise (UsageException(err))
      }
    let asts =
      u'.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
      |> Array.map parseAsync
      |> Async.Parallel
      |> Async.RunSynchronously
      |> function
         | [||] -> [|Eps, Opt.Default|]
         | res  -> res
    let i = ref 0
    let len = ref 0
    let argv = ref<string array> null
    let args = ref<Arguments.Dictionary> null
    let carg () = let argv = !argv in argv.[!i]
    let eval ast' opt' =
      let rec eval ast' =
        if !i < !len
        then let c = carg () in
             if c.Length > 1 && c.[0] = '-'
             then match if c.Length > 2 && c.[1] = '-'
                        then flop (c.Substring(2))
                        else fsop (c.Substring(1)) with
                  | None -> evalast ast'
                  | some -> some
             else evalast ast'
        else evalast ast'
      and evalast = function
      | Arg(arg) -> farg arg
      | Cmd(cmd) -> fcmd cmd
      | Xor(xor) -> fxor xor
      | Ell(ast) -> fell ast
      | Req(ast) -> freq ast
      | Sqb(ast) -> fsqb ast
      | Seq(seq) -> fseq seq
      | Eps      -> None
      and farg arg' = None
      and fcmd cmd' = None
      and fxor xor' = None
      and fell ast' = None
      and freq ast' = None
      and fsqb ast' = None
      and fseq seq' = None
      and fsop sop' = if opt'.Ano
                      then Seq.tryFind ((!args).AddShort >> not) sop'
                           |> Option.map (string >> ( + ) "short option -"
                                                 >> Err.unexpected)
                      else (incr i; None)
      and flop lop' = if opt'.Ano
                      then if (!args).AddLong(lop')
                           then None
                           else Some(Err.unexpected("long option --" + lop'))
                      else (incr i; None)
      in eval ast'
//      let e = ref None in
//      let pred ast' = match eval ast' with
//        | None -> false
//        | err  -> e := err; true
//      in if List.exists pred seq'
//      then !e
//      else None

    member __.Parse(argv':string array, args':Arguments.Dictionary) =
      let lastError = ref (ArgvException(null)) in
      len := argv'.Length;
      argv := argv';
      let predicate (ast, opt) =
        i := 0;
        args := args';
        match eval ast opt with
        | None             -> true
        | Some(err)        -> let pos = FParsec.Position("", 0L, 0L, 0L) in
                              lastError := Err.ParserError(pos, null, err).ToString()
                                           |> ArgvException;
                              false
      in if Array.exists ( predicate ) asts
      then !args
      else raise (!lastError)
    member __.Asts = asts
  end
;;
