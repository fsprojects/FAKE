module public Docopt.Parser
#nowarn "62"
#light "off"

open FParsec

type UserState = unit
;;

type Parser<'t> = Parser<'t, UserState>
;;

let pupperArg:Parser<string> =
  let start c' = isUpper c' || isDigit c' in
  let cont c' = start c' || c' = '-' in
  identifier (IdentifierOptions(isAsciiIdStart=start,
                                isAsciiIdContinue=cont,
                                label="UPPER-CASE identifier"))
;;

let plowerArg:Parser<string> =
  satisfyL (( = ) '<') "<lower-case> identifier"
  >>. many1SatisfyL (( <> ) '>') "any character except '>'"
  .>> pchar '>'
;;

let parg:Parser<Token.Argument> =
  let pidentifier = pupperArg <|> plowerArg in
  let ptype = many1SatisfyL (fun c' -> c' <> ' ' && c' <> ',') "F# type" in
  let poptType = opt (skipChar ':' >>. ptype) in
  pipe2 pidentifier poptType (fun a' b' -> Token.Argument(a', b'))
;;

type PoptDescLine(soptChars':string, raise':bool) =
  class
    let replyErr err' = Reply(Error, ErrorMessageList(err'))

    let loptPredicate c' = isLetter(c') || isDigit(c') || c' = '-'

    let rec ``start`` (stream':CharStream<_>) =
      let _ = stream'.SkipWhitespace() in
      if stream'.Match('-')
      then ``short or long option`` stream'
      else replyErr(Expected("'-'"))

    and``short or long option`` stream' =
      let c = stream'.SkipAndPeek() in
      if isLetter(c) || (isAnyOf soptChars') c
      then ``short option`` stream' (c, null, None)
      elif c = '-' then ``expecting long option`` stream' (EOS, null, None)
      else replyErr(Expected(sprintf "letters or any of «%s»" soptChars'))

    and ``short option`` stream' tuple' =
      match stream'.SkipAndPeek() with
        | ' ' -> ``argument or hyphens`` stream' tuple'
        | '=' -> ``expecting arg (short)`` stream' tuple'
        | ',' -> ``expecting space`` stream' tuple'
        | _   -> Reply(tuple')

    and ``expecting arg (short)`` stream' (s', l', _) =
      let () = stream'.Skip() in
      let r = parg stream' in match r.Status with
        | Ok -> ``short option plus arg`` stream' (s', l', Some(r.Result))
        | _  -> Reply(Error, r.Error)

    and ``argument or hyphens`` stream' (s', l', a') =
      if stream'.SkipAndPeek() = '-'
      then ``expecting hyphen 2`` stream' (s', l', a')
      else let r = parg stream' in match r.Status with
        | Ok -> ``short option plus arg`` stream' (s', l', Some(r.Result))
        | _  -> Reply(Error, ErrorMessageList(Expected("space"), r.Error))

    and ``short option plus arg`` stream' tuple' =
      match stream'.Peek() with
        | ' ' -> ``expecting hyphen 1`` stream' tuple'
        | ',' -> ``expecting space`` stream' tuple'
        | _   -> Reply(tuple')

    and ``expecting space`` stream' tuple' =
      if stream'.SkipAndPeek() = ' '
      then ``expecting hyphen 1`` stream' tuple'
      else replyErr(Expected("space"))

    and ``expecting hyphen 1`` stream' tuple' =
      if stream'.SkipAndPeek() = '-'
      then ``expecting hyphen 2`` stream' tuple'
      else replyErr(Expected("'-'"))

    and ``expecting hyphen 2`` stream' tuple' =
      if stream'.SkipAndPeek() = '-'
      then ``expecting long option`` stream' tuple'
      else replyErr(Expected("'-'"))

    and ``expecting long option`` stream' (s', _, a') =
      let () = stream'.Skip() in
      match stream'.ReadCharsOrNewlinesWhile(loptPredicate, true) with
        | "" -> replyErr(Expected("long option (letters or hyphens)"))
        | id -> ``long option (+short?)`` stream' (s', id, a')

    and ``long option (+short?)`` stream' tuple' =
      match stream'.Peek() with
        | '=' -> ``expecting arg (long)`` stream' tuple'
        | ' ' -> ``expecting arg or space (long)`` stream' tuple'
        | _   -> Reply(tuple')

    and ``expecting arg or space (long)`` stream' tuple' =
      if stream'.SkipAndPeek() = ' ' then Reply(tuple')
      else let r = parg stream' in match r.Status with
        | Ok -> ``long option plus arg (+short?)`` tuple' (Some(r.Result))
        | _  -> Reply(Error, ErrorMessageList(Expected("space"), r.Error))

    and ``expecting arg (long)`` stream' tuple' =
      let () = stream'.Skip() in
      let r = parg stream' in match r.Status with
        | Ok -> ``long option plus arg (+short?)`` tuple' (Some(r.Result))
        | _  -> Reply(Error, r.Error)

    and ``long option plus arg (+short?)`` (s', l', a') newa' =
      match a', newa' with
        | _, None          -> Reply((s', l', a'))
        | None, _          -> Reply((s', l', newa'))
        | Some(l), Some(r) ->
          try Reply((s', l', Some(Token.Argument.Merge(l, r))))
          with _ -> replyErr(Message("Arguments differ!"))

    member __.Parse:Parser<Token.Option> = fun stream' ->
      let reply = ``start`` stream' in
      Reply(reply.Status,
            (if reply.Status = Ok
             then reply.Result |> Token.Option
             else Unchecked.defaultof<_>),
            reply.Error)
  end
;;
