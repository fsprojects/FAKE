namespace Fake.Core.CommandLineParsing

open FParsec
open System
open System.Text.RegularExpressions


type OptionsParserState =
 { ShortName : char option
   LongName : string option
   IsRequired : bool
   mutable DefaultValue : string option
   AllowMultiple : bool
   ArgName : string option }
  static member CreateEmpty() =
    { ShortName = None
      LongName = None
      IsRequired = false
      AllowMultiple = false
      ArgName = None
      DefaultValue = None }
  static member Build x : SafeOption =
    { Short = x.ShortName; Long = x.LongName; ArgumentName = x.ArgName; AllowMultiple = x.AllowMultiple; IsRequired = x.IsRequired; DefaultValue = x.DefaultValue }    

[<NoComparison>]
type internal PoptLineResult =
  | Opt of OptionsParserState
  | Val of string
  | Nil

type OptionsParser(soptChars':string) =
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

    let pParam =
      satisfyL (( = ) '[') "parameter options (supported are [+] - one or more, [*] - zero or more, and [!] exactly one)"
      >>. many1SatisfyL (( <> ) ']') "any character except ']'"
      .>> skipChar ']'
      |>> (fun name' -> String.Concat("[", name', "]"))

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
      then ``short option`` stream' { OptionsParserState.CreateEmpty() with ShortName = Some c }
      elif c = '-' then ``expecting long option`` stream' (OptionsParserState.CreateEmpty())
      else replyErr(Expected(sprintf "letters or any of «%s»" soptChars'))
    and ``short option`` stream' state =
      match stream'.SkipAndPeek() with
        | ' ' -> ``argument or hyphens or space`` stream' state
        | '=' -> ``expecting arg (short)`` stream' state
        | ',' -> ``expecting space`` stream' state
        | _   -> Reply(state)
    and ``expecting arg (short)`` stream' (state) =
      let () = stream'.Skip() in
      let r = parg stream' 
      match r.Status with
      | Ok -> ``short option plus arg`` stream' { state with ArgName = Some(r.Result)}
      | _  -> Reply(Error, r.Error)
    and ``argument or hyphens or space`` stream' (state) =
      match stream'.SkipAndPeek() with
        | '-' -> ``expecting hyphen 2`` stream' (state)
        | ' ' -> Reply(state)
        | _   ->
          let r = parg stream'
          match r.Status with
          | Ok -> ``short option plus arg`` stream' { state with ArgName =  Some(r.Result)}
          | _  -> Reply(Error, ErrorMessageList(Expected("space"), r.Error))
    and ``short option plus arg`` stream' state =
      match stream'.Peek() with
        | ' ' -> ``expecting hyphen 1`` stream' state
        | ',' -> ``expecting space`` stream' state
        | _   -> Reply(state)
    and ``expecting space`` stream' state =
      if stream'.SkipAndPeek() = ' '
      then ``expecting hyphen 1`` stream' state
      else replyErr(Expected("space"))
    and ``expecting hyphen 1`` stream' state =
      match stream'.SkipAndPeek() with
      | '-' -> ``expecting hyphen 2`` stream' state
      | '[' -> ``parameters and end`` stream' state
      | _ -> Reply(state) //////////////////// CHANGE DFA //////////////////////
    and ``expecting hyphen 2`` stream' state =
      if stream'.SkipAndPeek() = '-'
      then ``expecting long option`` stream' state
      else replyErr(Expected("'-'"))
    and ``expecting long option`` stream' state =
      let () = stream'.Skip() in
      match stream'.ReadCharsOrNewlinesWhile(loptPredicate, true) with
        | "" -> replyErr(Expected("long option (letters or hyphens)"))
        | id -> ``long option (+short?)`` stream' { state with LongName = Some id }
    and ``long option (+short?)`` stream' state =
      match stream'.Peek() with
        | '=' -> ``expecting arg (long)`` stream' state
        | ' ' -> ``expecting arg or space (long)`` stream' state
        | _   -> Reply(state)
    and ``expecting arg or space (long)`` stream' state =
      match stream'.SkipAndPeek() with
      | ' ' -> Reply (state)
      | '[' -> ``parameters and end`` stream' state
      | _ ->
        let r = parg stream'
        match r.Status with
        | Ok -> ``long option plus arg (+short?)`` stream' state (Some(r.Result))
        | _  -> Reply(Error, ErrorMessageList(Expected("space"), r.Error))
    and ``expecting arg (long)`` stream' state =
      stream'.Skip()
      let r = parg stream'
      match r.Status with
      | Ok -> ``long option plus arg (+short?)`` stream' state (Some(r.Result))
      | _  -> Reply(Error, r.Error)
    and ``long option plus arg (+short?)`` stream state newa' =
      let newState =
        match state.ArgName, newa' with
        | _, None          -> state
        | None, _          -> { state with ArgName = newa'}
        | Some(l), Some(r) -> { state with ArgName = Some(String.Concat(l, " or ", r))}
      match stream.Peek() with
      | ' ' -> stream.Skip()
      | _ -> ()
      ``parameters and end`` stream newState

    and ``parameters and end`` stream' state =
      match stream'.Peek() with
        | '[' -> 
          let r = pParam stream'
          match r.Status with
          | Ok ->
            match r.Result with
            | "[!]" -> Reply { state with IsRequired = true; AllowMultiple = false }
            | "[*]" -> Reply { state with IsRequired = false; AllowMultiple = true }
            | "[+]" -> Reply { state with IsRequired = true; AllowMultiple = true }
            | _ -> Reply(Error, ErrorMessageList(ExpectedString "[!], [*] or [+]"))
          | _  -> Reply(Error, r.Error)
          //``expecting hyphen 1`` stream' state
        | _   -> Reply(state)

    let poptLine dflt = fun stream' ->
      let reply = ``start`` stream' in
      Reply(reply.Status,
            (if reply.Status = Ok
             then { reply.Result with DefaultValue = dflt }
             else Unchecked.defaultof<OptionsParserState>),
            reply.Error)

    let defaultRegex = Regex(@"(?<=\[default:\s).*(?=])",
                             RegexOptions.RightToLeft
                             ||| RegexOptions.IgnoreCase)

    member __.Parse(optionStrings':string seq) =
      let parseAsync line' = async {
          let dflt =
            let df = defaultRegex.Match(line')
            if df.Success then Some df.Value
            else None
          return
            match run (poptLine dflt) line' with
            | Failure(e, _, _) -> match dflt with
                                  | Some dfltVal -> Val(dfltVal)
                                  | None -> Nil
            | Success(r, _, _) -> Opt(r)
        } in
      let options = ResizeArray<_>() in
      let lastOpt = ref None in
      let action = function
      | Nil      -> ()
      | Opt(opt) ->
        lastOpt := Some opt
        options.Add(opt)
      | Val(str) -> 
          match !lastOpt with // In order to parse defaults from follow-up lines...
          | Some lastOptCopy ->
            lastOptCopy.DefaultValue <- Some str
          | None -> ()
      in optionStrings'
      |> Seq.map parseAsync
      |> Async.Parallel
      |> Async.RunSynchronously
      |> Seq.iter action
      |> (fun _ -> options |> Seq.map OptionsParserState.Build |> Seq.toList)

