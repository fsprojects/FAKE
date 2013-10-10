[<AutoOpen>]
module Fake.BasicStringHelper

open System
open System.IO
open System.Text
open System.Collections.Generic

/// <summary>Returns if the string is null or empty</summary>
/// <user/>
let inline isNullOrEmpty value = String.IsNullOrEmpty value

/// <summary>Returns if the string is not null or empty</summary>
/// <user/>
let inline isNotNullOrEmpty value = String.IsNullOrEmpty value |> not

/// <summary>Returns if the string is null or empty or completely whitespace</summary>
/// <user/>
let inline isNullOrWhiteSpace value = isNullOrEmpty value || value |> Seq.forall Char.IsWhiteSpace

/// Replaces the given pattern in the given text with the replacement
let inline replace (pattern:string) replacement (text:string) = text.Replace(pattern,replacement)

/// Converts a sequence of strings to a string with delimiters
let inline separated delimiter (items: string seq) = String.Join(delimiter, Array.ofSeq items)

/// Removes the slashes from the end of the given string
let inline trimSlash (s:string) = s.TrimEnd('\\')

/// Splits the given string at the given delimiter
let inline split (delimiter:char) (text:string) = text.Split [|delimiter|] |> Array.toList

/// Converts a sequence of strings into a string separated with line ends
let inline toLines s = separated "\r\n" s

/// Checks wether the given text starts with the given prefix
let startsWith prefix (text:string) = text.StartsWith prefix

/// Checks wether the given text ends with the given suffix
let endsWith suffix (text:string) = text.EndsWith suffix

/// Determines whether the last character of the given <see cref="string" />
/// matches Path.DirectorySeparatorChar.         
let endsWithSlash = endsWith (Path.DirectorySeparatorChar.ToString())

let replaceFirst (pattern: string) replacement (text: string) = 
    let pos = text.IndexOf pattern
    if pos < 0
        then text
        else text.Remove(pos, pattern.Length).Insert(pos, replacement)
        
/// Appends a text
let inline append s (builder:StringBuilder) = builder.Append(sprintf "\"%s\" " s)

/// Appends a text if the predicate is true
let inline appendIfTrue p s builder = if p then append s builder else builder

/// Appends a text if the predicate is false
let inline appendIfFalse p = appendIfTrue (not p)

/// Appends a text if the value is not null
let inline appendIfNotNull (value : Object) s = 
    appendIfTrue (value <> null) (
        match value with 
        | :? String as sv -> (sprintf "%s%s" s sv)
        | _ -> (sprintf "%s%A" s value))

/// Appends a quoted text if the value is not null
let inline appendQuotedIfNotNull (value : Object) s (builder:StringBuilder) =    
    if (value = null) then builder else (
        match value with 
        | :? String as sv -> builder.Append(sprintf "%s\"%s\" " s sv)
        | _ -> builder.Append(sprintf "%s\"%A\" " s value))


/// Appends a text if the value is not null
let inline appendStringIfValueIsNotNull value = appendIfTrue (value <> null)

/// Appends a text if the value is not null or empty
let inline appendStringIfValueIsNotNullOrEmpty value = appendIfTrue (isNullOrEmpty value |> not)

/// Appends all notnull fileNames
let inline appendFileNamesIfNotNull fileNames (builder:StringBuilder) =
    fileNames 
      |> Seq.fold (fun builder file -> appendIfTrue (isNullOrEmpty file |> not) file builder) builder

let private regexes = new Dictionary<_,_>()

let getRegEx pattern =
    match regexes.TryGetValue pattern with
    | true, regex -> regex
    | _ -> (new System.Text.RegularExpressions.Regex(pattern))

let regex_replace pattern (replacement:string) text =
    (getRegEx pattern).Replace(text,replacement)

/// Checks wether the given char is a german umlaut.
let isUmlaut c = Seq.contains c ['ä'; 'ö'; 'ü'; 'Ä'; 'Ö'; 'Ü'; 'ß']

let inline toLower (s:string) = s.ToLower()

/// Returns all standard chars and digits.
let charsAndDigits = ['a'..'z'] @ ['A'..'Z'] @ ['0'..'9'] 

/// Checks wether the given char is a standard char or digit.
let isLetterOrDigit c = List.exists ((=) c) charsAndDigits

/// Trims the given string with the DirectorySeparatorChar
let inline trimSeparator (s:string) = s.TrimEnd Path.DirectorySeparatorChar

let inline trimSpecialChars (s:string) =
    s
      |> Seq.filter isLetterOrDigit
      |> Seq.filter (isUmlaut >> not)
      |> Seq.fold (fun (acc:string) c -> acc + string c) ""

/// Lifts a string to an option
let liftString x = if isNullOrEmpty x then None else Some x