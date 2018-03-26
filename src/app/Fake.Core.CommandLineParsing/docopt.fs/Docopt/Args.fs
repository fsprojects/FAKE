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
                       if dict.ContainsKey(key')
                       then dict.[key'] <- value'
                       else dict.Add(key', value')
    end
    member __.AsList() = [for kv in dict -> kv.Key, kv.Value]
    member xx.Item
      with get key' = (xx :> IDictionary<_, _>).Item key'
       and set key' val' = (xx :> IDictionary<_, _>).Item key' <- val'
    member xx.Clear = (xx :> IDictionary<_, _>).Clear
    member xx.AddString(key':string, ?arg':string) =
      let newval =
        let currentVal = xx.[key'] in
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
      xx.[key'] <- newval
    member xx.AddOpt(o':Option, ?arg':string) =
      if o'.IsShort
      then xx.AddString(o'.FullShort, ?arg'=arg');
      if o'.IsLong
      then xx.AddString(o'.FullLong, ?arg'=arg')
    member xx.AddArg(a':string, val':string) =
      xx.AddString(a', val')
    member xx.AddCmd(cmd':string) =
      xx.AddString(cmd')
    member xx.AddRange(range':#IEnumerable<KeyValuePair<string, Result>>) =
      for kv in range' do
        (xx :> IDictionary<_, _>).Add(kv)
      done
    member internal xx.RegisterDefaults(opts':Options) =
      let canRegister (opt':Option) =
        if opt'.IsShort
        then match xx.[opt'.FullShort] with None -> true | _ -> false
        elif opt'.IsLong
        then match xx.[opt'.FullLong] with None -> true | _ -> false
        else false
      in for opt in opts' do
        if opt.HasDefault && canRegister opt
        then match opt.IsShort, opt.IsLong with
             | false, false -> ()
             | true, false  -> xx.[opt.FullShort] <- Argument(opt.Default)
             | false, true  -> xx.[opt.FullLong] <- Argument(opt.Default)
             | true, true   -> xx.[opt.FullShort] <- Argument(opt.Default);
                               xx.[opt.FullLong] <- Argument(opt.Default)
      done
    member private xx.SFDisplay = xx.AsList()
  end
;;
