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

module Parsers =
  begin
    type UserState = unit
    ;;

    type Parser<'t> = Parser<'t, UserState>
    ;;

    type Argument = string
    ;;

    let pupperArg:Parser<_> =
      let start = isUpper in
      let cont c = (isUpper c) || (c = '-') in
      identifier (IdentifierOptions(isAsciiIdStart=start,
                                    isAsciiIdContinue=cont,
                                    label="UPPER-CASE identifier"))
    ;;

    let plowerArg:Parser<_> =
      satisfyL (( = ) '<') "<lower-case> identifier"
  >>. many1SatisfyL (( <> ) '>') "any character except '>'"
  .>> pchar '>'
    ;;

    let parg:Parser<Argument> = pupperArg <|> plowerArg
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
