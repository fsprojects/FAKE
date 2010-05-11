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
let WriteToFile append file (lines: seq<string>) =    
    let fi = new FileInfo(file)

    use writer =  new StreamWriter(file,append && fi.Exists,Encoding.Default) 
    lines |> Seq.iter (writer.WriteLine)

/// Writes string to a file
let WriteStringToFile append file text = WriteToFile append file [text]

/// Replaces the file with the given string
let ReplaceFile fileName text =
    let fi = new FileInfo(fileName)
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
let separated delimiter items = String.Join(delimiter, Array.ofSeq items)
       
/// Reads a file as one text
let ReadFileAsString file = File.ReadAllText(file,Encoding.Default)

/// Removes linebreaks from the given string
let RemoveLineBreaks (s:string) = 
    (new StringBuilder(s))
      .Replace("\r",String.Empty)
      .Replace("\n",String.Empty)
      .ToString()

/// Encapsulates the Apostrophe
let EncapsulateApostrophe (s:string) = s.Replace("'","`") 

/// Appends a text
let append s (builder:StringBuilder) = builder.Append(sprintf "\"%s\" " s)

/// Appends a text if the predicate is true
let appendIfTrue p s builder = if p then append s builder else builder

/// Appends a text if the predicate is false
let appendIfFalse p = appendIfTrue (not p)

/// Appends a text if the value is not null
let appendIfNotNull value s = appendIfTrue (value <> null) (sprintf "%s%A" s value)

/// Appends a text if the value is not null
let appendStringIfValueIsNotNull value = appendIfTrue (value <> null)

/// Appends a text if the value is not null or empty
let appendStringIfValueIsNotNullOrEmpty value = appendIfTrue (String.IsNullOrEmpty value |> not)

/// Appends all notnull fileNames
let appendFileNamesIfNotNull fileNames (builder:StringBuilder) =
  fileNames 
    |> Seq.fold (fun builder file -> appendIfTrue (String.IsNullOrEmpty file |> not) file builder) builder

/// Replaces the absolute path to a relative
let toRelativePath (value:string) = value.Replace(currentDirectory,".")

/// Removes the slashes from the end of the given string
let trimSlash (s:string) = s.TrimEnd('\\')

/// Converts a sequence of strings into a string separated with line ends
let toLines s = separated "\r\n" s

/// Checks wether the given text starts with the given prefix
let (<*) prefix (text:string) = text.StartsWith prefix

let isUmlaut c = List.exists ((=) c) ['ä'; 'ö'; 'ü'; 'Ä'; 'Ö'; 'Ü'; 'ß']
let charsAndDigits = ['a'..'z'] @ ['A'..'Z'] @ ['0'..'9'] 
let isLetterOrDigit c = List.exists ((=) c) charsAndDigits

let trimBackslash (s:string) = s.Trim '\\'

let trimSpecialChars (s:string) =
    s
      |> Seq.filter isLetterOrDigit
      |> Seq.filter (isUmlaut >> not)
      |> Seq.fold (fun (acc:string) c -> acc + string c) ""