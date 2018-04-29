namespace Fake.Core.CommandLineParsing

open System
open System.Collections.Generic

type OptionSection = { Title : string; Lines : string list }

type SafeOption =
 { Short : char option; Long : string option; ArgumentName : string option; AllowMultiple : bool; IsRequired : bool; DefaultValue : string option }
 static member Empty =
   { Short = None; Long = None; ArgumentName = None ; AllowMultiple = false; IsRequired = false; DefaultValue = None }
 member x.FullShort =
    match x.Short with
    | None -> ""
    | Some s -> String([|'-';s|])
 member x.FullLong =
    match x.Long with
    | None -> ""
    | Some l -> String.Concat("--", l)
  member xx.IsEmpty = xx = SafeOption.Empty
  member xx.HasArgument = Option.isSome xx.ArgumentName
  member xx.HasDefault = Option.isSome  xx.DefaultValue
  member xx.IsShort = Option.isSome xx.Short
  member xx.IsLong = Option.isSome xx.Long
  override xx.ToString() =
    let inline printCOpt c =
      match c with
      | Some c -> sprintf "'%c'" c
      | None -> "<none>"
    let inline printSOpt s =
      match s with
      | Some s -> sprintf "\"%s\"" s
      | None -> "<none>"

    sprintf "Option { Short=%s; Long=%s; ArgName=%s; Default=%s }"
      (printCOpt xx.Short) (printSOpt xx.Long) (printSOpt xx.ArgumentName) (printSOpt xx.DefaultValue)

type SafeOptions(list:SafeOption list) =
    let findIn (l':string) (list:SafeOption list) =
      list
      |> List.tryFind(fun o' -> o'.Long = Some l')

    interface System.Collections.IEnumerable with
      member x.GetEnumerator () = (list :> System.Collections.IEnumerable).GetEnumerator()
    interface IEnumerable<SafeOption> with
      member x.GetEnumerator () = (list :> IEnumerable<SafeOption>).GetEnumerator()
    member __.Find(s':char) =
      list |> List.tryFind(fun o' -> o'.Short = Some s')
    member __.AddRange(opts:SafeOption list) =
      SafeOptions(opts @ list)
    member __.FindAndRemove(s':char) =
      match list |> List.tryFindIndex(fun o' -> o'.Short = Some s') with
      | None -> None
      | Some i  ->
        let ret = list.[i]
        Some (
          SafeOptions(list |> List.filter (fun t -> not <| obj.ReferenceEquals(t, ret))),
          ret)
    member __.Find(l':string) =
      findIn l' list
      //match base.Find(fun o' -> o'.Long = l') with
      //| null -> base.Find(fun o' -> o'.Long.StartsWith(l'))
      //| opt  -> opt
    member __.FindLast(l':string) =
      findIn l' (list |> List.rev)
      //match base.FindLast(fun o' -> o'.Long = l') with
      //| null -> base.FindLast(fun o' -> o'.Long.StartsWith(l'))
      //| opt  -> opt
    member __.Last = list |> List.last
