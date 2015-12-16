module Docopt.Arguments
#nowarn "62"
#light "off"

open System
open System.Collections.Generic

type Result =
  | None
  | Flag of bool
  | Flags of int
  | Command of bool
  | Argument of string
  | Arguments of string list

[<StructuredFormatDisplay("Docopt.Arguments.Dictionary {SFDisplay}")>]
[<AllowNullLiteral>]
type Dictionary() =
  class
    member val private dict = Dictionary<string, Result>()
    internal new(o':Docopt.Options) as xx = Dictionary()
      then
        for kv in o'.sopt do
          xx.dict.Add(String([|'-';kv.Key|]), Flag(false)) done;
        for kv in o'.lopt do
          xx.dict.Add(kv.Key, Flag(false)) done
    interface IDictionary<string, Result> with
      member xx.Add(k':string, v':Result) = xx.dict.Add(k', v')
      member xx.Add(kv':KeyValuePair<string,Result>) =
        let idict = xx.dict :> IDictionary<string, Result> in
        idict.Add(kv')
      member xx.Clear() = xx.dict.Clear()
      member xx.Contains(kv':KeyValuePair<string,Result>) =
        let idict = xx.dict :> IDictionary<string, Result> in
        idict.Contains(kv')
      member xx.ContainsKey(k':string) = xx.dict.ContainsKey(k')
      member xx.CopyTo(a':KeyValuePair<string,Result> [], i':int) = 
        let idict = xx.dict :> IDictionary<string, Result> in
        idict.CopyTo(a', i')
      member xx.Count = xx.dict.Count
      member xx.GetEnumerator() =
        xx.dict.GetEnumerator() :> IEnumerator<KeyValuePair<string,Result>>
      member xx.GetEnumerator() =
        xx.dict.GetEnumerator() :> System.Collections.IEnumerator
      member xx.Remove(k':string) = xx.dict.Remove(k')
      member xx.Remove(kv':KeyValuePair<string,Result>) =
        let idict = xx.dict :> IDictionary<string, Result> in
        idict.Remove(kv')
      member xx.TryGetValue(k':string, v':byref<Result>) =
        xx.dict.TryGetValue(k', &v')
      member __.IsReadOnly = false
      member xx.Keys = upcast xx.dict.Keys
      member xx.Values = upcast xx.dict.Values
      member xx.Item with get k' = xx.dict.[k']
                      and set k' v' = xx.dict.[k'] <- v'
    end
    member xx.AsDictionary() = xx :> IDictionary<string, Result>
    member xx.AsList() = [for kv in xx.dict do yield (kv.Key, kv.Value) done]
    member inline private xx.SFDisplay = xx.AsList()
  end
;;
