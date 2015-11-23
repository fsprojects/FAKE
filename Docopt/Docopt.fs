namespace Docopt
#nowarn "62"
#light "off"

(*
#r """D:\rendu\docopt.fs\packages\FParsec-Big-Data-Edition.1.0.2\lib\net45\FParsecCS.dll""";;
#r """D:\rendu\docopt.fs\packages\FParsec-Big-Data-Edition.1.0.2\lib\net45\FParsec.dll""";;
open FParsec;;
type UserState = unit;;
type Parser<'t> = Parser<'t, UserState>;;
*)

open System
open FParsec

module Token =
  begin
    type Argument =
      struct
        val Name : string
        val Type : System.Type
        val Default : obj
        new(name', type', val') = { Name=name'; Type=type'; Default=val'; }
        new(name', type', val') = match type' with
          | None        -> Argument(name', typeof<string>, val')
          | Some(tname) -> match tname with
            | "bool"    -> Argument(name', typeof<bool>, val')
            | "int"     -> Argument(name', typeof<int32>, val')
            | "uint"    -> Argument(name', typeof<uint32>, val')
            | "float"   -> Argument(name', typeof<System.Single>, val')
            | "double"  -> Argument(name', typeof<System.Double>, val')
            | "decimal" -> Argument(name', typeof<decimal>, val')
            | "string"  -> Argument(name', typeof<string>, val')
            | "time"
            | "date"    -> Argument(name', typeof<System.DateTime>, val')
            | _         -> Argument(name', System.Type.GetType(tname), val')
        new(name', type':option<_>) = Argument(name', type', null)
        override xx.ToString() =
          sprintf "Argument { Name = %s; Type = %A; Default = %A }"
            xx.Name xx.Type xx.Default
      end

    type Option =
      struct
        val Sname : char
        val Lname : string
        val Arg : Argument
        new(sname', lname', arg') = { Sname=sname'; Lname=lname'; Arg=arg'; }
        override xx.ToString() =
          sprintf "Option { Sname = %c; Lname = %s; Arg = %A"
            xx.Sname xx.Lname xx.Arg
      end
  end
;;

module Parser =
  begin
    type UserState = unit
    ;;

    type Parser<'t> = Parser<'t, UserState>
    ;;

    let pupperArg:Parser<string> =
      let start = isUpper in
      let cont c = (isUpper c) || (c = '-') in
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
      let ptype = many1SatisfyL (( <> ) ' ') "valid F# type" in
      let poptType = opt (skipChar ':' >>. ptype) in
      pidentifier .>>. poptType |>> Token.Argument
    ;;

    let poptDescLine:Parser<char * string * Token.Argument option> =
      let soptChars = "?"
      in let replyErr err' = Reply(Error, ErrorMessageList(err'))
      in let loptPredicate c' = isLetter(c') || isDigit(c') || c' = '-'

      in let rec ``short or long option`` (stream':CharStream<_>) =
        let c = stream'.SkipAndPeek() in
        if isLetter(c) || (isAnyOf soptChars) c
        then ``short option`` stream' (c, null, None)
        elif c = '-' then ``expecting long option`` stream' (EOS, null, None)
        else replyErr(Expected(sprintf "letters or any of «%s»" soptChars))

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

      and ``expecting arg or space (long)`` stream' (s', l', a') =
        if stream'.SkipAndPeek() = ' ' then Reply((s', l', a'))
        else let r = parg stream' in match r.Status with
          | Ok -> ``long option plus arg (+short?)`` (s', l', Some(r.Result))
          | _  -> Reply(Error, ErrorMessageList(Expected("space"), r.Error))

      and ``expecting arg (long)`` stream' (s', l', _) =
        let () = stream'.Skip() in
        let r = parg stream' in match r.Status with
          | Ok -> ``long option plus arg (+short?)`` (s', l', Some(r.Result))
          | _  -> Reply(Error, r.Error)

      and ``long option plus arg (+short?)`` tuple' = Reply(tuple')
      in fun stream' ->
        let state = stream'.State in
        let _ = stream'.SkipWhitespace() in
        if stream'.Match('-')
        then ``short or long option`` stream'
        else replyErr(Expected("'-'"))
    ;;

    let parseOptDesc doc' =
      ()
    ;;

    let popt = ()
    ;;

    let popts = ()
    ;;
  end
;;

type HelpCallback = unit -> string
;;

type Docopt(doc', argv', help', version':obj) =
  class
    new(doc', ?argv':string array, ?help':HelpCallback, ?version') =
      let argv = defaultArg argv' (Environment.GetCommandLineArgs()) in
      let help = defaultArg help' ( fun () -> doc' ) in
      let version = defaultArg version' null in
      Docopt(doc', argv, help, version)
    new(doc', ?argv':string array, ?help':string, ?version') =
      let argv = defaultArg argv' (Environment.GetCommandLineArgs()) in
      let help = match help' with Some(str) -> ( fun () -> str)
                                | None      -> ( fun () -> doc' ) in
      let version = defaultArg version' null in
      Docopt(doc', argv, help, version)
    member __.Parse() =
      begin
        
      end
  end
;;
