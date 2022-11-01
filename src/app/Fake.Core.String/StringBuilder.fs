namespace Fake.Core

/// <summary>
/// FAKE's StringBuilder module.
/// </summary>
[<RequireQualifiedAccess>]
module StringBuilder =
    type String = System.String
    type StringBuilder = System.Text.StringBuilder

    open Fake.Core
    open System
    open System.Text

    /// <summary>
    /// Appends a text to a StringBuilder.
    /// </summary>
    let inline append text (builder: StringBuilder) = builder.Append(sprintf "\"%s\" " text)

    /// <summary>
    /// Appends a text to a StringBuilder without surrounding quotes.
    /// </summary>
    let inline appendWithoutQuotes (text: string) (builder: StringBuilder) = builder.Append(sprintf "%s " text)

    /// <summary>
    /// Appends string of function value if option has some value
    /// </summary>
    let inline appendIfSome o f builder =
        match o with
        | Some value -> appendWithoutQuotes (f value) builder
        | None -> builder

    /// <summary>
    /// Appends a text if the predicate is true.
    /// </summary>
    let inline appendIfTrue p s builder = if p then append s builder else builder

    let inline appendIfTrueWithoutQuotes p s builder =
        if p then appendWithoutQuotes s builder else builder

    /// <summary>
    /// Appends a text if the predicate is false.
    /// </summary>
    let inline appendIfFalse p = appendIfTrue (not p)

    /// <summary>
    /// Appends a text without quoting if the value is not null.
    /// </summary>
    let inline appendWithoutQuotesIfNotNull (value: Object) s =
        appendIfTrueWithoutQuotes
            (value <> null)
            (match value with
             | :? String as sv -> (sprintf "%s%s" s sv)
             | _ -> (sprintf "%s%A" s value))

    /// <summary>
    /// Appends a text if the value is not null.
    /// </summary>
    let inline appendIfNotNull (value: Object) s =
        appendIfTrue
            (value <> null)
            (match value with
             | :? String as sv -> (sprintf "%s%s" s sv)
             | _ -> (sprintf "%s%A" s value))

    /// <summary>
    /// Appends a quoted text if the value is not null.
    /// </summary>
    let inline appendQuotedIfNotNull (value: Object) s (builder: StringBuilder) =
        if (value = null) then
            builder
        else
            (match value with
             | :? String as sv -> builder.Append(sprintf "%s\"%s\" " s sv)
             | _ -> builder.Append(sprintf "%s\"%A\" " s value))

    /// <summary>
    /// Appends a text if the value is not null.
    /// </summary>
    let inline appendStringIfValueIsNotNull value = appendIfTrue (not (isNull value))

    /// <summary>
    /// Appends a text if the value is not null or empty.
    /// </summary>
    let inline appendStringIfValueIsNotNullOrEmpty value =
        appendIfTrue (String.isNullOrEmpty value |> not)

    /// <summary>
    /// Appends a text if the value is not null or empty.
    /// </summary>
    let inline appendIfNotNullOrEmpty value s =
        appendIfTrue (String.isNotNullOrEmpty value) (sprintf "%s%s" s value)

    /// <summary>
    /// Appends all notnull fileNames.
    /// </summary>
    let inline appendFileNamesIfNotNull fileNames (builder: StringBuilder) =
        fileNames
        |> Seq.fold (fun builder file -> appendIfTrue (String.isNullOrEmpty file |> not) file builder) builder

    /// <summary>
    /// Applies action on builder for each element of list.
    /// </summary>
    let inline forEach items action text (builder: StringBuilder) =
        items |> List.iter (fun t -> builder |> (action t text) |> ignore)

        builder

    /// <summary>
    /// Returns the text from the StringBuilder
    /// </summary>
    let inline toText (builder: StringBuilder) = builder.ToString()
