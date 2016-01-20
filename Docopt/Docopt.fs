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

    let oRegex = Regex(@"(?<=(?:\n|^)\s*options:).*?(?=\n\s*\n|$|(?:\n|^)\s*options:)",
                       RegexOptions.IgnoreCase ||| RegexOptions.Singleline)

    let cut doc' =
      let uStr = uRegex.Match(doc') in
      let oStr = oRegex.Matches(doc', uStr.Index + uStr.Length) in
      uStr.Value, String.Join("\n", seq {for m in oStr -> m.Value})
  end
;;

type Docopt(doc', ?argv':string array, ?help':HelpCallback, ?version':obj,
            ?soptChars':string) =
  class
    static let noVersionObject =
      { new Object() with member __.ToString() = "<ERROR: NO VERSION GIVEN>" }
    let argv = defaultArg argv' (Environment.GetCommandLineArgs().[1..])
               |> Array.copy
    let help = defaultArg help' (fun () -> doc')
    let version = defaultArg version' noVersionObject
    let soptChars = defaultArg soptChars' "?"
    let (uStr, oStr) = DocHelper.cut doc'
    let options = OptionsParser(soptChars).Parse(oStr)
    let pusage = UsageParser(uStr, options)
    let defaultDict = Arguments.Dictionary(options)
    member __.Parse(?argv':string array, ?args':Arguments.Dictionary) =
      let args = defaultArg args' defaultDict in
      let argv = defaultArg argv' argv in
      pusage.Parse(argv, args)
    member __.Usage = uStr
    member __.UsageParser = pusage
    member __.DefaultDictionary = defaultDict
  end
;;
