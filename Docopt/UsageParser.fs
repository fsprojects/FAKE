namespace Docopt
#nowarn "62"
#light "off"

open FParsec
open System
open System.Text

exception private InternalException of ErrorMessageList
exception UsageException of string
exception ArgvException of string

module _Private =
  begin
    let raiseArgvException errlist' =
      let pos = Position(null, 0L, 0L, 0L) in
      let perror = ParserError(pos, null, errlist') in
      raise (ArgvException(perror.ToString()))
    let unexpectedShort = string
                          >> ( + ) "short option -"
                          >> unexpected
    let unexpectedLong = ( + ) "long option --"
                              >> unexpected
    let expectedArg = ( + ) "argument "
                      >> expected
    let unexpectedArg = ( + ) "argument "
                        >> unexpected
    let ambiguousArg = ( + ) "ambiguous long option --"
                       >> unexpected
    let raiseInternal exn' = raise (InternalException exn')
    let raiseUnexpectedShort s' = raiseInternal (unexpectedShort s')
    let raiseUnexpectedLong l' = raiseInternal (unexpectedLong l')
    let raiseExpectedArg a' = raiseInternal (expectedArg a')
    let raiseUnexpectedArg a' = raiseInternal (unexpectedArg a')
    let raiseAmbiguousArg s' = raiseInternal (ambiguousArg s')

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
      | Ell of Ast * bool ref
      | Kln of Ast
      | Xoq of Ast GList
      | Seq of Ast GList
      with
        static member Reduce(ast':Ast, opts':Options) =
          let rec impl = function
          | Sqb(ast)         -> Sqb(impl ast)
          | Req(ast)         -> impl ast
          | Seq(seq) when seq.Count = 1 -> impl (seq.[0])
          | Seq(seq) as ast  -> let mutable i = 0 in
                                while i < seq.Count do
                                  (match seq.[i] with
                                   | Sop(sop) -> let last = sop.[sop.Count - 1] in
                                                 (match opts'.Find(last) with
                                                  | null -> unexpectedShort last
                                                            |> raiseInternal
                                                  | opt  -> if opt.HasArgument && i < seq.Count - 1 && seq.[i + 1].IsArgCase
                                                            then seq.RemoveAt(i + 1))
                                   | _        -> seq.[i] <- impl seq.[i]);
                                  i <- i + 1
                                done;
                                ast
          | Ell(Sqb(ast), _) -> Kln(ast)
          | ast              -> ast
          in impl ast'
        static member MatchSopt(s':char) = function
        | Ano      -> true
        | Sqb(ast) -> Ast.MatchSopt s' ast
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
        static member MatchLopt(l':string) = function
        | Ano      -> true
        | Seq(seq) -> seq.Exists (fun ast' -> Ast.MatchLopt l' ast')
        | _        -> false
        static member Success = function
        | Eps
        | Ano
        | Sqb(_)   -> true
        | Seq(seq) -> seq.Count = 0 || Seq.forall (Ast.Success) seq
        | Sop(sop) -> sop.Count = 0
        | _        -> false
        member xx.IsArgCase = match xx with Arg(_) -> true | _ -> false
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
    let pusageLine = spaces >>. opp.ExpressionParser
  end
;;

open _Private

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
                    | Success(ast, _, _) -> Ast.Reduce(ast, opts')
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
        acc <- acc || (method' ast)
      done;
      acc

    member __.Parse(argv':string array, args':Arguments.Dictionary) =
      let mutable i = -1 in
      let getNext exn' =
        try i <- i + 1; argv'.[i]
        with :? IndexOutOfRangeException -> raiseInternal exn'
      in let (|Sopt|Lopt|Other|) (arg':string) =
        if arg'.Length > 1 && arg'.[0] = '-'
        then if arg'.Length > 2 && arg'.[1] = '-'
             then match arg'.IndexOf('=') with
                  | -1 -> let name = arg'.Substring(2) in
                          (match opts'.Find(name) with
                           | null -> raiseUnexpectedLong name
                           | opt  -> if opt <> opts'.FindLast(name)
                                     then raiseAmbiguousArg name
                                     elif opt.HasArgument
                                     then Lopt(opt, Some(getNext (expectedArg opt.ArgName)))
                                     else Lopt(opt, None))
                  | eq -> let name = arg'.Substring(2, eq - 3) in
                          match opts'.Find(name) with
                          | null -> raiseUnexpectedLong name
                          | opt  -> if opt <> opts'.FindLast(name)
                                    then raiseAmbiguousArg name
                                    elif opt.HasArgument
                                    then Lopt(opt, Some(arg'.Substring(eq + 1)))
                                    else arg'.Substring(eq + 1)
                                         |> raiseUnexpectedArg
             else let mutable i = 1 in
                  let opts = GList<Option>() in
                  let mutable arg = None in
                  while i < arg'.Length do
                    (match opts'.Find(arg'.[i]) with
                     | null -> raiseUnexpectedShort arg'.[i]
                     | opt  -> (if opt.HasArgument
                                then if i + 1 = arg'.Length
                                     then arg <- Some(getNext (expectedArg opt.ArgName))
                                     else (arg <- Some(arg'.Substring(i + 1));
                                           i <- arg'.Length));
                               opts.Add(opt));
                    i <- i + 1
                  done;
                  Sopt(opts, arg)
        else Other
      in try while true do
          match getNext null with
          | Sopt(sopts, arg) -> let mutable i = 1 in
                                while i < sopts.Count do
                                  let sopt = sopts.[i - 1] in
                                  (if inspect (Ast.MatchSopt sopt.Short)
                                   then args'.AddShort(sopt)
                                   else raiseUnexpectedShort sopt.Short);
                                  i <- i + 1
                                done;
                                let sopt = sopts.[i - 1] in
                                if inspect (Ast.MatchSopt sopt.Short)
                                then args'.AddShort(sopt, ?arg'=arg)
                                else raiseUnexpectedShort sopt.Short
          | Lopt(lopt, arg)  -> if inspect (Ast.MatchLopt lopt.Long)
                                then args'.AddLong(lopt, ?arg'=arg)
                                else raiseUnexpectedLong lopt.Long
          | Other            -> ()
        done;
        args'
      with InternalException(errlist) -> if errlist <> null
                                         then raiseArgvException errlist
                                         elif Array.exists (Ast.Success) asts
                                         then args'
                                         else "Usage:" + u'
                                              |> ArgvException
                                              |> raise
    member __.Asts = asts
  end
;;
