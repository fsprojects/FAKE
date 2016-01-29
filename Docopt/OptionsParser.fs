namespace Docopt
#nowarn "62"
#light "off"

open FParsec
open System
open System.Text.RegularExpressions

[<NoComparison>]
type internal PoptLineResult =
  | Opt of Option
  | Val of string
  | Nil
;;

type OptionsParser(soptChars':string) =
  class
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
      pupperArg <|> plowerArg

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
        | ' ' -> ``argument or hyphens or space`` stream' tuple'
        | '=' -> ``expecting arg (short)`` stream' tuple'
        | ',' -> ``expecting space`` stream' tuple'
        | _   -> Reply(tuple')
    and ``expecting arg (short)`` stream' (s', l', _) =
      let () = stream'.Skip() in
      let r = parg stream' in match r.Status with
        | Ok -> ``short option plus arg`` stream' (s', l', Some(r.Result))
        | _  -> Reply(Error, r.Error)
    and ``argument or hyphens or space`` stream' (s', l', a') =
      match stream'.SkipAndPeek() with
        | '-' -> ``expecting hyphen 2`` stream' (s', l', a')
        | ' ' -> Reply((s', l', a'))
        | _   -> let r = parg stream' in match r.Status with
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
      else Reply(tuple') //////////////////// CHANGE DFA //////////////////////
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
        | Some(l), Some(r) -> Reply((s', l', Some(String.Concat(l, " or ", r))))

    let poptLine = fun stream' ->
      let reply = ``start`` stream' in
      Reply(reply.Status,
            (if reply.Status = Ok
             then let (s, l, arg) = reply.Result in
                  Option(s, l, defaultArg arg null, null)
             else null),
            reply.Error)

    let defaultRegex = Regex(@"(?<=\[default:\s).*(?=])",
                             RegexOptions.RightToLeft
                             ||| RegexOptions.IgnoreCase)

    member __.Parse(optionStrings':string array) =
      let parseAsync line' = async {
          let dflt = defaultRegex.Match(line') in
          return
            match run poptLine line' with
            | Failure(e, _, _) -> if dflt.Success then Val(dflt.Value)
                                  else Nil
            | Success(r, _, _) -> (if dflt.Success then r.Default <- dflt.Value);
                                  Opt(r)
        } in
      let options = Options() in
      let lastOpt = ref Option.Empty in
      let action = function
      | Nil      -> ()
      | Opt(opt) -> let () = lastOpt := opt in options.Add(opt)
      | Val(str) -> let lastOptCopy = !lastOpt in
                    if not lastOptCopy.IsEmpty
                    then lastOptCopy.Default <- str
      in optionStrings'
      |> Seq.map parseAsync
      |> Async.Parallel
      |> Async.RunSynchronously
      |> Seq.iter action
      |> (fun _ -> options)
  end
;;
