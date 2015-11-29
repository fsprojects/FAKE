namespace Docopt
#nowarn "62"
#light "off"

open System
open System.Text.RegularExpressions

type HelpCallback = unit -> string
;;

type Docopt(doc', ?argv':string array, ?help':HelpCallback, ?version':obj) =
  class
    static let noVersionObject =
      { new Object() with member __.ToString() = "<ERROR: NO VERSION GIVEN>" }
    static let optionsRegex =
      Regex(@"(?<=\n\s*options:).*?(?=\n\r?\n)",
            RegexOptions.IgnoreCase ||| RegexOptions.Singleline)
    let argv = defaultArg argv' (Environment.GetCommandLineArgs().[1..])
    let help = defaultArg help' (fun () -> doc')
    let version = defaultArg version' noVersionObject
    let options = optionsRegex.Match(doc').Value
                  .Split([|'\n';'\r'|], StringSplitOptions.RemoveEmptyEntries)
    do printfn "%A" options
    member xx.Parse(?argv':string array, ?args':Args) =
      let args = if args'.IsSome then args'.Value else Args() in
      match defaultArg argv' argv with
        | [||] -> args
        | argv -> args
  end
;;
