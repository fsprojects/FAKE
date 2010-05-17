[<AutoOpen>]
module Fake.StringHelper

open System
open System.IO
open System.Text

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

    use writer =  new StreamWriter(fileName,append && fi.Exists,Encoding.Default) 
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
let inline appendStringIfValueIsNotNullOrEmpty value = appendIfTrue (String.IsNullOrEmpty value |> not)

/// Appends all notnull fileNames
let inline appendFileNamesIfNotNull fileNames (builder:StringBuilder) =
  fileNames 
    |> Seq.fold (fun builder file -> appendIfTrue (String.IsNullOrEmpty file |> not) file builder) builder

/// Replaces the absolute path to a relative
let inline toRelativePath value = replace currentDirectory "." value

/// Removes the slashes from the end of the given string
let inline trimSlash (s:string) = s.TrimEnd('\\')

/// Converts a sequence of strings into a string separated with line ends
let inline toLines s = separated "\r\n" s

/// Checks wether the given text starts with the given prefix
let inline (<*) prefix (text:string) = text.StartsWith prefix

let isUmlaut c = List.exists ((=) c) ['ä'; 'ö'; 'ü'; 'Ä'; 'Ö'; 'Ü'; 'ß']
let charsAndDigits = ['a'..'z'] @ ['A'..'Z'] @ ['0'..'9'] 
let isLetterOrDigit c = List.exists ((=) c) charsAndDigits

let inline trimSeparator (s:string) = s.Trim Path.DirectorySeparatorChar

let inline trimSpecialChars (s:string) =
    s
      |> Seq.filter isLetterOrDigit
      |> Seq.filter (isUmlaut >> not)
      |> Seq.fold (fun (acc:string) c -> acc + string c) ""