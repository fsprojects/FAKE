namespace Fake.Core

open System
open Fake.Core.CommandLineParsing

//type HelpCallback = unit -> string

[<RequireQualifiedAccess>]
module DocHelper =
    type private Last =
      | Usage = 0
      | Options = 1
      | Nothing = 2

    [<Literal>]
    let internal OrdinalIgnoreCase = StringComparison.OrdinalIgnoreCase

    let internal (|Usage|Options|Other|Newline|) (line':string) =
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
    type internal OptionBuilder =
      { /// The lines in reversed order
        mutable Lines : string list
        Title : string }
      member x.AddLine line = { x with Lines = line :: x.Lines }
      member x.Build() = { OptionSection.Title = x.Title; OptionSection.Lines = x.Lines |> List.rev }
      static member Build (x:OptionBuilder) = x.Build()
    let cut (doc':string) =
      let folder (usages', (sections':OptionBuilder list), last') = function
      | Usage(ustr) when String.IsNullOrWhiteSpace ustr  -> (usages', sections', Last.Usage)
      | Usage(ustr) -> (ustr::usages', sections', Last.Usage)
      | Options(sectionName, ostr) -> (usages', { Title = sectionName; Lines = [ostr] } :: sections', Last.Options)
        //match sections' with
        //| options' :: sections' -> (usages', (ostr::options')::sections', Last.Options)
        //| [] -> (usages', [{ Title = sectionName; Lines = [ostr] }], Last.Options)
      | Newline       -> (usages', sections', Last.Nothing)
      | Other(line)   -> match last' with
                         | Last.Usage when String.IsNullOrWhiteSpace line -> (usages', sections', Last.Usage)
                         | Last.Usage -> (line::usages', sections', Last.Usage)
                         | Last.Options -> 
                            match sections' with
                            | options' :: sections' -> (usages', (options'.AddLine line) :: sections', Last.Options)
                            | [] -> (usages', [{ Title = "options"; Lines = [line] }], Last.Options)
                         | _            -> (usages', sections', Last.Nothing)
      in doc'.Split([|"\r\n";"\n";"\r"|], StringSplitOptions.None)
      |> Array.fold folder ([], [], Last.Nothing)
      |> fun (ustr', ostrs', _) ->
           let ustrsArray = List.toArray ustr'
           let ostrsArray =
              ostrs'
              |> List.map OptionBuilder.Build
              |> List.toArray
           Array.Reverse(ostrsArray)
           Array.Reverse(ustrsArray)
           let groupedResults =
             ostrsArray
             |> Array.groupBy (fun sec -> sec.Title)
             |> Array.map (fun (title, group) ->
              { OptionSection.Title = title; OptionSection.Lines = group |> Seq.map (fun item -> item.Lines) |> Seq.concat |> Seq.toList })
           (ustrsArray, groupedResults)

type Docopt(doc', ?soptChars':string) =
    let soptChars = defaultArg soptChars' "?"
    let (uStrs, sections) = DocHelper.cut doc'
    let sectionsParsers =
      sections
      |> Seq.map (fun oStrs -> oStrs.Title, SafeOptions(OptionsParser(soptChars).Parse(oStrs.Lines)))
      |> Seq.toList
    let pusage = UsageParser(uStrs, sectionsParsers)
    member __.Parse(argv':string array) =      

      let result = pusage.ParseCommandLine(argv')
      // fill defaults
      sectionsParsers
      |> Seq.fold (fun map (_, section) ->
        section
        |> Seq.fold (fun map opt ->
          let addKey def key =
            if not (String.IsNullOrEmpty key) then // opt.FullShort else opt.FullLong
              match def, Map.tryFind key map with
              | Some defVal, None ->
                Map.add key (DocoptResult.Argument defVal)
              | _ -> id
            else id

          map
          |> addKey opt.DefaultValue opt.FullLong
          |> addKey opt.DefaultValue opt.FullShort
        ) map) result

    member __.Usage = String.Join("\n", uStrs)
    member __.UsageParser = pusage
