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
type Dictionary internal(o':Docopt.Options) =
  class
    let dict = Dictionary<string, Result>()
    do for kv in o'.sopt do dict.Add(String([|'-';kv.Key|]), None) done
    do for kv in o'.lopt do dict.Add(kv.Key, None) done
    interface IDictionary<string, Result> with
      member __.Add(k':string, v':Result) = dict.Add(k', v')
      member __.Add(kv':KeyValuePair<string,Result>) =
        let idict = dict :> IDictionary<string, Result> in
        idict.Add(kv')
      member __.Clear() = dict.Clear()
      member __.Contains(kv':KeyValuePair<string,Result>) =
        let idict = dict :> IDictionary<string, Result> in
        idict.Contains(kv')
      member __.ContainsKey(k':string) = dict.ContainsKey(k')
      member __.CopyTo(a':KeyValuePair<string,Result> [], i':int) = 
        let idict = dict :> IDictionary<string, Result> in
        idict.CopyTo(a', i')
      member __.Count = dict.Count
      member __.GetEnumerator() =
        dict.GetEnumerator() :> IEnumerator<KeyValuePair<string,Result>>
      member __.GetEnumerator() =
        dict.GetEnumerator() :> System.Collections.IEnumerator
      member __.Remove(k':string) = dict.Remove(k')
      member __.Remove(kv':KeyValuePair<string,Result>) =
        let idict = dict :> IDictionary<string, Result> in
        idict.Remove(kv')
      member __.TryGetValue(k':string, v':byref<Result>) =
        dict.TryGetValue(k', &v')
      member __.IsReadOnly = false
      member __.Keys = upcast dict.Keys
      member __.Values = upcast dict.Values
      member __.Item with get k' = dict.[k']
                      and set k' v' = dict.[k'] <- v'
    end
    member inline xx.AsDictionary() = xx :> IDictionary<string, Result>
    member __.AsList() = [for kv in dict do yield (kv.Key, kv.Value) done]
    member inline private xx.SFDisplay = xx.AsList()
  end
;;
