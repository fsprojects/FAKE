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
  | Default of string
  | Argument of string
  | Arguments of string list

[<StructuredFormatDisplay("Docopt.Arguments.Dictionary {SFDisplay}")>]
[<AllowNullLiteral>]
type Dictionary(options':Options) =
  class
    let dict = Dictionary<string, Result ref>()
    do for o in options' do
         let result = ref (if o.HasDefault
                           then Default(o.Default)
                           else Flag(false)) in
         match o.Short, o.Long with
         | short, null         -> dict.[String([|'-';short|])] <- result
         | Char.MaxValue, long -> dict.[String.Concat("--", long)] <- result
         | short, long         -> dict.[String([|'-';short|])] <- result;
                                  dict.[String.Concat("--", long)] <- result
       done
    member __.AsList() = [for kv in dict do yield (kv.Key, !kv.Value) done]
    member __.Item with get key' = !dict.[key']
                    and set key' value' = dict.[key'] := value'
    member xx.UnsafeAdd(key':string, ?arg':string) =
      let newval =
        if arg'.IsNone
        then match xx.[key'] with
             | None
             | Flag(false) -> Flag(true)
             | Flag(_)     -> Flags(2)
             | Flags(n)    -> Flags(n + 1)
             | value       -> value
        else match xx.[key'] with
             | None
             | Flag(_)
             | Flags(_)
             | Default(_)      -> Argument(arg'.Value)
             | Argument(arg)   -> Arguments([arg'.Value;arg])
             | Arguments(args) -> Arguments(arg'.Value::args)
             | value           -> value in
      xx.[key'] <- newval
    member xx.AddShort(o':Option, ?arg':string) =
      xx.UnsafeAdd(String([|'-';o'.Short|]), ?arg'=arg')
    member xx.AddLong(l':string, ?arg':string) =
      let predicate (o':Option) =
        if o'.Long = l'
        then (xx.UnsafeAdd(String.Concat("--", l'), ?arg'=arg'); true)
        else false
      in let predicateTruncated (o':Option) =
        if o'.Long.StartsWith(l')
        then (xx.UnsafeAdd(String.Concat("--", o'.Long), ?arg'=arg'); true)
        else false
      in if Seq.exists ( predicate ) options'
         then true
         else Seq.exists ( predicateTruncated ) options'
    member inline private xx.SFDisplay = xx.AsList()
  end
;;
