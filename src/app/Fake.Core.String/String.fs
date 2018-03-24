/// Contains basic functions for string manipulation.
[<RequireQualifiedAccess>]
module Fake.Core.String
type String = System.String

open System
open System.IO
open System.Text
open System.Collections.Generic

/// [omit]
let productName() = "FAKE"

/// Returns if the string is null or empty
let inline isNullOrEmpty value = String.IsNullOrEmpty value

/// Returns if the string is not null or empty
let inline isNotNullOrEmpty value = String.IsNullOrEmpty value |> not

/// Returns if the string is null or empty or completely whitespace
let inline isNullOrWhiteSpace value = isNullOrEmpty value || value |> Seq.forall Char.IsWhiteSpace

/// Replaces the given pattern in the given text with the replacement
let inline replace (pattern : string) replacement (text : string) = text.Replace(pattern, replacement)

/// Converts a sequence of strings to a string with delimiters
let inline separated delimiter (items : string seq) = String.Join(delimiter, Array.ofSeq items)

/// Removes the slashes from the end of the given string
let inline trimSlash (s : string) = s.TrimEnd('\\')

/// Splits the given string at the given char delimiter
let inline split (delimiter : char) (text : string) = text.Split [| delimiter |] |> Array.toList

/// Splits the given string at the given string delimiter
let inline splitStr (delimiterStr : string) (text : string) = 
    text.Split([| delimiterStr |], StringSplitOptions.None) |> Array.toList

/// Converts a sequence of strings into a string separated with line ends
let inline toLines text = separated Environment.NewLine text

/// Checks whether the given text starts with the given prefix
let startsWith prefix (text : string) = text.StartsWith prefix

/// Checks whether the given text ends with the given suffix
let endsWith suffix (text : string) = text.EndsWith suffix

/// Determines whether the last character of the given <see cref="string" />
/// matches Path.DirectorySeparatorChar.         
let endsWithSlash = endsWith (Path.DirectorySeparatorChar.ToString())

/// Replaces the first occurrence of the pattern with the given replacement.
let replaceFirst (pattern : string) replacement (text : string) = 
    let pos = text.IndexOf pattern
    if pos < 0 then text
    else text.Remove(pos, pattern.Length).Insert(pos, replacement)

/// [omit]
let private regexes = new Dictionary<_, _>()

/// [omit]
let getRegEx pattern = 
    match regexes.TryGetValue pattern with
    | true, regex -> regex
    | _ -> (new System.Text.RegularExpressions.Regex(pattern))

/// [omit]
let regex_replace pattern (replacement : string) text = (getRegEx pattern).Replace(text, replacement)

/// Checks whether the given char is a german umlaut.
let isUmlaut c = Seq.exists ((=) c) [ 'ä'; 'ö'; 'ü'; 'Ä'; 'Ö'; 'Ü'; 'ß' ]

/// Converts all characters in a string to lower case.
let inline toLower (s : string) = s.ToLower()

/// Returns all standard chars and digits.
let charsAndDigits = [ 'a'..'z' ] @ [ 'A'..'Z' ] @ [ '0'..'9' ]

/// Checks whether the given char is a standard char or digit.
let isLetterOrDigit c = List.exists ((=) c) charsAndDigits

/// Trims the given string with the DirectorySeparatorChar
let inline trimSeparator (s : string) = s.TrimEnd Path.DirectorySeparatorChar

/// Trims all special characters from a string.
let inline trimSpecialChars (text : string) = 
    text
    |> Seq.filter isLetterOrDigit
    |> Seq.filter (isUmlaut >> not)
    |> Seq.fold (fun (acc : string) c -> acc + string c) ""

/// Trims the given string
let inline trim (x : string) = 
    if isNullOrEmpty x then x
    else x.Trim()

/// Trims the given string
let inline trimChars chars (x : string) = 
    if isNullOrEmpty x then x
    else x.Trim chars

/// Trims the start of the given string
let inline trimStartChars chars (x : string) =
    if isNullOrEmpty x then x
    else x.TrimStart chars

/// Trims the end of the given string
let inline trimEndChars chars (x : string) =
    if isNullOrEmpty x then x
    else x.TrimEnd chars

/// Lifts a string to an option
let liftString x = 
    if isNullOrEmpty x then None
    else Some x


/// Removes all trailing .0 from a version string
let rec NormalizeVersion(version : string) =
    if version = null then "" else
    let elements = version.Split [| '.' |]
    let mutable version = ""
    for i in 0..3 do
        if i < elements.Length then 
            if version = "" then version <- elements.[i]
            else version <- version + "." + elements.[i]
    if version.EndsWith ".0" then version.Remove(version.Length - 2, 2) |> NormalizeVersion
    else version

let Colon = ','


/// Represents Linux line breaks
let LinuxLineBreaks = "\n"

/// Represents Windows line breaks
let WindowsLineBreaks = "\r\n"

/// Represents Mac line breaks
let MacLineBreaks = "\r"

/// Converts all line breaks in a text to windows line breaks
let convertTextToWindowsLineBreaks text = 
    text
    |> replace WindowsLineBreaks LinuxLineBreaks
    |> replace MacLineBreaks LinuxLineBreaks
    |> replace LinuxLineBreaks WindowsLineBreaks

/// Reads a file line by line and replaces all line breaks to windows line breaks
///   - uses a temp file to store the contents in order to prevent OutOfMemory exceptions
let convertFileToWindowsLineBreaksWithEncoding (encoding:System.Text.Encoding) (fileName : string) =
    let tempFileName = Path.GetTempFileName()
    ( use file = File.OpenRead fileName 
      use reader = new StreamReader(file, encoding)
      ( use tempFile = File.Open(tempFileName, FileMode.Create)
        use writer = new StreamWriter(tempFile, encoding)
        while not reader.EndOfStream do
            reader.ReadLine()
            |> convertTextToWindowsLineBreaks
            |> writer.WriteLine))
    File.Delete(fileName)
    File.Move(tempFileName, fileName)
    
let convertFileToWindowsLineBreak (encoding:System.Text.Encoding) (fileName : string) =
  convertFileToWindowsLineBreaksWithEncoding Encoding.UTF8 fileName

/// Removes linebreaks from the given string
let inline removeLineBreaks text = 
    text
    |> replace "\r" String.Empty
    |> replace "\n" String.Empty

/// Encapsulates the Apostrophe
let inline encapsulateApostrophe text = replace "'" "`" text

/// Decodes a Base64-encoded UTF-8-encoded string
let decodeBase64Utf8String(text : string) = 
    text
    |> Convert.FromBase64String
    |> Encoding.UTF8.GetString

module Operators =
    /// Checks whether the given text starts with the given prefix
    let inline (<*) prefix text = startsWith prefix text
    
    /// Find a regex pattern in a text and replaces it with the given replacement.
    let (>=>) pattern replacement text = regex_replace pattern replacement text

    /// Determines if a text matches a given regex pattern.
    let (>**) pattern text = (getRegEx pattern).IsMatch text
