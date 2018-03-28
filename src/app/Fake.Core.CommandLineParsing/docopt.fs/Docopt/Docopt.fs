namespace Docopt
#nowarn "62"

open System

type HelpCallback = unit -> string

module DocHelper =
    type private Last =
      | Usage = 0
      | Options = 1
      | Nothing = 2

    [<Literal>]
    let OrdinalIgnoreCase = StringComparison.OrdinalIgnoreCase

    let (|Usage|Options|Other|Newline|) (line':string) =
      if String.IsNullOrEmpty(line')
      then Newline
      elif line'.[0] = ' ' || line'.[0] = '\t'
      then Other(line')
      else let idx = line'.IndexOf("usage:", OrdinalIgnoreCase) in
           if idx <> -1
           then Usage(line'.Substring(idx + 6))
           else let idx = line'.IndexOf("options", OrdinalIgnoreCase)
                let idxCol = line'.IndexOf(":", OrdinalIgnoreCase)
                if idx <> -1 && idxCol <> -1
                then 
                  let title = line'.Substring(0, idxCol)
                  let sectionName =
                    let startIdx = title.IndexOf("[")
                    let endIdx = title.IndexOf("]")
                    if startIdx <> -1 && endIdx > startIdx
                    then title.Substring(startIdx + 1, endIdx - startIdx - 1)
                    else "options"
                  Options(sectionName, line'.Substring(idxCol + 1))
                else Other(line')
    type OptionBuilder =
      { mutable Lines : string list
        Title : string }
      member x.AddLine line = { x with Lines = line :: x.Lines }
      member x.Build() = { OptionSection.Title = x.Title; OptionSection.Lines = x.Lines }
      static member Build (x:OptionBuilder) = x.Build()
    let cut (doc':string) =
      let folder (usages', (sections':OptionBuilder list), last') = function
      | Usage(ustr)   -> (ustr::usages', sections', Last.Usage)
      | Options(sectionName, ostr) -> (usages', { Title = sectionName; Lines = [ostr] } :: sections', Last.Options)
        //match sections' with
        //| options' :: sections' -> (usages', (ostr::options')::sections', Last.Options)
        //| [] -> (usages', [{ Title = sectionName; Lines = [ostr] }], Last.Options)
      | Newline       -> (usages', sections', Last.Nothing)
      | Other(line)   -> match last' with
                         | Last.Usage   -> (line::usages', sections', Last.Usage)
                         | Last.Options -> 
                            match sections' with
                            | options' :: sections' -> (usages', (options'.AddLine line) :: sections', Last.Options)
                            | [] -> (usages', [{ Title = "options"; Lines = [line] }], Last.Options)
                         | _            -> (usages', sections', Last.Nothing)
      in doc'.Split([|"\r\n";"\n";"\r"|], StringSplitOptions.None)
      |> Array.fold folder ([], [], Last.Nothing)
      |> fun (ustr', ostrs', _) ->
           let ustrsArray = List.toArray ustr' in
           let ostrsArray = List.toArray (ostrs' |> List.map OptionBuilder.Build) in
           Array.Reverse(ostrsArray);
           Array.Reverse(ustrsArray);
           (ustrsArray, ostrsArray)

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
    let (uStrs, sections) = DocHelper.cut doc'
    let sectionsParsers =
      sections
      |> Seq.map (fun oStrs -> oStrs.Title, SafeOptions(OptionsParser(soptChars).Parse(oStrs.Lines)))
      |> dict
    let pusage = UsageParser(uStrs, sectionsParsers)
    member __.Parse(?argv':string array, ?args':Arguments.Dictionary) =
      let args = defaultArg args' (Arguments.Dictionary()) in
      let argv = defaultArg argv' argv in
      let result = pusage.ParseCommandLine(argv)
      result |> Map.iter (fun key i ->
        match i with
        | Arguments.Argument cmd -> args.AddArg(key, cmd)
        | Arguments.Arguments cmds -> cmds |> Seq.iter (fun cmd -> args.AddArg(key, cmd))
        | Arguments.Command -> args.AddCmd(key)
        | Arguments.Flag -> args.AddString(key)
        | Arguments.Flags i -> [1..i] |> Seq.iter (fun _ -> args.AddString(key))
        | Arguments.None -> args.AddString(key))

      args
    member __.Usage = String.Join("\n", uStrs)
    member __.UsageParser = pusage
  end
;;
