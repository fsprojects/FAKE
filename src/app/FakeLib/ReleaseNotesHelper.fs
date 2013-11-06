/// Contains helpers which allow to parse Release Notes text files. Either "simple" or "complex" format is accepted.
///
/// ### Simple format
///
///     * 1.1.10 - Support for heterogeneous XML attributes. Make CsvFile re-entrant.
///     * 1.1.9 - Infer booleans for ints that only manifest 0 and 1.
///
/// ### Complex format
///
///     ### New in 1.1.10 (Released 2013/09/12)
///     * Support for heterogeneous XML attributes.
///     * Make CsvFile re-entrant. 
///     * Support for compressed HTTP responses. 
///     * Fix JSON conversion of 0 and 1 to booleans.
///
///     ### New in 1.1.9 (Released 2013/07/21)
///     * Infer booleans for ints that only manifest 0 and 1.    
///     * Support for partially overriding the Schema in CsvProvider.
///     * PreferOptionals and SafeMode parameters for CsvProvider.
///
/// ## Sample
///
///     let release =
///         ReadFile "RELEASE_NOTES.md"
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
module Fake.ReleaseNotesHelper

open System
open System.Text.RegularExpressions

/// Contains the parsed information of the release notes text file.
type ReleaseNotes =
    { /// The parsed version.
      AssemblyVersion: string
      /// The nuget package version.
      NugetVersion: string
      /// Semantic version
      SemVer: SemVerHelper.SemVerInfo
      // The parsed release notes.
      Notes: string list }
    override x.ToString() = sprintf "%A" x

    static member New(assemblyVersion,nugetVersion,notes) = { 
        AssemblyVersion = assemblyVersion
        NugetVersion = nugetVersion
        SemVer = SemVerHelper.parse nugetVersion
        Notes = notes }

let private nugetRegex = getRegEx @"([0-9]+.)+[0-9]+(-[a-zA-Z]+)?(.[0-9]+)?"
let private assemblyRegex = getRegEx @"([0-9]+.)+[0-9]+"

/// Parse simple release notes sequence
let private parseSimpleReleaseNotes line =
    let assemblyVersion, nugetVersion = assemblyRegex.Match (line), nugetRegex.Match (line)
    if not assemblyVersion.Success
    then failwith "Unable to parse valid Assembly version from release notes."
    let trimDot (s:string) = s.TrimEnd('.')
    let notes = 
        line.Substring (nugetVersion.Index + nugetVersion.Length)
        |> trimChars [|' '; '-'|]
        |> splitStr ". "
        |> List.map (trimDot >> trim)
        |> List.filter isNotNullOrEmpty
        |> List.map (fun x -> x + ".")
    ReleaseNotes.New(assemblyVersion.Value,nugetVersion.Value,notes)

/// Parse "complex" release notes text sequence
let private parseAllComplexReleaseNotes (text: seq<string>) =
    let rec findNextNotesBlock text =
        let isHeader line = "##" <* line
        let rec findEnd notes text =
            match text with
            | [] -> notes,[]
            | h :: rest -> if isHeader h then notes,text else findEnd (h :: notes) rest

        match text with
        | [] -> None
        | h :: rest -> if isHeader h then Some(h,findEnd [] rest) else findNextNotesBlock rest

    let rec loop releaseNotes text =
        match findNextNotesBlock text with
        | Some(header,(notes, rest)) ->        
            let assemblyVer, nugetVer = assemblyRegex.Match header, nugetRegex.Match header
            if not assemblyVer.Success then failwith "Unable to parse valid Assembly version from release notes."
            let newReleaseNotes = ReleaseNotes.New(assemblyVer.Value,nugetVer.Value,notes |> List.filter isNotNullOrEmpty |> List.rev)
            loop (newReleaseNotes::releaseNotes) rest
        | None -> releaseNotes

    loop [] (text |> Seq.map (trimChars [|' '; '*'|]) |> Seq.toList)


/// Parses a Release Notes text and returns all release notes.
///
/// ## Parameters
///  - `data` - Release notes text
let parseAllReleaseNotes (data: seq<string>) = 
    let data = data |> Seq.toList |> List.filter (not << isNullOrWhiteSpace)
    match data with
    | [] -> failwith "Empty Release file."
    | h :: _ ->
        let (|Simple|Complex|Invalid|) = function '*' -> Simple | '#' -> Complex | _ -> Invalid
        let firstNonEmptyChar = h.Trim([|'-'; ' '|]).[0]
        match firstNonEmptyChar with
        | Simple -> 
            data 
            |> Seq.map parseSimpleReleaseNotes 
            |> Seq.toList
        | Complex -> parseAllComplexReleaseNotes data
        | Invalid -> failwith "Invalid Release Notes format."
        |> List.sortBy (fun x -> x.SemVer)
        |> List.rev

    
/// Parses a Release Notes text and returns the lastest release notes.
///
/// ## Parameters
///  - `data` - Release notes text
let parseReleaseNotes (data: seq<string>) =
    data
    |> parseAllReleaseNotes
    |> Seq.head