module CommandlineParams

open Fake

let printAllParams() = printfn "FAKE.exe [buildScript]"
let parseArgs cmdArgs =
    let splitter = [|'='|]
    cmdArgs 
        |> Seq.skip 1
        |> Seq.map (fun (a:string) ->
                if a.Contains "=" then
                    let s = a.Split splitter
                    if s.[0] = "logfile" then
                        addXmlListener s.[1]
                    s.[0], s.[1]
                else
                    a,"1")
        |> Seq.toList