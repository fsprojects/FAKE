[<AutoOpen>]
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.HttpHeaderHelper
open System
open System.Text.RegularExpressions

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let toHeaderValue (values:string []) : string = 
    values
    |> Array.map (fun x -> 
        x.Replace("\"", "%22")
        |> sprintf "\"%s\""
    )
    |> String.concat ","

let private regex = Regex("(\"[^\"]*\")(?:,(\"[^\"]*\"))*", RegexOptions.Compiled)

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let fromHeaderValue (value: string) = 
    match regex.Matches value |> Seq.cast<Match> |> Seq.toList with
    | [] ->
        //back compat: existing agents not expecting quoted params will continue to function.
        [|value|]
    | matches ->
        match [ for m in matches do
                    for x in m.Groups -> x ] with
        | _ :: gs ->
            [| for g in gs do
                    for c in g.Captures do
                    yield c.Value.[1..c.Value.Length - 2].Replace("%22", "\"") |]
        | _ -> [|value|]
        
    
    