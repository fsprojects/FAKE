namespace Docopt
#nowarn "62"
#light "off"

open System

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
