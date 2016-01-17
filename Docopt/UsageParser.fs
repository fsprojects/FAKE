namespace Docopt
#nowarn "42"
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
    let inline toIAst obj' = (# "" obj' : IAst #)
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
    let mutable last:IAst = null
    let updatelastAst ast' = last <- ast'; ast'
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
               |>> fun arg' -> if last.Tag = Tag.Sop
                               then Eps() |> toIAst
                               else Arg(arg') |> toIAst
    let pano = skipString "[options]"
               |>> fun () -> Ano(opts) |> toIAst
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
                 Sop(sops) |> toIAst
               in skipChar '-'
                  >>. many1SatisfyL ( isLetterOrDigit ) "Short option(s)"
                  |>> filterSops
    let psqb = between (skipChar '[' >>. spaces) (skipChar ']')
                       opp.ExpressionParser
               |>> (Sqb >> toIAst)
    let preq = between (skipChar '(' >>. spaces) (skipChar ')')
                       opp.ExpressionParser
               |>> (Req >> toIAst)
    let pcmd = many1Satisfy (fun c' -> isLetter(c') || isDigit(c') || c' = '-')
               >>% (Eps() |> toIAst) //|>> (Cmd >> toIAst)
    let term = choice [|pano;
                        psop;
                        psqb;
                        preq;
                        parg;
                        pcmd|]
               |>> updatelastAst
    let pxor = InfixOperator("|", spaces, 10, Associativity.Left,
                             fun x' y' -> updatelastAst (Xor(x', y') |> toIAst))
//    let pell = let makeEll (ast':IAst) =
//                 match ast'.Tag with
//                 | Tag.Seq -> let cell = seq.[seq.Count - 1] in
//                              seq.[seq.Count - 1] <- Ell(cell, ref false);
//                              ast
//                 | _       -> Eps() //Ell(ast')
//               in PostfixOperator("...", spaces, 20, false,
//                                  makeEll >> updatelastAst)
    let _ = opp.TermParser <- sepEndBy1 term spaces1
                              |>> (GList<IAst> >> Seq >> toIAst)
    let _ = opp.AddOperator(pxor)
//    let _ = opp.AddOperator(pell)
    let pusageLine = spaces >>. opp.ExpressionParser
  end
;;

open _Private

type UsageParser(u':string, opts':Options) =
  class
    do opts <- opts'
    let parseAsync = function
    | ""   -> async { return Eps() |> toIAst }
    | line -> async {
        let line = line.TrimStart() in
        let index = line.IndexOfAny([|' ';'\t'|]) in
        return if index = -1 then Eps() |> toIAst
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

    let mutable i = Unchecked.defaultof<int>
    let mutable argv = Unchecked.defaultof<string array>
    let mutable args = Unchecked.defaultof<Arguments.Dictionary>
    let getNext exn' =
      try i <- i + 1; argv.[i]
      with :? IndexOutOfRangeException -> raiseInternal exn'

    let matchSopt (names':string) getArg' =
      for ast in asts do
        if not (ast.MatchSopt(names', getArg'))
        then raiseUnexpectedShort '?'
      done

    let matchLopt (name':string) getArg' =
      for ast in asts do
        if not (ast.MatchLopt(name', getArg'))
        then raiseUnexpectedLong name'
      done

    let matchArg (str:string) =
      for ast in asts do
        if not (ast.MatchArg(str))
        then raiseUnexpectedArg str
      done

    member __.Parse(argv':string array, args':Arguments.Dictionary) =
      i <- -1;
      argv <- argv';
      args <- args';
      let (|Sopt|Lopt|Argument|) (arg':string) =
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
        else Argument(arg')
      in try while true do
          match getNext null with
          | Sopt(names, getArg) -> matchSopt names getArg
          | Lopt(name, getArg)  -> matchLopt name getArg
          | Argument(str)       -> matchArg str
        done;
        args'
      with InternalException(errlist) ->
        if errlist <> null
        then raiseArgvException errlist
        elif Array.exists (fun (ast':IAst) -> ast'.TryFill(args')) asts
        then args'
        else raise (ArgvException("Usage:" + u'))
    member __.Asts = asts
  end
;;
