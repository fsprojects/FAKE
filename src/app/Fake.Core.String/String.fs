namespace Fake.Core

/// <summary>
/// Contains basic functions for string manipulation.
/// </summary>
[<RequireQualifiedAccess>]
module String =
    type String = System.String

    open System
    open System.IO
    open System.Text
    open System.Collections.Generic

    /// [omit]
    let productName () = "FAKE"

    /// <summary>
    /// Returns if the string is null or empty
    /// </summary>
    let inline isNullOrEmpty value = String.IsNullOrEmpty value

    /// <summary>
    /// Returns if the string is not null or empty
    /// </summary>
    let inline isNotNullOrEmpty value = String.IsNullOrEmpty value |> not

    /// <summary>
    /// Returns if the string is null or empty or completely whitespace
    /// </summary>
    let inline isNullOrWhiteSpace value =
        isNullOrEmpty value || value |> Seq.forall Char.IsWhiteSpace

    /// <summary>
    /// Replaces the given pattern in the given text with the replacement
    /// </summary>
    let inline replace (pattern: string) replacement (text: string) = text.Replace(pattern, replacement)

    /// <summary>
    /// Converts a sequence of strings to a string with delimiters
    /// </summary>
    let inline separated (delimiter: string) (items: string seq) =
        String.Join(delimiter, Array.ofSeq items)

    /// <summary>
    /// Removes the slashes from the end of the given string
    /// </summary>
    let inline trimSlash (s: string) = s.TrimEnd('\\')

    /// <summary>
    /// Splits the given string at the given char delimiter
    /// </summary>
    let inline split (delimiter: char) (text: string) =
        text.Split [| delimiter |] |> Array.toList

    /// <summary>
    /// Splits the given string at the given string delimiter
    /// </summary>
    let inline splitStr (delimiterStr: string) (text: string) =
        text.Split([| delimiterStr |], StringSplitOptions.None) |> Array.toList

    /// <summary>
    /// Converts a sequence of strings into a string separated with line ends
    /// </summary>
    let inline toLines text = separated Environment.NewLine text

    /// <summary>
    /// Checks whether the given text starts with the given prefix
    /// </summary>
    let startsWith (prefix: string) (text: string) = text.StartsWith prefix

    /// <summary>
    /// Checks whether the given text ends with the given suffix
    /// </summary>
    let endsWith (suffix: string) (text: string) = text.EndsWith suffix

    /// <summary>
    /// Determines whether the last character of the given <see cref="string" />
    /// matches <c>Path.DirectorySeparatorChar</c>.
    /// </summary>
    let endsWithSlash = endsWith (Path.DirectorySeparatorChar.ToString())

    /// <summary>
    /// Replaces the first occurrence of the pattern with the given replacement.
    /// </summary>
    let replaceFirst (pattern: string) replacement (text: string) =
        let pos = text.IndexOf pattern

        if pos < 0 then
            text
        else
            text.Remove(pos, pattern.Length).Insert(pos, replacement)

    let private regexes = Dictionary<_, _>()

    /// [omit]
    let getRegEx pattern =
        match regexes.TryGetValue pattern with
        | true, regex -> regex
        | _ -> System.Text.RegularExpressions.Regex(pattern)

    /// [omit]
    let regex_replace pattern (replacement: string) text =
        (getRegEx pattern).Replace(text, replacement)

    /// <summary>
    /// Checks whether the given char is a german umlaut.
    /// </summary>
    let isUmlaut c =
        Seq.exists ((=) c) [ 'ä'; 'ö'; 'ü'; 'Ä'; 'Ö'; 'Ü'; 'ß' ]

    /// <summary>
    /// Converts all characters in a string to lower case.
    /// </summary>
    let inline toLower (s: string) = s.ToLower()

    /// <summary>
    /// Returns all standard chars and digits.
    /// </summary>
    let charsAndDigits = [ 'a' .. 'z' ] @ [ 'A' .. 'Z' ] @ [ '0' .. '9' ]

    /// <summary>
    /// Checks whether the given char is a standard char or digit.
    /// </summary>
    let isLetterOrDigit c = List.exists ((=) c) charsAndDigits

    /// <summary>
    /// Trims the given string with the DirectorySeparatorChar
    /// </summary>
    let inline trimSeparator (s: string) = s.TrimEnd Path.DirectorySeparatorChar

    /// <summary>
    /// Trims all special characters from a string.
    /// </summary>
    let inline trimSpecialChars (text: string) =
        text
        |> Seq.filter isLetterOrDigit
        |> Seq.filter (isUmlaut >> not)
        |> Seq.fold (fun (acc: string) c -> acc + string c) ""

    /// <summary>
    /// Trims the given string
    /// </summary>
    let inline trim (x: string) = if isNullOrEmpty x then x else x.Trim()

    /// <summary>
    /// Trims the given string
    /// </summary>
    let inline trimChars (chars: char[]) (x: string) =
        if isNullOrEmpty x then x else x.Trim chars

    /// <summary>
    /// Trims the start of the given string
    /// </summary>
    let inline trimStartChars (chars: char[]) (x: string) =
        if isNullOrEmpty x then x else x.TrimStart chars

    /// <summary>
    /// Trims the end of the given string
    /// </summary>
    let inline trimEndChars (chars: char[]) (x: string) =
        if isNullOrEmpty x then x else x.TrimEnd chars

    /// <summary>
    /// Lifts a string to an option
    /// </summary>
    let liftString x =
        if isNullOrEmpty x then None else Some x


    /// <summary>
    /// Removes all trailing .0 from a version string
    /// </summary>
    let rec NormalizeVersion (version: string) =
        if isNull version then
            ""
        else
            let elements = version.Split [| '.' |]
            let mutable version = ""

            for i in 0..3 do
                if i < elements.Length then
                    if version = "" then
                        version <- elements[i]
                    else
                        version <- version + "." + elements[i]

            if version.EndsWith ".0" then
                version.Remove(version.Length - 2, 2) |> NormalizeVersion
            else
                version

    /// <summary>
    /// The colon character
    /// </summary>
    let Colon = ','

    /// <summary>
    /// Represents Linux line breaks
    /// </summary>
    let LinuxLineBreaks = "\n"

    /// <summary>
    /// Represents Windows line breaks
    /// </summary>
    let WindowsLineBreaks = "\r\n"

    /// <summary>
    /// Represents Mac line breaks
    /// </summary>
    let MacLineBreaks = "\r"

    /// <summary>
    /// Converts all line breaks in a text to windows line breaks
    /// </summary>
    let convertTextToWindowsLineBreaks text =
        text
        |> replace WindowsLineBreaks LinuxLineBreaks
        |> replace MacLineBreaks LinuxLineBreaks
        |> replace LinuxLineBreaks WindowsLineBreaks

    /// <summary>
    /// Reads a file line by line and replaces all line breaks to windows line breaks
    /// uses a temp file to store the contents in order to prevent OutOfMemory exceptions
    /// </summary>
    let convertFileToWindowsLineBreaksWithEncoding (encoding: Encoding) (fileName: string) =
        let tempFileName = Path.GetTempFileName()

        (use file = File.OpenRead fileName
         use reader = new StreamReader(file, encoding)

         (use tempFile = File.Open(tempFileName, FileMode.Create)
          use writer = new StreamWriter(tempFile, encoding)

          while not reader.EndOfStream do
              reader.ReadLine() |> convertTextToWindowsLineBreaks |> writer.WriteLine))

        File.Delete(fileName)
        File.Move(tempFileName, fileName)

    let convertFileToWindowsLineBreak (_: Encoding) (fileName: string) =
        convertFileToWindowsLineBreaksWithEncoding Encoding.UTF8 fileName

    /// <summary>
    /// Removes linebreaks from the given string
    /// </summary>
    let inline removeLineBreaks text =
        text |> replace "\r" String.Empty |> replace "\n" String.Empty

    /// <summary>
    /// Encapsulates the Apostrophe
    /// </summary>
    let inline encapsulateApostrophe text = replace "'" "`" text

    /// <summary>
    /// Decodes a Base64-encoded UTF-8-encoded string
    /// </summary>
    let decodeBase64Utf8String (text: string) =
        text |> Convert.FromBase64String |> Encoding.UTF8.GetString

    /// <summary>
    /// FAKE string module operators
    /// </summary>
    module Operators =
        /// <summary>
        /// Checks whether the given text starts with the given prefix
        /// </summary>
        let inline (<*) prefix text = startsWith prefix text

        /// <summary>
        /// Find a regex pattern in a text and replaces it with the given replacement.
        /// </summary>
        let (>=>) pattern replacement text = regex_replace pattern replacement text

        /// <summary>
        /// Determines if a text matches a given regex pattern.
        /// </summary>
        let (>**) pattern (text: string) = (getRegEx pattern).IsMatch text
