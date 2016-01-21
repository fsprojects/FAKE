module Docopt.Arguments
#nowarn "62"
#light "off"

open System
open System.Collections.Generic

type Result =
  | None
  | Flag
  | Flags of int
  | Command of bool
  | Argument of string
  | Arguments of string list

[<StructuredFormatDisplay("Docopt.Arguments.Dictionary {SFDisplay}")>]
[<AllowNullLiteral>]
type Dictionary() =
  class
    let dict = Dictionary<string, Result>() :> IDictionary<_, _>
    interface IDictionary<string, Result> with
      member __.Add(k', v') = dict.Add(k', v')
      member __.Add(kv') = dict.Add(kv')
      member __.Clear() = dict.Clear()
      member __.Contains(kv') = dict.Contains(kv')
      member __.ContainsKey(k') = dict.ContainsKey(k')
      member __.CopyTo(array', arrayIndex') = dict.CopyTo(array', arrayIndex')
      member __.Count = dict.Count
      member __.GetEnumerator() = dict.GetEnumerator() :> Collections.IEnumerator
      member __.GetEnumerator() = dict.GetEnumerator()
      member __.IsReadOnly = dict.IsReadOnly
      member __.Keys = dict.Keys
      member __.Remove(kv':KeyValuePair<_, _>) = dict.Remove(kv')
      member __.Remove(k':string) = dict.Remove(k')
      member __.TryGetValue(k', v') = dict.TryGetValue(k', &v')
      member __.Values = dict.Values
      member __.Item with get key' =
                       let mutable value = Unchecked.defaultof<Result> in
                       if dict.TryGetValue(key', &value)
                       then value
                       else None
                      and set key' value' =
                       dict.Add(key', value')
    end
    member __.AsList() = [for kv in dict -> kv.Key, kv.Value]
    member xx.Clear = (xx :> IDictionary<_, _>).Clear
    member xx.AddString(key':string, ?arg':string) =
      let newval =
        let currentVal = (xx :> IDictionary<_, _>).[key'] in
        if arg'.IsNone
        then match currentVal with
             | None        -> Flag
             | Flag        -> Flags(2)
             | Flags(n)    -> Flags(n + 1)
             | value       -> value
        else match currentVal with
             | None
             | Flag
             | Flags(_)        -> Argument(arg'.Value)
             | Argument(arg)   -> Arguments([arg'.Value;arg])
             | Arguments(args) -> Arguments(arg'.Value::args)
             | value           -> value in
      (xx :> IDictionary<_, _>).[key'] <- newval
    member xx.AddOpt(o':Option, ?arg':string) =
      if o'.IsShort
      then xx.AddString(String([|'-';o'.Short|]), ?arg'=arg');
      if o'.IsLong
      then xx.AddString(String.Concat("--", o'.Long), ?arg'=arg')
    member xx.AddArg(a':string, val':string) =
      xx.AddString(a', val')
    member xx.AddCmd(cmd':string) =
      xx.AddString(cmd')
    member __.AddRange(range':#IEnumerable<KeyValuePair<string, Result>>) =
      let idict = dict :> IDictionary<_, _> in
      for kv in range' do
        idict.Add(kv)
      done
    member private xx.SFDisplay = xx.AsList()
  end
;;
