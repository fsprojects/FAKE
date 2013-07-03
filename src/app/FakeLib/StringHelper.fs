[<AutoOpen>]
module Fake.StringHelper

open System
open System.IO
open System.Text
open System.Collections.Generic

let productName() = "FAKE"

/// <summary>Returns if the string is null or empty</summary>
/// <user/>
let isNullOrEmpty = String.IsNullOrEmpty

/// <summary>Returns if the string is not null or empty</summary>
/// <user/>
let isNotNullOrEmpty = String.IsNullOrEmpty >> not

/// <summary>Reads a file line by line</summary>
/// <user/>
let ReadFile (file:string) =   
    seq {use textReader = new StreamReader(file, Encoding.Default)
         while not textReader.EndOfStream do
             yield textReader.ReadLine()}

/// <summary>Writes a file line by line</summary>
/// <user/>
let WriteToFile append fileName (lines: seq<string>) =    
    let fi = fileInfo fileName

    use writer = new StreamWriter(fileName,append && fi.Exists,Encoding.Default) 
    lines |> Seq.iter writer.WriteLine

/// <summary>Removes all trailing .0 from a version string</summary>
/// <user/>
let rec NormalizeVersion(version:string) =
    let elements = version.Split [|'.'|]
    let mutable version = ""
    for i in 0..3 do
        if i < elements.Length then
            if version = "" then version <- elements.[i] else version <- version + "." + elements.[i]

    if version.EndsWith ".0" then 
        version.Remove(version.Length-2,2) |> NormalizeVersion 
    else 
        version

/// <summary>Writes a byte array to a file</summary>
/// <user/>
let WriteBytesToFile file bytes = File.WriteAllBytes(file,bytes)

/// <summary>Writes string to a file</summary>
/// <user/>
let WriteStringToFile append file text = WriteToFile append file [text]

/// <summary>Replaces the file with the given string</summary>
/// <user/>
let ReplaceFile fileName text =
    let fi = fileInfo fileName
    if fi.Exists then
        fi.IsReadOnly <- false
        fi.Delete()
    WriteStringToFile false fileName text

let Colon = ','

/// <summary>Writes a file line by line</summary>
/// <user/>
let WriteFile file lines = WriteToFile false file lines
  
/// <summary>Appends all lines to a file line by line</summary>
/// <user/>
let AppendToFile file lines = WriteToFile true file lines

/// Replaces the given pattern in the given text with the replacement
let inline replace (pattern:string) replacement (text:string) = text.Replace(pattern,replacement)

/// Converts a sequence of strings to a string with delimiters
let inline separated delimiter (items: string seq) = String.Join(delimiter, Array.ofSeq items)

/// <summary>Reads a file as one text</summary>
/// <user/>
let inline ReadFileAsString file = File.ReadAllText(file,Encoding.Default)

/// <summary>Reads a file as one array of bytes</summary>
/// <user/>
let ReadFileAsBytes = File.ReadAllBytes

/// Replaces any occurence of the currentDirectory with .
let inline shortenCurrentDirectory value = replace currentDirectory "." value

/// Removes the slashes from the end of the given string
let inline trimSlash (s:string) = s.TrimEnd('\\')

/// Splits the given string at the given delimiter
let inline split (delimiter:char) (text:string) = text.Split [|delimiter|] |> Array.toList

/// Converts a sequence of strings into a string separated with line ends
let inline toLines s = separated "\r\n" s

/// Checks wether the given text starts with the given prefix
let startsWith prefix (text:string) = text.StartsWith prefix

/// Checks wether the given text starts with the given prefix
let inline (<*) prefix text = startsWith prefix text

/// Checks wether the given text ends with the given suffix
let endsWith suffix (text:string) = text.EndsWith suffix

/// Determines whether the last character of the given <see cref="string" />
/// matches Path.DirectorySeparatorChar.         
let endsWithSlash = endsWith (Path.DirectorySeparatorChar.ToString())

/// Replaces the text in the given file
let ReplaceInFile replaceF fileName =
    fileName
    |> ReadFileAsString
    |> replaceF
    |> ReplaceFile fileName

let LinuxLineBreaks = "\n"
let WindowsLineBreaks = "\r\n"
let MacLineBreaks = "\r"

let ConvertTextToWindowsLineBreaks text = 
    text
    |> replace WindowsLineBreaks LinuxLineBreaks 
    |> replace MacLineBreaks LinuxLineBreaks 
    |> replace LinuxLineBreaks WindowsLineBreaks

