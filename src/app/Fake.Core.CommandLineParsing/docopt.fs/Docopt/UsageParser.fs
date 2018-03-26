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

module private Helpers =
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
    let raiseInternal exn' = raise (InternalException exn')
    let raiseUnexpectedShort s' = raiseInternal (unexpectedShort s')
    let raiseUnexpectedLong l' = raiseInternal (unexpectedLong l')
    let raiseUnexpectedArg a' = raiseInternal (unexpectedArg a')
  end

open Helpers

type UsageParser(usageStrings':string array, opts':Options) =
  class
    let mutable isAno = false
    let toIAst obj' = (# "" obj' : IAst #) // maybe #IAst instead of IAst
    let updateUserState (map':'a -> IAst -> #IAst) : 'a -> Parser<IAst, IAst> =
      fun arg' ->
        fun stream' ->
          let res = map' arg' stream'.UserState |> toIAst in
          stream'.UserState <- res;
          Reply(res)
    let isLetterOrDigit c' = isLetter(c') || isDigit(c')
    let opp = OperatorPrecedenceParser<IAst, _, IAst>()
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
    let parg =
      let filterArg arg' (last':IAst) =
        if last' = null
        then Arg(arg') |> toIAst
        elif (last'.Tag = Tag.Sop && (last' :?> Sop).Option.HasArgument)
             || (last'.Tag = Tag.Lop && (last' :?> Lop).Option.HasArgument)
        then Eps.Instance
        else Arg(arg') |> toIAst
      in pupperArg <|> plowerArg
         >>= updateUserState filterArg
    let pano = skipString "[options]"
               >>= updateUserState (fun _ _ -> isAno <- true; Ano(opts'))
    let psdh = skipString "[-]"
               >>= updateUserState (fun _ _ -> Sdh.Instance)
    let psop = let filterSops (sops':string) (last':IAst) =
                 let sops = Options() in
                 let mutable i = -1 in
                 while (i <- i + 1; i < sops'.Length) do
                   match opts'.Find(sops'.[i]) with
                   | null -> sops.Add(Option(short'=sops'.[i]))
                   | opt  -> (if opt.HasArgument && i + 1 < sops'.Length
                              then i <- sops'.Length);
                             if not isAno then sops.Add(opt)
                 done;
                 if sops.Count = 0
                 then Eps.Instance
                 elif last' = null || last'.Tag <> Tag.Sop
                 then Sop(sops) |> toIAst
                 else ((last' :?> Sop).AddRange(sops);
                       Eps.Instance)
               in skipChar '-'
                  >>. many1SatisfyL ( isLetterOrDigit ) "Short option(s)"
                  >>= updateUserState filterSops
    let plop =
      let filterLopt (lopt':string, arg':string Option) _ =
        match opts'.Find(lopt') with
        | null -> Lop(Option(long'=lopt', ?argName'=arg')) |> toIAst
        | lopt -> if isAno then Eps.Instance else Lop(lopt) |> toIAst
      in skipString "--"
         >>. manySatisfy (fun c' -> Char.IsLetterOrDigit(c') || c' = '-')
        .>>. opt (skipChar '=' >>. (plowerArg <|> pupperArg))
         >>= updateUserState filterLopt
    let psqb = between (skipChar '[' >>. spaces) (skipChar ']')
                       opp.ExpressionParser
               >>= updateUserState (fun ast' _ -> Sqb(ast'))
    let preq = between (skipChar '(' >>. spaces) (skipChar ')')
                       opp.ExpressionParser
               >>= updateUserState (fun ast' _ -> Req(ast'))
    let pcmd = many1Satisfy (fun c' -> isLetter(c') || isDigit(c') || c' = '-')
               >>= updateUserState (fun cmd' _ -> Cmd(cmd'))
    let term = choice [|pano;
                        psdh;
                        plop;
                        psop;
                        psqb;
                        preq;
                        parg;
                        pcmd|]
    let pxor = let afterStringParser =
                 spaces
                 .>> updateUserState (fun _ _ -> Xor()) ()
               in InfixOperator("|", afterStringParser, 10, Associativity.Left,
                                fun x' y' -> Xor(x', y') |> toIAst)
    let pell = let afterStringParser =
                 spaces .>> updateUserState (fun _ _ -> Ell(Eps.Instance)) ()
               in let makeEll (ast':IAst) =
                 match ast'.Tag with
                 | Tag.Seq -> let seq = (ast' :?> Seq).Asts in
                              let cell = seq.[seq.Count - 1] in
                              seq.[seq.Count - 1] <- Ell(cell);
                              ast'
                 | _       -> Ell(ast') |> toIAst
               in PostfixOperator("...", afterStringParser, 20, false, makeEll)
    let _ = opp.TermParser <-
      sepEndBy1 term spaces1
      >>= updateUserState (fun ast' _ ->
                             match ast' |> List.filter (fun ast' -> ast'.Tag <> Tag.Eps) with
                             | []    -> Eps.Instance
                             | [ast] -> ast
                             | list  -> Seq(GList<IAst>(list)) |> toIAst)
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
    let pusageLine = spaces >>. opp.ExpressionParser

    let parseAsync = function
    | ""   -> async { return Eps.Instance }
    | line -> async {
        let line = line.TrimStart() in
        let index = line.IndexOfAny([|' ';'\t'|]) in
        return if index = -1 then Eps.Instance
               else let line = line.Substring(index) in
                    match runParserOnString pusageLine null "" line with
                    | Success(ast, _, _) -> ast
                    | Failure(err, _, _) -> raise (UsageException(err))
      }
    let asts =
      usageStrings'
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
      let mutable res = Unchecked.defaultof<string> in
      let folder acc' (ast':IAst) =
        res <- ast'.MatchSopt(names', getArg');
        Sop.Success res || acc'
      in if not (Array.fold folder false asts)
      then raiseUnexpectedShort (if res = null then names' else res).[0]

    let matchLopt (name':string) getArg' =
      let folder acc' (ast':IAst) =
        ast'.MatchLopt(name', getArg') || acc'
      in if not (Array.fold folder false asts)
      then raiseUnexpectedLong name'

    let matchArg (str':string) =
      let folder acc' (ast':IAst) =
        ast'.MatchArg(str') || acc'
      in if not (Array.fold folder false asts)
      then raiseUnexpectedArg str'

    let tryFill (args':Arguments.Dictionary) =
      let predicate (ast':IAst) =
        let args = Arguments.Dictionary() in
        match ast'.TryFill(args) with
        | false -> false
        | _     -> args'.AddRange(args);
                   true
      in Array.exists predicate asts

    member __.Parse(argv':string array, args':Arguments.Dictionary) =
      i <- -1;
      argv <- argv';
      args <- args';
      let (|Sopt|Lopt|Argument|) (arg':string) =
        if arg'.Length > 1 && arg'.[0] = '-'
        then if arg'.Length > 2 && arg'.[1] = '-'
             then match arg'.IndexOf('=') with
                  | -1 -> let name = arg'.Substring(2) in
                          let getArg =
                            let s = ref String.Empty in
                            let arg = lazy getNext (expectedArg !s) in
                            fun s' -> s := s'; arg.Value
                          in Lopt(name, getArg)
                  | eq -> let name = arg'.Substring(2, eq - 3) in
                          let arg = arg'.Substring(eq + 1) in
                          let getArg _ = arg in
                          Lopt(name, getArg)
             else let names = arg'.Substring(1) in
                  let getArg = getNext << expectedArg in
                  Sopt(names, getArg)
        else Argument(arg')
      in try
        while true do
          match getNext null with
          | Sopt(names, getArg) -> matchSopt names getArg
          | Lopt(name, getArg)  -> matchLopt name getArg
          | Argument(str)       -> matchArg str
        done;
        args'
      with InternalException(errlist) ->
        if errlist <> null
        then raiseArgvException errlist
        elif tryFill args'
        then (args'.RegisterDefaults(opts'); args')
        else raise (ArgvException("Usage:" + String.Join("\n", usageStrings')))
    member __.Asts = asts
  end
