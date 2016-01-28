namespace Docopt
#nowarn "62"
#light "off"

open System

type HelpCallback = unit -> string
;;

module internal DocHelper =
  begin
    type private Last =
      | Usage = 0
      | Options = 1
      | Nothing = 2

    let (|Usage|Options|Nothing|Newline|) (line':string) =
      if line'.StartsWith("usage:", StringComparison.OrdinalIgnoreCase)
      then Usage(line'.Substring(6))
      elif line'.StartsWith("options:", StringComparison.OrdinalIgnoreCase)
      then Options(line'.Substring(8))
      elif String.IsNullOrEmpty(line')
      then Newline
      else Nothing(line')

    let cut (doc':string) =
      let folder (usages', options', last') = function
      | Usage(ustr)   -> (ustr::usages', options', Last.Usage)
      | Options(ostr) -> (usages', ostr::options', Last.Options)
      | Newline       -> (usages', options', Last.Nothing)
      | Nothing(line) -> match last' with
                         | Last.Usage   -> (line::usages', options', Last.Usage)
                         | Last.Options -> (usages', line::options', Last.Options)
                         | _            -> (usages', options', Last.Nothing)
      in doc'.Split([|"\r\n";"\n";"\r"|], StringSplitOptions.None)
      |> Array.fold folder ([], [], Last.Nothing)
      |> fun (ustr', ostrs', _) ->
           let ostrsArray = List.toArray ostrs' in
           Array.Reverse(ostrsArray);
           (String.Join("\n", ustr'), ostrsArray)
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
    let (uStr, oStrs) = DocHelper.cut doc'
    let options = OptionsParser(soptChars).Parse(oStrs)
    let pusage = UsageParser(uStr, options)
    member __.Parse(?argv':string array, ?args':Arguments.Dictionary) =
      let args = defaultArg args' (Arguments.Dictionary()) in
      let argv = defaultArg argv' argv in
      pusage.Parse(argv, args)
    member __.Usage = uStr
    member __.UsageParser = pusage
  end
;;