/// Reads a file line by line and replaces all line breaks to windows line breaks
///   - uses a temp file to store the contents in order to prevent OutOfMemory exceptions
let ConvertFileToWindowsLineBreaks (fileName:string) = 
    use reader = new StreamReader(fileName, Encoding.Default)

    let tempFileName = Path.GetTempFileName()

    use writer = new StreamWriter(tempFileName,false,Encoding.Default) 
    
    while not reader.EndOfStream do
        reader.ReadLine()
        |> ConvertTextToWindowsLineBreaks
        |> writer.WriteLine

    reader.Close()
    writer.Close()

    File.Delete(fileName)
    File.Move(tempFileName,fileName)

let replaceFirst (pattern: string) replacement (text: string) = 
    let pos = text.IndexOf pattern
    if pos < 0
        then text
        else text.Remove(pos, pattern.Length).Insert(pos, replacement)

/// Removes linebreaks from the given string
let inline RemoveLineBreaks text = 
    text
      |> replace "\r" String.Empty
      |> replace "\n" String.Empty

/// Encapsulates the Apostrophe
let inline EncapsulateApostrophe text = replace "'" "`" text

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

/// The directory separator string. On most systems / or \
let directorySeparator = Path.DirectorySeparatorChar.ToString()

/// A cache of relative path names.
let relativePaths = new Dictionary<_,_>()

/// <summary>Produces relative path when possible to go from baseLocation to targetLocation.</summary>
/// <param name="baseLocation">The root folder</param>
/// <param name="targetLocation">The target folder</param>
/// <returns>The relative path relative to baseLocation</returns>
/// <exception cref="ArgumentNullException">base or target locations are null or empty</exception>
let ProduceRelativePath baseLocation targetLocation =
    if isNullOrEmpty baseLocation then
        raise (new ArgumentNullException "baseLocation")
    
    if isNullOrEmpty targetLocation then
        raise (new ArgumentNullException "targetLocation")

    if not <| Path.IsPathRooted baseLocation then baseLocation else
    if not <| Path.IsPathRooted targetLocation then targetLocation else
    if String.Compare(Path.GetPathRoot baseLocation, Path.GetPathRoot targetLocation, true) <> 0 then targetLocation else
    if String.Compare(baseLocation, targetLocation, true) = 0 then "." else
    let resultPath = ref "."

    let targetLocation =
        if targetLocation |> endsWith directorySeparator then targetLocation
        else targetLocation + directorySeparator
    
    let baseLocation =
        if baseLocation |> endsWith directorySeparator then
            ref (baseLocation.Substring(0, baseLocation.Length - 1))
        else 
            ref baseLocation

    while not <| targetLocation.StartsWith(!baseLocation + directorySeparator, StringComparison.OrdinalIgnoreCase) do
        resultPath := !resultPath + directorySeparator + ".."
        baseLocation := Path.GetDirectoryName !baseLocation

        if (!baseLocation) |> endsWith directorySeparator then
            baseLocation := (!baseLocation).Substring(0, (!baseLocation).Length - 1)

    resultPath := 
        (!resultPath + targetLocation.Substring((!baseLocation).Length))
          |> replace (directorySeparator + directorySeparator) directorySeparator

    // preprocess .\..\ case
    if (sprintf ".%s..%s" directorySeparator directorySeparator) <* (!resultPath) then
        (!resultPath).Substring(2, (!resultPath).Length - 3)
    else
        (!resultPath).Substring(0, (!resultPath).Length - 1)

/// Replaces the absolute path to a relative
let inline toRelativePath value = 
    match relativePaths.TryGetValue value with
    | true,x -> x
    | _ ->
         let x = ProduceRelativePath currentDirectory value
         relativePaths.Add(value,x)
         x

let private regexes = new Dictionary<_,_>()

let getRegEx pattern =
    match regexes.TryGetValue pattern with
    | true, regex -> regex
    | _ -> (new System.Text.RegularExpressions.Regex(pattern))

let regex_replace pattern (replacement:string) text =
    (getRegEx pattern).Replace(text,replacement)

let (>=>) pattern replacement text = regex_replace pattern replacement text

let (>**) pattern text = (getRegEx pattern).IsMatch text

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

/// Decodes a Base64-encoded UTF-8-encoded string
let DecodeBase64Utf8String (text:string) = 
  text
  |> Convert.FromBase64String
  |> Encoding.UTF8.GetString
