/// Contains helpers which allow to parse Release Notes text files.
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
    let trimDot (s:string) = s.TrimEnd('.')
    let notes = 
        lastLine.Substring (nugetVersion.Index + nugetVersion.Length)
        |> trimChars [|' '; '-'|]
        |> splitStr ". "
        |> List.map (trimDot >> trim)
        |> List.filter isNotNullOrEmpty
        |> List.map (fun x -> x + ".")
    { AssemblyVersion = assemblyVersion.Value
      NugetVersion = nugetVersion.Value
      Notes = notes }

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
    { AssemblyVersion = assemblyVer.Value
      NugetVersion = nugetVer.Value
      Notes = notes }
    
/// Parse a Release Notes text - Either "simple" or "complex" format (see below).
///
/// ## Parameters
///  - `data` - Release notes text
///
/// ### Simple format
///
///     * 1.1.9 - Infer booleans for ints that only manifest 0 and 1.
///     * 1.1.10 - Support for heterogeneous XML attributes. Make CsvFile re-entrant.
///
/// ### Complex format
///
///     ### New in 1.1.9 (Released 2013/07/21)
///     * Infer booleans for ints that only manifest 0 and 1.    
///     * Support for partially overriding the Schema in CsvProvider.
///     * PreferOptionals and SafeMode parameters for CsvProvider.
///     
///     ### New in 1.1.10 (Released 2013/09/12)
///     * Support for heterogeneous XML attributes.
///     * Make CsvFile re-entrant. 
///     * Support for compressed HTTP responses. 
///     * Fix JSON conversion of 0 and 1 to booleans.
///
/// ## Sample
///
///     let release =
///         File.ReadLines "RELEASE_NOTES.md"
///         |> ReleaseNotesHelper.parseReleaseNotes
///
///
///     Target "AssemblyInfo" (fun _ ->
///         CreateFSharpAssemblyInfo "src/Common/AssemblyInfo.fs"
///           [ Attribute.Title project
///             Attribute.Product project
///             Attribute.Description summary
///             Attribute.Version release.AssemblyVersion
///             Attribute.FileVersion release.AssemblyVersion]
///     )
let parseReleaseNotes (data: seq<string>) = 
    let data = data |> Seq.toList |> List.filter (not << isNullOrWhiteSpace)
    match data with
    | [] -> failwith "Empty Release file."
    | h :: _ -> 
        let (|Simple|Complex|Invalid|) = function '*' -> Simple | '#' -> Complex | _ -> Invalid
        let firstNonEmptyChar = h.Trim([|'-'; ' '|]).[0]
        match firstNonEmptyChar with
        | Simple -> parseSimpleReleaseNotes data
        | Complex -> parseComplexReleaseNotes data
        | Invalid -> failwith "Invalid Release Notes format."
