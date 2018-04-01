namespace Docopt

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
    sprintf "Option { Short=%A; Long=%A; ArgName=%A; Default=%A }"
      xx.Short xx.Long xx.ArgumentName xx.DefaultValue

[<AllowNullLiteral>]
type Option(?short':char, ?long':string, ?argName':string, ?default':string) =
  class
    let short' = defaultArg short' Char.MaxValue
    let long' = defaultArg long' null
    let argName' = defaultArg argName' null
    let default' = defaultArg default' null
    static member op_Equality(lhs':Option, rhs':Option) =
      lhs'.Short = rhs'.Short
      && lhs'.Long = rhs'.Long
    static member Empty = Option()
    member val Short = short'
    member val Long = long'
    member val ArgName = argName'
    member val Default = default' with get, set
    member val FullShort = if short' = Char.MaxValue
                           then "" else String([|'-';short'|])
    member val FullLong = if long' = null
                          then "" else String.Concat("--", long')
    member xx.IsEmpty = xx = Option.Empty
    member xx.HasArgument = not (isNull xx.ArgName)
    member xx.HasDefault = not (isNull xx.Default)
    member xx.IsShort = xx.Short <> Char.MaxValue
    member xx.IsLong = not (isNull xx.Long)
    override xx.ToString() =
      sprintf "Option { Short=%A; Long=%A; ArgName=%A; Default=%A }"
        xx.Short xx.Long xx.ArgName xx.Default
  end
;;


[<AllowNullLiteral>]
type SafeOptions(list:SafeOption list) =
    let findIn (l':string) (list:SafeOption list) =
      list
      |> List.tryFind(fun o' -> o'.Long = Some l')
      |> Option.orElseWith (fun _ -> List.tryFind(fun o' -> o'.Long.IsSome && o'.Long.Value.StartsWith(l')) list)

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

[<AllowNullLiteral>]
type Options() =
  class
    inherit List<Option>()
    member __.Find(s':char) =
      base.Find(fun o' -> o'.Short = s')
    member __.FindAndRemove(s':char) =
      match base.FindIndex(fun o' -> o'.Short = s') with
      | -1 -> null
      | i  -> let ret = base.[i] in
              base.RemoveAt(i);
              ret
    member __.Find(l':string) =
      match base.Find(fun o' -> o'.Long = l') with
      | null -> base.Find(fun o' -> o'.Long.StartsWith(l'))
      | opt  -> opt
    member __.FindLast(l':string) =
      match base.FindLast(fun o' -> o'.Long = l') with
      | null -> base.FindLast(fun o' -> o'.Long.StartsWith(l'))
      | opt  -> opt
    member xx.Copy() =
      let newOptions = Options() in
      newOptions.AddRange(xx :> IEnumerable<Option>);
      newOptions
  end
;;
