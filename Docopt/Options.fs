namespace Docopt

open System
open System.Collections.Generic

[<AllowNullLiteral>]
type Option(short':char, long':string, argName':string, default':string) =
  class
    member val Short = short'
    member val Long = long'
    member val ArgName = argName'
    member val Default = default' with get, set
    new() = Option('\uFFFF', null, null, null)
    static member op_Equality(lhs':Option, rhs':Option) =
      lhs'.Short = rhs'.Short
      && lhs'.Long = rhs'.Long
    static member Empty = Option()
    static member Trash = Option('\uFFFE', null, null, null)
    member xx.IsEmpty = xx = Option.Empty
    member xx.IsTrash = xx = Option.Trash
    member xx.HasArgument = xx.ArgName <> null
    member xx.HasDefault = xx.Default <> null
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
  end
;;
