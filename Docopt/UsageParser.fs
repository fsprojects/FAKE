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

    let mutable opts = null
    let mutable lastAst = Eps
    let updatelastAst ast' = lastAst <- ast'; ast'
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
               |>> fun arg' -> if lastAst.IsSopCase then Eps else Arg(arg')
    let pano = skipString "[options]" |>> fun () -> Ano(opts)
    let psop = let filterSops (sops':string) =
                 let sops = Options() in
                 let mutable i = -1 in
                 while (i <- i + 1; i < sops'.Length) do
                   match opts.Find(sops'.[i]) with
                   | null -> raiseUnexpectedShort sops'.[i]
                   | opt  -> (if opt.HasArgument && i + 1 < sops'.Length
                              then i <- sops'.Length);
                             sops.Add(opt)
                 done;
                 Sop(sops)
               in skipChar '-'
                  >>. many1SatisfyL ( isLetterOrDigit ) "Short option(s)"
                  |>> filterSops
    let psqb = between (skipChar '[' >>. spaces) (skipChar ']')
                       opp.ExpressionParser
               |>> Sqb
    let preq = between (skipChar '(' >>. spaces) (skipChar ')')
                       opp.ExpressionParser
               |>> Req
    let pcmd = many1Satisfy (fun c' -> isLetter(c') || isDigit(c') || c' = '-')
               |>> Cmd
    let term = choice [|pano;
                        psop;
                        psqb;
                        preq;
                        parg;
                        pcmd|]
               |>> updatelastAst
    let pxor = InfixOperator("|", spaces, 10, Associativity.Left,
                             fun x' y' -> updatelastAst (Xor(x', y')))
    let pell = let makeEll = function
               | Seq(seq) as ast -> let cell = seq.[seq.Count - 1] in
                                    seq.[seq.Count - 1] <- Ell(cell, ref false);
                                    ast
               | ast             -> Ell(ast, ref false)
               in PostfixOperator("...", spaces, 20, false,
                                  makeEll >> updatelastAst)
    let _ = opp.TermParser <- sepEndBy1 term spaces1 |>> (GList<Ast> >> Seq)
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
    let pusageLine = spaces >>. opp.ExpressionParser
  end
;;

open _Private

type UsageParser(u':string, opts':Options) =
  class
    do opts <- opts'
    let parseAsync = function
    | ""   -> async { return Eps }
    | line -> async {
        let line = line.TrimStart() in
        let index = line.IndexOfAny([|' ';'\t'|]) in
        return if index = -1 then Eps
               else match run (spaces >>. opp.ExpressionParser)
                              (line.Substring(index)) with
                    | Success(ast, _, _) -> Ast.Reduce ast
                    | Failure(err, _, _) -> raise (UsageException(err))
      }
    let asts =
      u'.Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
      |> Array.map parseAsync
      |> Async.Parallel
      |> Async.RunSynchronously

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
                          let getArg = getNext << expectedArg in
                          Lopt(name, getArg)
                  | eq -> let name = arg'.Substring(2, eq - 3) in
                          let arg = arg'.Substring(eq + 1) in
                          let getArg _ = arg in
                          Lopt(name, getArg)
             else let names = arg'.Substring(1) in
                  let getArg = getNext << expectedArg in
                  Sopt(names, getArg)
        else Other
      in try while true do
          match getNext null with
          | Sopt(names, getArg) -> let mutable i = 0 in
                                   for ast in asts do
                                     i <- 0;
                                     while i < names.Length do
                                       (match Ast.MatchSopt(names.[i], ast) with
                                        | null -> raiseUnexpectedShort names.[i]
                                        | opt  -> let arg = if opt.HasArgument && i + 1 = names.Length
                                                            then Some(getArg opt.ArgName)
                                                            elif opt.HasArgument
                                                            then let j = i in
                                                                 i <- names.Length;
                                                                 Some(names.Substring(j + 1))
                                                            else None in
                                                  args'.AddShort(opt, ?arg'=arg));
                                       i <- i + 1
                                     done
                                   done
          | Lopt(name, getArg)  -> for ast in asts do
                                     match Ast.MatchLopt(name, ast) with
                                     | null -> raiseUnexpectedLong name
                                     | opt  -> (if opts'.FindLast(name) <> opt
                                                then raiseAmbiguousArg (argv'.[i].Substring(2)));
                                               let arg = if opt.HasArgument
                                                         then Some(getArg opt.ArgName)
                                                         else None in
                                               args'.AddLong(opt, ?arg'=arg)
                                   done
          | Other               -> ()
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
