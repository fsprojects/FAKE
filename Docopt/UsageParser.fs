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
      | Ano
      | Sop of char GList
      | Sqb of Ast
      | Req of Ast
      | Arg of string
      | Cmd of string
      | Xor of Ast * Ast
      | Xoq of Ast GList
      | Seq of Ast GList
      | Ell of Ast * bool ref
      | Kln of Ast
      with
        static member Reduce = function
        | Seq(seq) when seq.Count = 1 -> Ast.Reduce (seq.[0])
        | Seq(seq)         -> let sops = GList<char>() in
                              let newseq = GList<Ast>() in
                              Seq.iter (fun ast' -> match Ast.Reduce ast' with
                                        | Sop(sop) -> sops.AddRange(sop)
                                        | ast      -> newseq.Add(ast)) seq;
                              if sops.Count <> 0 then newseq.Add(Sop(sops));
                              newseq |> Seq |> Ast.Reduce
        | Ell(Req(ast), _) -> Ell(ast, ref false)
        | Ell(Sqb(ast), _) -> Kln(ast)
        | ast              -> ast
        static member MatchSopt(s':char) = function
        | Sop(sop) -> (match sop.FindIndex(fun x' -> x' = s') with
                       | -1 -> false
                       | i  -> sop.RemoveAt(i); true)
        | Seq(seq) -> (match seq.FindIndex(fun ast' -> Ast.MatchSopt s' ast') with
                       | -1 -> false
                       | i  -> (match seq.[i] with
                                | Sop(sop) when sop.Count = 0 -> seq.RemoveAt(i)
                                | _                           -> ());
                               true)
        | _        -> false
        static member Success = function
        | Seq(seq) -> seq.Count = 0 || Seq.forall (Ast.Success) seq
        | Sop(sop) -> sop.Count = 0
        | _        -> true
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
                        pano >>% Ano;
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
                                    seq.[seq.Count - 1] <- Ell(cell, ref false);
                                    ast
               | ast             -> Ell(ast, ref false)
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

exception private InternalException of ErrorMessageList
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
    let asts =
      u'.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
      |> Array.map parseAsync
      |> Async.Parallel
      |> Async.RunSynchronously

    let inspect method' =
      let mutable acc = false in
      for ast in asts do
        acc <- (method' ast) || acc
      done;
      acc
    member __.Parse(argv':string array, args':Arguments.Dictionary) =
      let mutable i = -1 in
      let getNext reason' =
        try i <- i + 1; argv'.[i]
        with :? IndexOutOfRangeException -> raise (InternalException(reason'))
      in let (|Sopt|Other|) (arg':string) =
        if arg'.Length > 1 && arg'.[0] = '-'
        then let mutable i = 1 in
             let opts = GList<Option>() in
             let mutable arg = None in
             while i < arg'.Length do
               (match opts'.Find(arg'.[i]) with
                | null -> ()
                | opt  -> (if opt.HasArgument
                           then if opt.HasDefault
                                then arg <- Some(opt.Default)
                                elif i + 1 = arg'.Length
                                then arg <- Some(getNext (Err.expectedArg opt.ArgName))
                                else arg <- Some(arg'.Substring(i + 1)));
                          opts.Add(opt));
               i <- i + 1
             done;
             Sopt(opts, arg)
        else Other
      in try while true do
          match getNext null with
          | Sopt(sopts, arg) -> for sopt in sopts do
                                  if inspect (Ast.MatchSopt (sopt.Short))
                                  then args'.AddShort(sopt, ?arg'=arg) |> ignore
                                  else raise (InternalException(Err.unexpectedShort sopt))
                                done
          | Other            -> ()
        done;
        args'
      with InternalException(errlist) -> if errlist <> null
                                         then raise (ArgvException(""))
                                         elif Array.exists (Ast.Success) asts
                                         then args'
                                         else "Usage:" + u'
                                              |> ArgvException
                                              |> raise
    member __.Asts = asts
  end
;;
