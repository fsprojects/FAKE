namespace Docopt.Token
#nowarn "62"
#nowarn "52"
#light "off"

open System

type Argument =
  class
    val Type : Type
    val mutable Dflt : string
    new(type', val') = { Type=type'; Dflt=val'; }
    new(type', val') = match type' with
      | None        -> Argument(typeof<string>, val')
      | Some(tname) -> match tname with
        | "bool"    -> Argument(typeof<bool>, val')
        | "int"     -> Argument(typeof<int32>, val')
        | "uint"    -> Argument(typeof<uint32>, val')
        | "float"   -> Argument(typeof<System.Single>, val')
        | "double"  -> Argument(typeof<System.Double>, val')
        | "decimal" -> Argument(typeof<decimal>, val')
        | "string"  -> Argument(typeof<string>, val')
        | "time"
        | "date"    -> Argument(typeof<System.DateTime>, val')
        | _         -> Argument(Type.GetType(tname), val')
    new(type':option<_>) = Argument(type', null)
    member xx.MutateDflt(val') = xx.Dflt <- val'
    static member Merge(lhs':Argument, rhs':Argument) =
      let tyρe = if Unchecked.equals lhs'.Type rhs'.Type then lhs'.Type
                 else invalidArg null "Different type" in
      let dflt = if lhs'.Dflt = rhs'.Dflt then lhs'.Dflt
                 else invalidArg null "Different default value" in
      Argument(tyρe, dflt)
    override xx.ToString() =
      sprintf "Argument { Type = %A; Dflt = %A }"
        xx.Type xx.Dflt
  end
;;

[<NoComparison>]
type Option =
  struct
    val Sname : char
    val Lname : string
    val Arg : Argument option
    new(sname', lname', arg') = { Sname=sname'; Lname=lname'; Arg=arg'; }
    static member Default = Option('\000', null, None)
    override xx.ToString() =
      sprintf "Option { Sname = %A; Lname = %A; Arg = %A }"
        xx.Sname xx.Lname xx.Arg
    member xx.MutateArgDflt(val':string) =
      match xx.Arg with
        | Some(arg) -> arg.MutateDflt(val')
        | _         -> ()
    member xx.IsDefault =
      xx.Sname = '\000' && xx.Lname = null && xx.Arg.IsNone
  end
;;
