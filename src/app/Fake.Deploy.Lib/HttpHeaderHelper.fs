[<AutoOpen>]
module Fake.HttpHeaderHelper
open System
open System.Text.RegularExpressions

let toHeaderValue (values:string []) : string = 
    values
    |> Array.map (fun x -> 
        x.Replace("\"", "%22")
        |> sprintf "\"%s\""
    )
    |> fun strs -> System.String.Join(",", strs
    )

let private regex = Regex("(\"[^\"]*\")(?:,(\"[^\"]*\"))*", RegexOptions.Compiled)
let fromHeaderValue (value:string) : string [] = 
    let matches = regex.Matches(value)
    //back compat: existing agents not expecting quoted params will continue to function.
    if matches.Count = 0 then [|value|]
    else
    matches |> Seq.cast
    |> Seq.collect (fun (m:Match) -> m.Groups |> Seq.cast)
    |> Seq.skip 1
    |> Seq.collect (fun (g:Group) ->
        g.Captures |> Seq.cast |> Seq.map (fun (c:Capture) -> c.Value)
        |> Seq.map (fun (x:string) -> 
            x.Substring(1, x.Length - 2)
            |> fun y -> y.Replace("%22", "\"")
        )
    )
    |> Array.ofSeq
        
    
    