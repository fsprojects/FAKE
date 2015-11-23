namespace Docopt.Token
#nowarn "62"
#light "off"

type Argument =
  struct
    val Name : string
    val Type : System.Type
    val Default : obj
    new(name', type', val') = { Name=name'; Type=type'; Default=val'; }
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
        | _         -> Argument(name', System.Type.GetType(tname), val')
    new(name', type':option<_>) = Argument(name', type', null)
    override xx.ToString() =
      sprintf "Argument { Name = %s; Type = %A; Default = %A }"
        xx.Name xx.Type xx.Default
  end
;;

type Option =
  struct
    val Sname : char
    val Lname : string
    val Arg : Argument option
    new(sname', lname', arg') = { Sname=sname'; Lname=lname'; Arg=arg'; }
    override xx.ToString() =
      sprintf "Option { Sname = %c; Lname = %s; Arg = %A"
        xx.Sname xx.Lname xx.Arg
  end
;;
