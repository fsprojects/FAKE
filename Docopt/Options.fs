namespace Docopt

open System
open System.Collections.Generic

type Option(short':char, long':string, default':string) =
  class
    member val Short = short'
    member val Long = long'
    member val Default = default' with get, set
    new() = Option(Char.MaxValue, null, null)
    static member op_Equality(lhs':Option, rhs':Option) =
      lhs'.Short = rhs'.Short
      && lhs'.Long = rhs'.Long
    static member Empty = Option()
    member xx.IsEmpty = xx = Option.Empty
  end
;;

type Options = List<Option>
;;
