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
        val Val : obj
        new(name', type', val') = { Name=name'; Type=type'; Val=val'; }
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
        override xx.ToString() =
          sprintf "Argument { Name = %s; Type = %A }" xx.Name xx.Type
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

    let ptype:Parser<string> =
      many1SatisfyL (( <> ) ' ') "valid F# type"
    ;;

    let parg:Parser<string * string option> =
      let pidentifier = pupperArg <|> plowerArg in
      let poptType = opt (skipChar ':' >>. ptype) in
      pidentifier .>>. poptType
    ;;

    let poptDesc:Parser<_> =
      let psopt = satisfyL (( <> ) '-') "short option" in
      let plopt = let pred c' = isLetter(c') || (c' = '-') in
                  many1SatisfyL pred "long option" in
      let space = skipChar ' ' <?> "space" in
      let p2opt = psopt .>>. ((skipString "  " >>% None) <|> (opt (optional (skipChar ',') .>> space >>. (skipString "--" >>. plopt)))) in
      spaces >>. skipChar '-' >>. (attempt ((skipChar '-' >>. plopt) |>> (fun s -> (EOS, s))) <|> (p2opt |>> (function c, Some(s) -> (c, s) | c, None -> (c, null))))
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
