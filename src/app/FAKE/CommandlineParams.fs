module CommandlineParams

open Fake

let printAllParams() = printfn "FAKE.exe [buildScript] [Target] Variable1=Value1 Variable2=Value2 ... "

(* 
    This is a set of flags that exist in code lower in the target processing that MUST be normalized to a certain form. This is necessary because
    If any old styole variables are present in the FAKE invocation, Argu parsing will fail and we will not have parsed out those commands correctly
    from the overall command line.

    You can typically find usages of 'hasBuildParam' in this codebase to find places where these values are required.
*)
let specialFlags = 
    [
        "-st", "single-target"
        "--single-target", "single-target" 
        "-pd", "details"
        "--print-details", "details"
    ] |> Map.ofList

let parseArgs cmdArgs = 
    let (|KeyValue|Flag|TargetName|) ((i,arg) : int * string) =
        if i = 0 then TargetName arg
        else
            match arg.IndexOf '=' with
            | -1 -> Flag arg
            | i -> KeyValue (arg.Substring(0, i), arg.Substring(i + 1, arg.Length - i - 1))

    cmdArgs
    |> Seq.skip 1
    |> Seq.mapi (fun i a -> match (i, a) with 
                            | TargetName t -> "target", t
                            | Flag f when Map.containsKey f specialFlags -> Map.find f specialFlags, "true"
                            | Flag f -> f, "true"
                            | KeyValue (k,v) when k = "logfile" -> addXmlListener v; (k,v)
                            | KeyValue kvp -> kvp )
    |> Seq.toList