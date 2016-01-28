namespace Docopt

open System
open System.Collections.Generic

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
    member xx.HasArgument = xx.ArgName <> null
    member xx.HasDefault = xx.Default <> null
    member xx.IsShort = xx.Short <> Char.MaxValue
    member xx.IsLong = xx.Long <> null
    override xx.ToString() =
      sprintf "Option { Short=%A; Long=%A; ArgName=%A; Default=%A }"
        xx.Short xx.Long xx.ArgName xx.Default
  end
;;

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
