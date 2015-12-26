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
type Dictionary(options':Options) =
  class
    let dict = Dictionary<string, Result ref>()
    do for o in options' do
         match o.Short, o.Long with
         | short, null         -> dict.[String([|'-';short|])] <- ref (if o.Default = null then Flag(false) else Argument(o.Default))
         | Char.MaxValue, long -> dict.[long] <- ref (if o.Default = null then Flag(false) else Argument(o.Default))
         | short, long         -> let result = ref (if o.Default = null then Flag(false) else Argument(o.Default)) in
                                  dict.[String([|'-';short|])] <- result;
                                  dict.[long] <- result
       done
    member __.AsList() = [for kv in dict do yield (kv.Key, !kv.Value) done]
    member __.Item with get key' = !dict.[key']
                    and set key' value' = dict.[key'] := value'
    member xx.UnsafeAddShort(s':char, ?arg':string) =
      let key = String([|'-';s'|]) in
      let newval = match xx.[key] with
      | None
      | Flag(false)                      -> Flag(true)
      | Flag(_)                          -> Flags(2)
      | Flags(n)                         -> Flags(n + 1)
      | Argument(arg) when arg'.IsSome   -> Arguments([arg'.Value;arg])
      | Arguments(args) when arg'.IsSome -> Arguments(arg'.Value::args)
      | value                            -> value in
      xx.[key] <- newval
    member xx.AddShort(s':char, ?arg':string) =
      let predicate (o':Option) =
        if o'.Short <> s'
        then false
        else (xx.UnsafeAddShort(s', ?arg'=arg'); true)
      in Seq.exists ( predicate ) options'
    member inline private xx.SFDisplay = xx.AsList()
  end
;;
