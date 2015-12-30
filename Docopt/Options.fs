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
    new() = Option(Char.MaxValue, null, null, null)
    static member op_Equality(lhs':Option, rhs':Option) =
      lhs'.Short = rhs'.Short
      && lhs'.Long = rhs'.Long
    static member Empty = Option()
    member xx.IsEmpty = xx = Option.Empty
    member xx.HasArgument = xx.ArgName <> null
  end
;;

type Options() =
  class
    inherit List<Option>()
    member __.Find(s':char) = base.Find(fun o' -> o'.Short = s')
  end
;;
