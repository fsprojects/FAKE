﻿[<AutoOpen>]
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
/// Contains basic functions for string manipulation.
module Fake.StringHelper

#nowarn "44"

open System
open System.IO
open System.Text
open System.Collections.Generic

/// [omit]
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let productName () = "FAKE"

/// Returns if the string is null or empty
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline isNullOrEmpty value = String.IsNullOrEmpty value

/// Returns if the string is not null or empty
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline isNotNullOrEmpty value = String.IsNullOrEmpty value |> not

/// Returns if the string is null or empty or completely whitespace
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline isNullOrWhiteSpace value =
    isNullOrEmpty value || value |> Seq.forall Char.IsWhiteSpace

/// Replaces the given pattern in the given text with the replacement
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline replace (pattern: string) replacement (text: string) = text.Replace(pattern, replacement)

/// Converts a sequence of strings to a string with delimiters
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline separated delimiter (items: string seq) =
    String.Join(delimiter, Array.ofSeq items)

/// Removes the slashes from the end of the given string
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline trimSlash (s: string) = s.TrimEnd('\\')

/// Splits the given string at the given char delimiter
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline split (delimiter: char) (text: string) =
    text.Split [| delimiter |] |> Array.toList

[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline splitRemove (delimiter: char) (text: string) =
    text.Split([| delimiter |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.toList

/// Splits the given string at the given string delimiter
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline splitStr (delimiterStr: string) (text: string) =
    text.Split([| delimiterStr |], StringSplitOptions.None) |> Array.toList

/// Converts a sequence of strings into a string separated with line ends
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline toLines text = separated Environment.NewLine text

/// Checks whether the given text starts with the given prefix
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let startsWith prefix (text: string) = text.StartsWith prefix

/// Checks whether the given text ends with the given suffix
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let endsWith suffix (text: string) = text.EndsWith suffix

/// Determines whether the last character of the given <see cref="string" />
/// matches Path.DirectorySeparatorChar.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let endsWithSlash = endsWith (Path.DirectorySeparatorChar.ToString())

/// Replaces the first occurrence of the pattern with the given replacement.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let replaceFirst (pattern: string) replacement (text: string) =
    let pos = text.IndexOf pattern

    if pos < 0 then
        text
    else
        text.Remove(pos, pattern.Length).Insert(pos, replacement)

/// Appends a text to a StringBuilder.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline append text (builder: StringBuilder) = builder.Append(sprintf "\"%s\" " text)

/// Appends a text to a StringBuilder without surrounding quotes.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendWithoutQuotes (text: string) (builder: StringBuilder) = builder.Append(sprintf "%s " text)

/// Appends string of function value if option has some value
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendIfSome o f builder =
    match o with
    | Some(value) -> appendWithoutQuotes (f value) builder
    | None -> builder

/// Appends a text if the predicate is true.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendIfTrue p s builder = if p then append s builder else builder

[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendIfTrueWithoutQuotes p s builder =
    if p then appendWithoutQuotes s builder else builder

/// Appends a text if the predicate is false.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendIfFalse p = appendIfTrue (not p)

/// Appends a text without quoting if the value is not null.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendWithoutQuotesIfNotNull (value: Object) s =
    appendIfTrueWithoutQuotes
        (value <> null)
        (match value with
         | :? String as sv -> (sprintf "%s%s" s sv)
         | _ -> (sprintf "%s%A" s value))

/// Appends a text if the value is not null.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendIfNotNull (value: Object) s =
    appendIfTrue
        (value <> null)
        (match value with
         | :? String as sv -> (sprintf "%s%s" s sv)
         | _ -> (sprintf "%s%A" s value))

/// Appends a quoted text if the value is not null.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendQuotedIfNotNull (value: Object) s (builder: StringBuilder) =
    if (value = null) then
        builder
    else
        (match value with
         | :? String as sv -> builder.Append(sprintf "%s\"%s\" " s sv)
         | _ -> builder.Append(sprintf "%s\"%A\" " s value))

/// Appends a text if the value is not null.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendStringIfValueIsNotNull value = appendIfTrue (value <> null)

/// Appends a text if the value is not null or empty.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendStringIfValueIsNotNullOrEmpty value =
    appendIfTrue (isNullOrEmpty value |> not)

/// Appends a text if the value is not null or empty.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendIfNotNullOrEmpty value s =
    appendIfTrue (isNotNullOrEmpty value) (sprintf "%s%s" s value)

/// Appends all notnull fileNames.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline appendFileNamesIfNotNull fileNames (builder: StringBuilder) =
    fileNames
    |> Seq.fold (fun builder file -> appendIfTrue (isNullOrEmpty file |> not) file builder) builder

/// Applies action on builder for each element of list.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline forEach items action text (builder: StringBuilder) =
    items |> List.iter (fun t -> builder |> (action t text) |> ignore)

    builder

/// Returns the text from the StringBuilder
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline toText (builder: StringBuilder) = builder.ToString()

/// [omit]
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let private regexes = new Dictionary<_, _>()

/// [omit]
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let getRegEx pattern =
    match regexes.TryGetValue pattern with
    | true, regex -> regex
    | _ -> (new System.Text.RegularExpressions.Regex(pattern))

/// [omit]
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let regex_replace pattern (replacement: string) text =
    (getRegEx pattern).Replace(text, replacement)

/// Checks whether the given char is a german umlaut.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let isUmlaut c =
    Seq.exists ((=) c) [ 'ä'; 'ö'; 'ü'; 'Ä'; 'Ö'; 'Ü'; 'ß' ]

/// Converts all characters in a string to lower case.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline toLower (s: string) = s.ToLower()

/// Returns all standard chars and digits.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let charsAndDigits = [ 'a' .. 'z' ] @ [ 'A' .. 'Z' ] @ [ '0' .. '9' ]

/// Checks whether the given char is a standard char or digit.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let isLetterOrDigit c = List.exists ((=) c) charsAndDigits

/// Trims the given string with the DirectorySeparatorChar
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline trimSeparator (s: string) = s.TrimEnd Path.DirectorySeparatorChar

/// Trims all special characters from a string.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline trimSpecialChars (text: string) =
    text
    |> Seq.filter isLetterOrDigit
    |> Seq.filter (isUmlaut >> not)
    |> Seq.fold (fun (acc: string) c -> acc + string c) ""

/// Trims the given string
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline trim (x: string) = if isNullOrEmpty x then x else x.Trim()

/// Trims the given string
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline trimChars chars (x: string) =
    if isNullOrEmpty x then x else x.Trim chars

/// Trims the start of the given string
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline trimStartChars chars (x: string) =
    if isNullOrEmpty x then x else x.TrimStart chars

/// Trims the end of the given string
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline trimEndChars chars (x: string) =
    if isNullOrEmpty x then x else x.TrimEnd chars

/// Lifts a string to an option
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let liftString x =
    if isNullOrEmpty x then None else Some x

/// Reads a file line by line
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.read`/`File.readWithEncoding` instead (encoding parameter added, default to UTF8)">]
let ReadFile (file: string) =
    seq {
        use textReader = new StreamReader(file, encoding)

        while not textReader.EndOfStream do
            yield textReader.ReadLine()
    }

/// Reads the first line of a file. This can be helpful to read a password from file.
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.readLine`/`File.readLineWithEncoding` instead (encoding parameter added, default to UTF8)">]
let ReadLine (file: string) =
    use sr = new StreamReader(file, Encoding.Default)
    sr.ReadLine()

/// Writes a file line by line
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.write`/`File.writeWithEncoding` instead (encoding parameter added, default to UTF8)">]
let WriteToFile append fileName (lines: seq<string>) =
    let fi = fileInfo fileName
    use writer = new StreamWriter(fileName, append && fi.Exists, encoding)
    lines |> Seq.iter writer.WriteLine

/// Removes all trailing .0 from a version string
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let rec NormalizeVersion (version: string) =
    if version = null then
        ""
    else
        let elements = version.Split [| '.' |]
        let mutable version = ""

        for i in 0..3 do
            if i < elements.Length then
                if version = "" then
                    version <- elements.[i]
                else
                    version <- version + "." + elements.[i]

        if version.EndsWith ".0" then
            version.Remove(version.Length - 2, 2) |> NormalizeVersion
        else
            version

/// Writes a byte array to a file
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.writeBytes` instead">]
let WriteBytesToFile file bytes = File.WriteAllBytes(file, bytes)

/// Writes a string to a file
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.writeString`/`File.writeStringWithEncoding` instead (encoding parameter added, default to UTF8)">]
let WriteStringToFile append fileName (text: string) =
    let fi = fileInfo fileName
    use writer = new StreamWriter(fileName, append && fi.Exists, encoding)
    writer.Write text

/// Replaces the file with the given string
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.replaceContent` instead">]
let ReplaceFile fileName text =
    let fi = fileInfo fileName

    if fi.Exists then
        fi.IsReadOnly <- false
        fi.Delete()

    WriteStringToFile false fileName text

[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let Colon = ','

/// Writes a file line by line
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.writeNew` instead">]
let WriteFile file lines = WriteToFile false file lines

/// Appends all lines to a file line by line
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.append` instead">]
let AppendToFile file lines = WriteToFile true file lines

/// Reads a file as one text
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.readAsString`/`File.readAsStringWithEncoding` instead">]
let inline ReadFileAsString file = File.ReadAllText(file, encoding)

/// Reads a file as one array of bytes
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.readAsBytes` instead">]
let ReadFileAsBytes file = File.ReadAllBytes file

/// Replaces any occurence of the currentDirectory with .
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline shortenCurrentDirectory value = replace currentDirectory "." value

/// Checks whether the given text starts with the given prefix
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline (<*) prefix text = startsWith prefix text

/// Replaces the text in the given file
[<System.Obsolete "FAKE0001 use `open Fake.IO` and `File.applyReplace` instead">]
let ReplaceInFile replaceF fileName =
    fileName |> ReadFileAsString |> replaceF |> ReplaceFile fileName

/// Represents Linux line breaks
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let LinuxLineBreaks = "\n"

/// Represents Windows line breaks
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let WindowsLineBreaks = "\r\n"

/// Represents Mac line breaks
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let MacLineBreaks = "\r"

/// Converts all line breaks in a text to windows line breaks
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let ConvertTextToWindowsLineBreaks text =
    text
    |> replace WindowsLineBreaks LinuxLineBreaks
    |> replace MacLineBreaks LinuxLineBreaks
    |> replace LinuxLineBreaks WindowsLineBreaks

/// Reads a file line by line and replaces all line breaks to windows line breaks
///   - uses a temp file to store the contents in order to prevent OutOfMemory exceptions
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let ConvertFileToWindowsLineBreaks (fileName: string) =
    use reader = new StreamReader(fileName, encoding)
    let tempFileName = Path.GetTempFileName()
    use writer = new StreamWriter(tempFileName, false, encoding)

    while not reader.EndOfStream do
        reader.ReadLine() |> ConvertTextToWindowsLineBreaks |> writer.WriteLine

    reader.Close()
    writer.Close()
    File.Delete(fileName)
    File.Move(tempFileName, fileName)

/// Removes linebreaks from the given string
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline RemoveLineBreaks text =
    text |> replace "\r" String.Empty |> replace "\n" String.Empty

/// Encapsulates the Apostrophe
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline EncapsulateApostrophe text = replace "'" "`" text

/// A cache of relative path names.
/// [omit]
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let relativePaths = new Dictionary<_, _>()

/// <summary>Produces relative path when possible to go from baseLocation to targetLocation.</summary>
/// <param name="baseLocation">The root folder</param>
/// <param name="targetLocation">The target folder</param>
/// <returns>The relative path relative to baseLocation</returns>
/// <exception cref="ArgumentNullException">base or target locations are null or empty</exception>
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let ProduceRelativePath baseLocation targetLocation =
    if isNullOrEmpty baseLocation then
        raise (new ArgumentNullException "baseLocation")

    if isNullOrEmpty targetLocation then
        raise (new ArgumentNullException "targetLocation")

    if not <| Path.IsPathRooted baseLocation then
        baseLocation
    else if not <| Path.IsPathRooted targetLocation then
        targetLocation
    else if
        String.Compare(Path.GetPathRoot baseLocation, Path.GetPathRoot targetLocation, true)
        <> 0
    then
        targetLocation
    else if String.Compare(baseLocation, targetLocation, true) = 0 then
        "."
    else
        let resultPath = ref "."

        let targetLocation =
            if targetLocation |> endsWith directorySeparator then
                targetLocation
            else
                targetLocation + directorySeparator

        let baseLocation =
            if baseLocation |> endsWith directorySeparator then
                ref (baseLocation.Substring(0, baseLocation.Length - 1))
            else
                ref baseLocation

        while not
              <| targetLocation.StartsWith(!baseLocation + directorySeparator, StringComparison.OrdinalIgnoreCase) do
            resultPath := !resultPath + directorySeparator + ".."
            baseLocation := Path.GetDirectoryName !baseLocation

            if (!baseLocation) |> endsWith directorySeparator then
                baseLocation := (!baseLocation).Substring(0, (!baseLocation).Length - 1)

        resultPath
        := (!resultPath + targetLocation.Substring((!baseLocation).Length))
           |> replace (directorySeparator + directorySeparator) directorySeparator
        // preprocess .\..\ case
        if (sprintf ".%s..%s" directorySeparator directorySeparator) <* (!resultPath) then
            (!resultPath).Substring(2, (!resultPath).Length - 3)
        else
            (!resultPath).Substring(0, (!resultPath).Length - 1)

/// Replaces the absolute path to a relative path.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let inline toRelativePath value =
    match relativePaths.TryGetValue value with
    | true, x -> x
    | _ ->
        let x = ProduceRelativePath currentDirectory value
        relativePaths.Add(value, x)
        x

/// Find a regex pattern in a text and replaces it with the given replacement.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let (>=>) pattern replacement text = regex_replace pattern replacement text

/// Determines if a text matches a given regex pattern.
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let (>**) pattern text = (getRegEx pattern).IsMatch text

/// Decodes a Base64-encoded UTF-8-encoded string
[<System.Obsolete "FAKE0001 use Fake.Core.String instead">]
let DecodeBase64Utf8String (text: string) =
    text |> Convert.FromBase64String |> Encoding.UTF8.GetString
