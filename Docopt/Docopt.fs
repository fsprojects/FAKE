namespace Docopt
#nowarn "62"
#light "off"

open System

type HelpCallback = unit -> string
;;

type Docopt(doc', ?argv':string array, ?help':HelpCallback, ?version':obj,
            ?soptChars':string) =
  class
    static let noVersionObject =
      { new Object() with member __.ToString() = "<ERROR: NO VERSION GIVEN>" }
    let argv = defaultArg argv' (Environment.GetCommandLineArgs().[1..])
    let help = defaultArg help' (fun () -> doc')
    let version = defaultArg version' noVersionObject
    let soptChars = defaultArg soptChars' "?"
    let usage, options = DocParser(soptChars).Parse(doc')
    member __.Parse(?argv':string array, ?args':Args) =
      let args = if args'.IsSome then args'.Value else Args() in
      match defaultArg argv' argv with
        | [||] -> args
        | argv -> args
    member __.Usage = usage
  end
;;
