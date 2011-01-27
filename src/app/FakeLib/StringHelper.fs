[<AutoOpen>]
module Fake.StringHelper

open System
open System.IO
open System.Text
open System.Collections.Generic

let productName() = "FAKE"

/// Returns if the string is null or empty
let isNullOrEmpty = String.IsNullOrEmpty

/// Reads a file line by line
let ReadFile (file:string) =   
    seq {use textReader = new StreamReader(file, Encoding.Default)
         while not textReader.EndOfStream do
             yield textReader.ReadLine()}

/// Writes a file line by line
let WriteToFile append fileName (lines: seq<string>) =    
    let fi = fileInfo fileName

    use writer = new StreamWriter(fileName,append && fi.Exists,Encoding.Default) 
    lines |> Seq.iter writer.WriteLine

/// Writes string to a file
let WriteStringToFile append file text = WriteToFile append file [text]

/// Replaces the file with the given string
let ReplaceFile fileName text =
    let fi = fileInfo fileName
    if fi.Exists then
        fi.IsReadOnly <- false
        fi.Delete()
    WriteStringToFile false fileName text

let Colon = ','

/// Writes a file line by line
let WriteFile file lines = WriteToFile false file lines
  
/// Appends all lines to a file line by line
let AppendToFile file lines = WriteToFile true file lines

/// Converts a sequence of strings to a string with delimiters
let inline separated delimiter items = String.Join(delimiter, Array.ofSeq items)
       
/// Reads a file as one text
let ReadFileAsString file = File.ReadAllText(file,Encoding.Default)

/// Replaces the given pattern in the given text with the replacement
let inline replace (pattern:string) replacement (text:string) = text.Replace(pattern,replacement)

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
let inline appendIfNotNull value s = appendIfTrue (value <> null) (sprintf "%s%A" s value)

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
        if targetLocation.EndsWith directorySeparator then targetLocation
        else targetLocation + directorySeparator
    
    let baseLocation =
        if baseLocation.EndsWith directorySeparator then
            ref (baseLocation.Substring(0, baseLocation.Length - 1))
        else 
            ref baseLocation

    while not <| targetLocation.StartsWith(!baseLocation + directorySeparator, StringComparison.OrdinalIgnoreCase) do
        resultPath := !resultPath + directorySeparator + ".."
        baseLocation := Path.GetDirectoryName !baseLocation

        if (!baseLocation).EndsWith directorySeparator then
            baseLocation := (!baseLocation).Substring(0, (!baseLocation).Length - 1)

    resultPath := (!resultPath + targetLocation.Substring((!baseLocation).Length)).Replace(directorySeparator + directorySeparator,directorySeparator)

    // preprocess .\..\ case
    if (!resultPath).StartsWith (sprintf ".%s..%s" directorySeparator directorySeparator) then
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

/// Replaces any occurence of the currentDirectory with .
let inline shortenCurrentDirectory value = replace currentDirectory "." value

/// Removes the slashes from the end of the given string
let inline trimSlash (s:string) = s.TrimEnd('\\')

/// Converts a sequence of strings into a string separated with line ends
let inline toLines s = separated "\r\n" s

/// Checks wether the given text starts with the given prefix
let inline (<*) prefix (text:string) = text.StartsWith prefix

let private regexes = new Dictionary<_,_>()

let getRegEx pattern =
    match regexes.TryGetValue pattern with
    | true, regex -> regex
    | _ -> (new System.Text.RegularExpressions.Regex(pattern))

let (>=>) pattern (replacement:string) text =
    (getRegEx pattern).Replace(text,replacement)

let (>**) pattern text = (getRegEx pattern).IsMatch text

/// Checks wether the given char is a german umlaut.
let isUmlaut c = Seq.contains c ['ä'; 'ö'; 'ü'; 'Ä'; 'Ö'; 'Ü'; 'ß']

/// Returns all standard chars and digits.
let charsAndDigits = ['a'..'z'] @ ['A'..'Z'] @ ['0'..'9'] 

/// Checks wether the given char is a standard char or digit.
let isLetterOrDigit c = List.exists ((=) c) charsAndDigits

/// Trims the given string with the DirectorySeparatorChar
let inline trimSeparator (s:string) = s.Trim Path.DirectorySeparatorChar

let inline trimSpecialChars (s:string) =
    s
      |> Seq.filter isLetterOrDigit
      |> Seq.filter (isUmlaut >> not)
      |> Seq.fold (fun (acc:string) c -> acc + string c) ""