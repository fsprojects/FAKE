module CommandlineParams

open Fake

let printAllParams() = printfn "FAKE.exe [buildScript] [Target] Variable1=Value1 Variable2=Value2 ... "
let parseArgs cmdArgs =
    let splitter = [|'='|]
    cmdArgs 
        |> Seq.skip 1
        |> Seq.mapi (fun (i:int) (arg:string) ->
                if arg.Contains "=" then
                    let s = arg.Split splitter
                    if s.[0] = "logfile" then
                        addXmlListener s.[1]
                    s.[0], s.[1]
                else
                    if i = 0 then
                        "Target", arg
                    else
                        arg, "")
        |> Seq.toList