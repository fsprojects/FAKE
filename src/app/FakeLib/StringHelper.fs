[<AutoOpen>]
module Fake.StringHelper

open System
open System.IO
open System.Text
open System.Collections.Generic

let productName() = "FAKE"

/// <summary>Reads a file line by line</summary>
/// <user/>
let ReadFile (file:string) =   
    seq {use textReader = new StreamReader(file, encoding)
         while not textReader.EndOfStream do
             yield textReader.ReadLine()}

/// <summary>Writes a file line by line</summary>
/// <user/>
let WriteToFile append fileName (lines: seq<string>) =    
    let fi = fileInfo fileName

    use writer = new StreamWriter(fileName,append && fi.Exists,encoding) 
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

/// <summary>Writes a string to a file</summary>
/// <user/>
let WriteStringToFile append fileName (text:string) =
    let fi = fileInfo fileName

    use writer = new StreamWriter(fileName,append && fi.Exists,encoding) 
    writer.Write text

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

/// <summary>Reads a file as one text</summary>
/// <user/>
let inline ReadFileAsString file = File.ReadAllText(file,encoding)

/// <summary>Reads a file as one array of bytes</summary>
/// <user/>
let ReadFileAsBytes = File.ReadAllBytes

/// Replaces any occurence of the currentDirectory with .
let inline shortenCurrentDirectory value = replace currentDirectory "." value

/// Checks wether the given text starts with the given prefix
let inline (<*) prefix text = startsWith prefix text

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
    use reader = new StreamReader(fileName, encoding)

    let tempFileName = Path.GetTempFileName()

    use writer = new StreamWriter(tempFileName,false,encoding) 
    
    while not reader.EndOfStream do
        reader.ReadLine()
        |> ConvertTextToWindowsLineBreaks
        |> writer.WriteLine

    reader.Close()
    writer.Close()

    File.Delete(fileName)
    File.Move(tempFileName,fileName)

/// Removes linebreaks from the given string
let inline RemoveLineBreaks text = 
    text
      |> replace "\r" String.Empty
      |> replace "\n" String.Empty

/// Encapsulates the Apostrophe
let inline EncapsulateApostrophe text = replace "'" "`" text
        
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

let (>=>) pattern replacement text = regex_replace pattern replacement text

let (>**) pattern text = (getRegEx pattern).IsMatch text

/// Decodes a Base64-encoded UTF-8-encoded string
let DecodeBase64Utf8String (text:string) = 
  text
  |> Convert.FromBase64String
  |> Encoding.UTF8.GetString
    