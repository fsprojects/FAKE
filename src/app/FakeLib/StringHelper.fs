[<AutoOpen>]
module Fake.StringHelper

open System
open System.IO
open System.Text

let productName() = "FAKE"

/// Reads a file line by line
let ReadFile (file:string) =   
  seq {use textReader = new StreamReader(file, Encoding.Default)
       while not textReader.EndOfStream do
         yield textReader.ReadLine()}      

/// Writes a file line by line
let WriteToFile append file (lines: seq<string>) =    
  let fi = new FileInfo(file)
  use writer = if append && fi.Exists then fi.AppendText() else fi.CreateText() 
  lines |> Seq.iter (writer.WriteLine)

/// Writes a file line by line
let WriteFile file lines = WriteToFile false file lines
  
/// Appends all lines to a file line by line
let AppendToFile file lines = WriteToFile true file lines

/// Converts a sequence of strings to a string with delimiters
let separated delimiter =
  Seq.fold (fun acc t -> if acc <> "" then acc + delimiter + t else t) ""
       
/// Reads a file as one text
let ReadFileAsString file = File.ReadAllText file 

/// Removes linebreaks from the given string
let RemoveLineBreaks (s:string) = s.Replace("\r",String.Empty).Replace("\n",String.Empty)  

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

/// Appends a text if the value is not null´or empty
let appendStringIfValueIsNotNullOrEmpty value = appendIfTrue (String.IsNullOrEmpty value |> not)

/// Appends all notnull fileNames
let appendFileNamesIfNotNull fileNames (builder:StringBuilder) =
  fileNames 
    |> Seq.fold (fun builder file -> appendIfTrue (String.IsNullOrEmpty file |> not) file builder) builder

/// Replaces the absolute path to a relative
let toRelativePath (value:string) =
  value.Replace((new DirectoryInfo(".")).FullName,".")

/// Removes the slashes from the end of the given string
let trimSlash (s:string) = s.TrimEnd('\\')