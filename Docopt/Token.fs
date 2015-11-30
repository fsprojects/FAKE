namespace Docopt.Token
#nowarn "62"
#nowarn "52"
#light "off"

open System

type Argument =
  class
    val Name : string
    val Type : Type
    val mutable Dflt : string
    new(name', type', val') = { Name=name'; Type=type'; Dflt=val'; }
    new(name', type', val') = match type' with
      | None        -> Argument(name', typeof<string>, val')
      | Some(tname) -> match tname with
        | "bool"    -> Argument(name', typeof<bool>, val')
        | "int"     -> Argument(name', typeof<int32>, val')
        | "uint"    -> Argument(name', typeof<uint32>, val')
        | "float"   -> Argument(name', typeof<System.Single>, val')
        | "double"  -> Argument(name', typeof<System.Double>, val')
        | "decimal" -> Argument(name', typeof<decimal>, val')
        | "string"  -> Argument(name', typeof<string>, val')
        | "time"
        | "date"    -> Argument(name', typeof<System.DateTime>, val')
        | _         -> Argument(name', Type.GetType(tname), val')
    new(name', type':option<_>) = Argument(name', type', null)
    member xx.MutateDflt(val') = xx.Dflt <- val'
    member private xx.TrueName =
      (fun c' ->
         if Char.IsLetter(c')
         then Some(Char.ToUpper(c'))
         elif Char.IsDigit(c')
         then Some(c')
         else None)
      |> Seq.choose
      >> String.Concat
      <| xx.Name
    static member Merge(lhs':Argument, rhs':Argument) =
      let name = if lhs'.Name = rhs'.Name then lhs'.Name
                 elif lhs'.TrueName = rhs'.TrueName then lhs'.Name
                 else invalidArg null "Different name" in
      let tyρe = if Unchecked.equals lhs'.Type rhs'.Type then lhs'.Type
                 else invalidArg null "Different type" in
      let dflt = if lhs'.Dflt = rhs'.Dflt then lhs'.Dflt
                 else invalidArg null "Different default value" in
      Argument(name, tyρe, dflt)
    override xx.ToString() =
      sprintf "Argument { Name = %s; Type = %A; Dflt = %A }"
        xx.Name xx.Type xx.Dflt
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
