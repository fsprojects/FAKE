/// Contains helpers which allow tp parse Release Notes text files.
module Fake.ReleaseNotesHelper

open System
open System.Text.RegularExpressions

/// Contains the parsed information of the release notes text file.
type ReleaseNotes =
    { /// The parsed version.
      AssemblyVersion: string
      /// The nuget package version.
      NugetVersion: string
      // The parsed release notes.
      Notes: string list }
    override x.ToString() = sprintf "%A" x

let private nugetRegex = getRegEx @"([0-9]+.)+[0-9]+(-[a-zA-Z]+)?"
let private assemblyRegex = getRegEx @"([0-9]+.)+[0-9]+"

/// Parse simple release notes sequence
let private parseSimpleReleaseNotes (text: seq<string>) = 
    let lastLine = text |> Seq.last
    let assemblyVersion, nugetVersion = assemblyRegex.Match (lastLine), nugetRegex.Match (lastLine)
    if not assemblyVersion.Success
    then failwith "Unable to parse valid Assembly version from release notes."
    let notes = 
        lastLine.Substring (nugetVersion.Index + nugetVersion.Length)
        |> trimChars [|' '; '-'|]
        |> split '.'
        |> List.map trim
        |> List.filter isNotNullOrEmpty
        |> List.map (fun x -> x + ".")
    { AssemblyVersion = assemblyVersion.Value; NugetVersion = nugetVersion.Value; Notes = notes }

/// Parse "complex" release notes text sequence
let private parseComplexReleaseNotes (text: seq<string>) =
    let rec loop notes = function
        | [] -> failwithf "No header in %A" text
        | h :: t -> 
            if "#" <* h then h, notes
            else loop (h :: notes) t

    let header, notes = loop [] (text |> Seq.map (trimChars [|' '; '*'|]) |> List.ofSeq |> List.rev)
    let assemblyVer, nugetVer = assemblyRegex.Match header, nugetRegex.Match header
    if not assemblyVer.Success then failwith "Unable to parse valid Assembly version from release notes."
    { AssemblyVersion = assemblyVer.Value; NugetVersion = nugetVer.Value; Notes = notes }
    
/// Parse a Release Notes text - Either simple or "complex" format
/// See: [http://github.com/fsharp/FAKE/issues/171](http://github.com/fsharp/FAKE/issues/171)
/// ## Parameters
///  - `data` - Release notes text
let parseReleaseNotes (data: seq<string>) = 
    let data = data |> Seq.toList |> List.filter (not << isNullOrWhiteSpace)
    match data with
    | [] -> failwith "Empty Realease file."
    | h :: _ -> 
        let (|Simple|Complex|Invalid|) = function '*' -> Simple | '#' -> Complex | _ -> Invalid
        let firstNonEmptyChar = h.Trim([|'-'; ' '|]).[0]
        match firstNonEmptyChar with
        | Simple -> parseSimpleReleaseNotes data
        | Complex -> parseComplexReleaseNotes data
        | Invalid -> failwith "Invalid Release Notes format."