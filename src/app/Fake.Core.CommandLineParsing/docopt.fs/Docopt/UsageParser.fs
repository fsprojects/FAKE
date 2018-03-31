namespace Docopt

open FParsec
open System
open System.Text

exception private InternalException of ErrorMessageList
exception UsageException of string
  with override x.ToString () = sprintf "UsageException: %s" x.Data0
exception ArgvException of string
  with override x.ToString () = sprintf "ArgvException: %s" x.Data0

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
open FParsec

type TempResultMap = Map<string, Docopt.Arguments.Result list>
type ResultMap = Map<string, Docopt.Arguments.Result>
type UsageParserState =
  { AllowOptions : bool
    Result : ResultMap }
type ArgumentStreamState<'TUserState> = { Pos : int; StateTag : int64; UserState : 'TUserState }
type ArgumentStream<'item, 'TUserState>(argv:'item array, initState:'TUserState) =
  let mutable pos = 0
  let mutable stateTag = 0L
  let mutable state = initState
  let markChange () =
    stateTag <- stateTag + 1L
  let inc () =
    markChange()
    if pos < argv.Length then pos <- pos + 1
  let current () =
    if pos < argv.Length then Some argv.[pos] else None
  member x.CurrentState = { Pos = pos; StateTag = stateTag; UserState = state}
  member x.Position = pos
  member x.RestoreState (oldState:ArgumentStreamState<'TUserState>) =
    pos <- oldState.Pos
    stateTag <- oldState.StateTag
    state <- oldState.UserState
  member x.StateTag = stateTag
  member x.UserState 
    with get () = state
    and set v = markChange (); state <- v
  member x.Seek newPos =
    markChange()
    pos <- newPos
  member x.Peek () = current()
  member x.IsEnd = pos = argv.Length
  member x.Skip () = inc()
  member x.SkipAndPeek () = x.Skip(); x.Peek()
  member x.Read () =
    let va = current()
    inc()
    va

  override x.ToString() =
    sprintf "Pos: %d, %A, state: %A" pos argv state
type ArgumentParser<'TItem, 'TUserState, 'TResult> = ArgumentStream<'TItem, 'TUserState> -> Reply<'TResult>

module ArgParser =
  let preturn x : ArgumentParser<_,_,_> = fun stream -> Reply(x)
  let pzero : ArgumentParser<_,_,_> = fun stream -> Reply(Error, FParsec.Error.NoErrorMessages)

  let (>>=) (p: ArgumentParser<'i, 'u, 'a>) (f: 'a -> ArgumentParser<'i, 'u, 'b>) : ArgumentParser<_,_,_> =
    fun stream ->
        let reply1 = p stream
        if reply1.Status = Ok then
            let p2 = f reply1.Result
            let stateTag = stream.StateTag
            let mutable reply2 = p2 stream
            if stateTag = stream.StateTag && reply2.Status = Error then
                reply2.Error <- mergeErrors reply1.Error reply2.Error
            reply2
        else
            Reply(reply1.Status, reply1.Error)
  let (>>%) p x = p >>= fun _ -> preturn x          
  let (>>.) p1 p2 = p1 >>= fun _ -> p2
  let (.>>) p1 p2 = p1 >>= fun x -> p2 >>% x
  let (.>>.) p1 p2 = p1 >>= fun a -> p2 >>= fun b -> preturn (a, b)
  let between (popen: ArgumentParser<_,_,'u>) (pclose: ArgumentParser<_, 'u,_>) (p: ArgumentParser<_, 'u,_>) =
     popen >>. p .>> pclose
  let (|>>) p f = p >>= fun x -> preturn (f x)  


  let (<?>) (p: ArgumentParser<_,'u,'a>) label  : ArgumentParser<_,'u,'a> =
      let error = expected label
      fun stream ->
          let stateTag = stream.StateTag
          let mutable reply = p stream
          if stateTag = stream.StateTag then
              reply.Error <- error
          reply

  let choice (ps : seq<ArgumentParser<_, 'u, 'a>>) : ArgumentParser<_, _, _> =
    fun (stream:ArgumentStream<_,_>) ->
       use iter = ps.GetEnumerator()
       if iter.MoveNext() then
           let state = stream.CurrentState 
           let stateTag = stream.StateTag
           let mutable error = NoErrorMessages
           let mutable reply = iter.Current stream
           while reply.Status = Error && iter.MoveNext() do
               if stateTag <> stream.StateTag then
                  stream.RestoreState state
               error <- mergeErrors error reply.Error
               reply <- iter.Current stream
           if stateTag = stream.StateTag && reply.Status = Error then
               error <- mergeErrors error reply.Error
               reply.Error <- error
           reply
       else Reply()

  let (<|>) p1 p2 = choice [p1;p2]


  [<Sealed>]
  type Inline =
    [<NoDynamicInvocation>]
    static member inline
                       Many(stateFromFirstElement,
                            foldState,
                            resultFromState,
                            elementParser: ArgumentParser<_,_,_>,
                            ?firstElementParser: ArgumentParser<_,_,_>,
                            ?resultForEmptySequence) : ArgumentParser<_,_,_> =
      fun stream ->
        let mutable stateTag = stream.StateTag
        let firstElementParser = match firstElementParser with Some p -> p | _ -> elementParser
        let mutable reply = firstElementParser stream
        if reply.Status = Ok then
            let mutable xs = stateFromFirstElement reply.Result
            let mutable error = reply.Error
            stateTag <- stream.StateTag
            reply <- elementParser stream
            while reply.Status = Ok do
                if stateTag = stream.StateTag then
                    failwithf "infiniteLoopException %A" stream
                xs    <- foldState xs reply.Result
                error <- reply.Error
                stateTag <- stream.StateTag
                reply <- elementParser stream
            if reply.Status = Error && stateTag = stream.StateTag then
                error <- mergeErrors error reply.Error
                Reply(Ok, resultFromState xs, error)
            else
                error <- if stateTag <> stream.StateTag then reply.Error
                         else mergeErrors error reply.Error
                Reply(reply.Status, error)
        else
            match resultForEmptySequence with
            | Some _ (* if we bind f here, fsc won't be able to inline it *)
              when reply.Status = Error && stateTag = stream.StateTag ->
                Reply(Ok, (match resultForEmptySequence with Some f -> f() | _ -> Unchecked.defaultof<_>), reply.Error)
            | _ ->
                Reply(reply.Status, reply.Error)

  let many p = Inline.Many((fun x -> [x]), (fun xs x -> x::xs), List.rev, p, resultForEmptySequence = fun () -> [])
  let many1 p = Inline.Many((fun x -> [x]), (fun xs x -> x::xs), List.rev, p)

  let pseq (ps : seq<ArgumentParser<_, _, _>>) : ArgumentParser<_, _, _> =
    Seq.fold (>>.) (preturn Map.empty) ps

  let chooseParser itemType chooser =
    fun (stream:ArgumentStream<_,_>) ->
        match chooser (stream.Peek()) with
        | Some result ->
          stream.Skip()
          Reply(result)
        | None ->
          let e1 = expected itemType
          let e2 = unexpected (sprintf "%A" (stream.Peek()))
          let error = mergeErrors e1 e2
          Reply(ReplyStatus.Error, error)

  let chooseParser' itemType chooser =
    let choose arg =
      match arg with
      | Some a -> chooser a
      | None -> None
    chooseParser itemType choose


  let pcmd cmd =
      let chooseCmd arg =
        if arg = cmd then Some cmd else None
      chooseParser' (sprintf "Command '%s'" cmd) chooseCmd

  let pLongFlag (flag:SafeOption) =
      let chooseCmd arg =
        match arg with
        | Some arg when arg = flag.FullLong -> Some arg
        | _ -> None
      chooseParser (sprintf "Flag '%s'" flag.FullLong) chooseCmd

  let pLongFlagWithArg (flag:SafeOption) =
      let chooseCmd arg =
        match arg with
        | Some (arg:string) when arg.StartsWith (flag.FullLong + "=") -> Some (arg.Substring (flag.FullLong.Length + 1))
        | _ -> None
      chooseParser (sprintf "Flag '%s='" flag.FullLong) chooseCmd
  let parg argName = chooseParser (sprintf "Argument for  '%s'" argName) id

  let updateUserState (map':'a -> ResultMap -> ResultMap) : 'a -> ArgumentParser<_, ResultMap, ResultMap> =
      fun arg' ->
        fun stream' ->
          let res = map' arg' stream'.UserState
          stream'.UserState <- res
          Reply(res)

  let debug (map':'a -> ArgumentStream<'item,'state> -> unit) : 'a -> ArgumentParser<'item, 'state, 'a> =
      fun arg' ->
        fun stream' ->
          map' arg' stream'
          Reply(arg')

  let saveInMap key f = 
    updateUserState (fun item map ->
      let newItem = f item
      match Map.tryFind key map, newItem with
      | None, _
      | Some Docopt.Arguments.Result.None, _ -> Map.add key newItem map
      | _, Docopt.Arguments.Result.None -> map
      | Some (Docopt.Arguments.Result.Argument arg1), Docopt.Arguments.Result.Argument arg2 ->
          Map.add key (Docopt.Arguments.Result.Arguments [arg1; arg2]) map
      | Some (Docopt.Arguments.Result.Argument arg1), Docopt.Arguments.Result.Arguments argList ->
          Map.add key (Docopt.Arguments.Result.Arguments (arg1 :: argList)) map
      | Some (Docopt.Arguments.Result.Arguments argList1), Docopt.Arguments.Result.Argument arg2 ->
          Map.add key (Docopt.Arguments.Result.Arguments (argList1 @ [arg2])) map
      | Some (Docopt.Arguments.Result.Arguments argList1),  Docopt.Arguments.Result.Arguments argList2 ->
          Map.add key (Docopt.Arguments.Result.Arguments (argList1 @ argList2)) map
      | Some (Docopt.Arguments.Result.Flag), Docopt.Arguments.Result.Flag ->
          Map.add key (Docopt.Arguments.Result.Flags 2) map
      | Some (Docopt.Arguments.Result.Flags n1), Docopt.Arguments.Result.Flag ->
          Map.add key (Docopt.Arguments.Result.Flags (n1 + 1)) map
      | Some (Docopt.Arguments.Result.Flag), Docopt.Arguments.Result.Flags n2 ->
          Map.add key (Docopt.Arguments.Result.Flags (n2 + 1)) map
      | Some (Docopt.Arguments.Result.Flags n1), Docopt.Arguments.Result.Flags n2 ->
          Map.add key (Docopt.Arguments.Result.Flags (n1 + n2)) map
      | Some v, _ -> failwithf "Cannot add value %A as %s -> %A already exists in the result map" newItem key v)
  let mergeMap m1 m2 =
    Map.fold (fun s k v -> Map.add k v s) m1 m2
  let mergeMaps maps =
    Seq.fold mergeMap Map.empty maps      
  let rec getParser (ast:UsageAst) : ArgumentParser<string, _, _> =
    let p = 
      match ast with
      | UsageAst.Eps -> preturn Map.empty
      | UsageAst.Ano (title, o') ->
        pzero <?> "Option annotation is not supported yet"
        //CharParsers.
      | UsageAst.Sop o' ->
        //o'.
        pzero <?> "Short options are not supported yet"
      | UsageAst.Lop o' ->
        if o'.HasArgument then
          (pLongFlag o'
           >>. parg o'.FullLong) <|> pLongFlagWithArg o'
          >>= saveInMap o'.FullLong (Arguments.Result.Argument)
        else
          pLongFlag o'
          >>= saveInMap o'.FullLong (fun _ -> Arguments.Result.Flag)
      | UsageAst.Sqb ast' ->
        getParser ast' <|> preturn Map.empty
      | UsageAst.Req ast' ->
        getParser ast'
      | UsageAst.Arg name' ->
        parg name'
        >>= saveInMap (name') (Arguments.Result.Argument)
      | UsageAst.XorEmpty -> preturn Map.empty
      | UsageAst.Xor (l', r') ->
        getParser l' <|> getParser r'
      | UsageAst.Seq asts' ->
        asts'
        |> Seq.map getParser
        |> pseq
      | UsageAst.Cmd cmd' -> 
        pcmd cmd'
        >>= saveInMap cmd' (fun _ -> Arguments.Result.Command)
      | UsageAst.Ell (UsageAst.Sqb ast') ->
        // Allow zero matches
        many (getParser ast')
        >>= updateUserState (fun st state ->
          printfn "many state: %A ||| %A" state st
          state)
      | UsageAst.Ell ast' ->
        // One or more
        many1 (getParser ast')
        >>= updateUserState (fun st state ->
          printfn "many1 state: %A ||| %A" state st
          state)
      | UsageAst.Sdh ->
        pcmd "-"
        >>= saveInMap "-" (fun _ -> Arguments.Result.Command)
    (debug (fun _ stream ->
      printfn ">>>> STARTING ast %A, state: %A" ast stream) ())
    >>. p      
    >>= debug (fun result stream ->
      printfn ">>>> FINISHED ast %A, state: %A, result: %A" ast stream result)
type UsageParser(usageStrings':string array, sections:Collections.Generic.IDictionary<string, SafeOptions>) =
    //let opts' = sections.["options"]
    //let mutable isAno = false
    //let toIAst obj' = (# "" obj' : UsageAst #) // maybe #IAst instead of IAst
    let updateUserState (map':'a -> UsageAstCell -> UsageAstCell) : 'a -> Parser<UsageAstCell, UsageAstCell> =
      fun arg' ->
        fun stream' ->
          let res = map' arg' stream'.UserState in
          stream'.UserState <- res;
          Reply(res)
    let isLetterOrDigit c' = isLetter(c') || isDigit(c')
    let opp = OperatorPrecedenceParser<UsageAstCell, _, UsageAstCell>()
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
      let filterArg arg' (last':UsageAstCell) =
        if obj.ReferenceEquals(null, last')
        then UsageAstBuilder.Arg(arg')
        elif (match last'.Content with Some (UsageAstBuilder.Sop opts) -> opts.Last.HasArgument | _ -> false)
             || (match last'.Content with Some(UsageAstBuilder.Lop opt) -> opt.HasArgument | _ ->  false)
        then UsageAstBuilder.Eps
        else UsageAstBuilder.Arg(arg')
        |> UsageAstCell.FromBuilder
      in pupperArg <|> plowerArg
         >>= updateUserState filterArg
    let pano (title, so:SafeOptions) =
      skipString (sprintf "[%s]" title)
      >>= updateUserState (fun _ _ -> UsageAstBuilder.Ano(title, so) |> UsageAstBuilder.ToCell)
    let psdh = skipString "[-]"
               >>= updateUserState (fun _ _ ->  UsageAstBuilder.Sdh |> UsageAstBuilder.ToCell)
    let psop = let filterSops (sops':string) (last':UsageAstCell) =
                 let sops = ResizeArray<SafeOption>() in
                 let mutable i = -1 in
                 while (i <- i + 1; i < sops'.Length) do
                   sops.Add({SafeOption.Empty with Short = Some sops'.[i] })
                   //match opts'.Find(sops'.[i]) with
                   //| None -> sops.Add({SafeOption.Empty with Short = Some sops'.[i] })
                   //| Some opt ->
                   //   (if opt.HasArgument && i + 1 < sops'.Length
                   //    then i <- sops'.Length);
                   //    sops.Add(opt)
                 done;
                 if sops.Count = 0
                 then UsageAstBuilder.Eps
                 else match last'.Content with
                      | Some (UsageAstBuilder.Sop list) ->
                        last'.Content <- Some (UsageAstBuilder.Sop (list.AddRange(sops |> List.ofSeq)))
                        UsageAstBuilder.Eps
                      | _ -> UsageAstBuilder.Sop(SafeOptions(sops |> Seq.toList))
                |> UsageAstCell.FromBuilder
               in skipChar '-'
                  >>. many1SatisfyL ( isLetterOrDigit ) "Short option(s)"
                  >>= updateUserState filterSops
    let plop =
      let filterLopt (lopt':string, arg':string Option) _ =
        UsageAstBuilder.Lop({SafeOption.Empty with Long = Some lopt'; ArgumentName = arg'})
        //match opts'.Find(lopt') with
        //| None -> UsageAstBuilder.Lop({SafeOption.Empty with Long = Some lopt'; ArgumentName = arg'})
        //| Some lopt -> UsageAstBuilder.Lop(lopt)
        |> UsageAstCell.FromBuilder
      in skipString "--"
         >>. manySatisfy (fun c' -> Char.IsLetterOrDigit(c') || c' = '-')
        .>>. opt (skipChar '=' >>. (plowerArg <|> pupperArg))
         >>= updateUserState filterLopt
    let psqb = between (skipChar '[' >>. spaces) (skipChar ']')
                       opp.ExpressionParser
               >>= updateUserState (fun ast' _ -> UsageAstBuilder.Sqb(ast')|> UsageAstCell.FromBuilder)
    let preq = between (skipChar '(' >>. spaces) (skipChar ')')
                       opp.ExpressionParser
               >>= updateUserState (fun ast' _ -> UsageAstBuilder.Req(ast')|> UsageAstCell.FromBuilder)
    let pcmd = many1Satisfy (fun c' -> isLetter(c') || isDigit(c') || c' = '-')
               >>= updateUserState (fun cmd' _ -> UsageAstBuilder.Cmd(cmd')|> UsageAstCell.FromBuilder)
    let panoParsers =
      sections |> Seq.map (fun kv -> pano(kv.Key, kv.Value)) |> List.ofSeq
    let term = choice (Seq.append panoParsers [|
                        psdh;
                        plop;
                        psop;
                        psqb;
                        preq;
                        parg;
                        pcmd|])
    let pxor = let afterStringParser =
                 spaces
                 .>> updateUserState (fun _ _ -> UsageAstBuilder.XorEmpty|> UsageAstCell.FromBuilder) ()
               in InfixOperator("|", afterStringParser, 10, Associativity.Left,
                                fun x' y' -> UsageAstBuilder.Xor(x', y') |> UsageAstCell.FromBuilder)
    let pell = let afterStringParser =
                 spaces .>> updateUserState (fun _ _ -> UsageAstBuilder.Ell(UsageAstBuilder.Eps|> UsageAstCell.FromBuilder)|> UsageAstCell.FromBuilder) ()
               let makeEll (ast':UsageAstCell) =
                 match ast'.Content with
                 | Some (UsageAstBuilder.Seq seq) -> 
                    let cell = seq |> List.last
                    cell.Content <-                     
                      match cell.Content with
                      | Some c -> 
                        Some (UsageAstBuilder.Ell (UsageAstCell.FromBuilder c))
                      | None -> Some <| UsageAstBuilder.Ell(UsageAstBuilder.Eps|> UsageAstCell.FromBuilder)           
                    ast'
                 | _       -> UsageAstBuilder.Ell(ast')|> UsageAstCell.FromBuilder
               in PostfixOperator("...", afterStringParser, 20, false, makeEll)
    let _ = 
      opp.TermParser <-
        sepEndBy1 term spaces1
        >>= updateUserState (fun ast' _ ->
                               match ast' |> List.filter (fun ast' -> ast'.Content.IsSome && ast'.Content.Value.UsageTag <> Tag.Eps) with
                               | []    -> UsageAstBuilder.Eps|> UsageAstCell.FromBuilder
                               | [ast] -> ast
                               | list  -> UsageAstBuilder.Seq(list)|> UsageAstCell.FromBuilder
                              )
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
    let pusageLine = spaces >>. opp.ExpressionParser

    let parseAsync = function
    | ""   -> async { return UsageAst.Eps }
    | line -> async {
        let line = line.TrimStart() in
        let index = line.IndexOfAny([|' ';'\t'|]) in
        return if index = -1 then UsageAst.Eps
               else let line = line.Substring(index) in
                    match runParserOnString pusageLine { Content = None } "" line with
                    | Success(ast, _, _) -> ast.Build()
                    | Failure(err, _, _) -> raise (UsageException(err))
      }
    let asts =
      usageStrings'
      |> Array.map parseAsync
      |> Async.Parallel
      |> Async.RunSynchronously

(*
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
      then raiseUnexpectedShort (if isNull res then names' else res).[0]

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
*)
    let getAstParser =
      let (>>.) = ArgParser.(>>.)
      let (>>=) = ArgParser.(>>=)  
      asts
      |> Seq.map ArgParser.getParser
      |> ArgParser.choice
      >>= ArgParser.updateUserState (fun _ state -> state)
      
    member __.ParseCommandLine (argv) =
      let state = ArgumentStream(argv, Map.empty)
      let reply = getAstParser state
      let errors = ErrorMessageList.ToSortedArray(reply.Error)
      let parseError = ParserError(Position("argv", int64 state.Position,0L,int64 state.Position), state.UserState, reply.Error)
      let errorText =
        use sw = new System.IO.StringWriter()
        parseError.WriteTo(sw)
        sw.ToString()
      match reply.Status = ReplyStatus.Ok, errors, state.IsEnd with
      | true, [||], true -> reply.Result
      | _, _ , true -> raise <| ArgvException (sprintf "errors %A: %s" reply.Status errorText)
      | _, [||], false ->
          let unparsed = argv.[state.Position..argv.Length - 1]
          raise <| ArgvException (sprintf "'%A' could not be parsed" unparsed)
      | _ ->
          let unparsed = argv.[state.Position..argv.Length - 1]
          raise <| ArgvException (sprintf "errors: %s, ('%A' could not be parsed)" errorText unparsed)

(*
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
        else raise (ArgvException("Usage:" + String.Join("\n", usageStrings')))*)
    member __.Asts = asts
