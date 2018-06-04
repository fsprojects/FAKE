[<AutoOpen>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.HttpHeaderHelper
open System
open System.Text.RegularExpressions

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let toHeaderValue (values:string []) : string = 
    values
    |> Array.map (fun x -> 
        x.Replace("\"", "%22")
        |> sprintf "\"%s\""
    )
    |> String.concat ","

let private regex = Regex("(\"[^\"]*\")(?:,(\"[^\"]*\"))*", RegexOptions.Compiled)

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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
        
    
    