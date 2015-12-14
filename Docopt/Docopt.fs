namespace Docopt
#nowarn "62"
#light "off"

open System

type HelpCallback = unit -> string
;;

module internal DocHelper =
  begin
    open System.Text.RegularExpressions
    let uRegex = Regex(@"(?<=(?:\n|^)\s*usage:).*?(?=\n\s*\n|$)",
                       RegexOptions.IgnoreCase ||| RegexOptions.Singleline)

    let oRegex = Regex(@"(?<=(?:\n|^)\s*options:).*?(?=\n\s*\n|$)",
                       RegexOptions.IgnoreCase ||| RegexOptions.Singleline)

    let cut doc' =
      let uStr = uRegex.Match(doc') in
      let oStr = oRegex.Match(doc', uStr.Index + uStr.Length) in
      (uStr.Value, oStr.Value)
  end
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
    let (uStr, oStr) = DocHelper.cut doc'
    let options = OptionsParser(soptChars).Parse(oStr)
    let pusage = UsageParser(uStr, options)
    member __.Parse(?argv':string array, ?args':Arguments.Dictionary) =
      let args = if args'.IsSome then args'.Value else Arguments.Dictionary(options) in
      match defaultArg argv' argv with
        | [||] -> args
        | argv -> pusage.Parse(argv, args)
    member __.Usage = uStr
  end
;;
