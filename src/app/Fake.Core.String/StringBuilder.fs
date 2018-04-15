[<RequireQualifiedAccess>]
module Fake.Core.StringBuilder
type String = System.String
type StringBuilder = System.Text.StringBuilder

open Fake.Core
open System
open System.IO
open System.Text
open System.Collections.Generic


/// Appends a text to a StringBuilder.
let inline append text (builder : StringBuilder) = builder.Append(sprintf "\"%s\" " text)

/// Appends a text to a StringBuilder without surrounding quotes.
let inline appendWithoutQuotes (text : string) (builder : StringBuilder) = builder.Append(sprintf "%s " text)

/// Appends string of function value if option has some value
let inline appendIfSome o f builder = 
    match o with
    | Some(value) -> appendWithoutQuotes (f value) builder
    | None -> builder

/// Appends a text if the predicate is true.
let inline appendIfTrue p s builder = 
    if p then append s builder
    else builder

let inline appendIfTrueWithoutQuotes p s builder = 
    if p then appendWithoutQuotes s builder
    else builder

/// Appends a text if the predicate is false.
let inline appendIfFalse p = appendIfTrue (not p)

/// Appends a text without quoting if the value is not null.
let inline appendWithoutQuotesIfNotNull (value : Object) s = 
    appendIfTrueWithoutQuotes 
        (value <> null) 
        (match value with
         | :? String as sv -> (sprintf "%s%s" s sv)
         | _ -> (sprintf "%s%A" s value))

/// Appends a text if the value is not null.
let inline appendIfNotNull (value : Object) s = 
    appendIfTrue 
        (value <> null) 
        (match value with
         | :? String as sv -> (sprintf "%s%s" s sv)
         | _ -> (sprintf "%s%A" s value))

/// Appends a quoted text if the value is not null.
let inline appendQuotedIfNotNull (value : Object) s (builder : StringBuilder) = 
    if (value = null) then builder
    else 
        (match value with
         | :? String as sv -> builder.Append(sprintf "%s\"%s\" " s sv)
         | _ -> builder.Append(sprintf "%s\"%A\" " s value))

/// Appends a text if the value is not null.
let inline appendStringIfValueIsNotNull value = appendIfTrue (not (isNull value))

/// Appends a text if the value is not null or empty.
let inline appendStringIfValueIsNotNullOrEmpty value = appendIfTrue (String.isNullOrEmpty value |> not)

/// Appends a text if the value is not null or empty.
let inline appendIfNotNullOrEmpty value s = appendIfTrue (String.isNotNullOrEmpty value) (sprintf "%s%s" s value)

/// Appends all notnull fileNames.
let inline appendFileNamesIfNotNull fileNames (builder : StringBuilder) = 
    fileNames |> Seq.fold (fun builder file -> appendIfTrue (String.isNullOrEmpty file |> not) file builder) builder

/// Applies action on builder for each element of list.
let inline forEach items action text (builder: StringBuilder) =
    items
    |> List.iter (fun t -> builder |> (action t text) |> ignore)

    builder

/// Returns the text from the StringBuilder
let inline toText (builder : StringBuilder) = builder.ToString()
