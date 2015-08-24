/// Contains helpers which allow to parse Release Notes text files. Either "simple" or "complex" format is accepted.
///
/// ## Formats
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
open Fake.AssemblyInfoFile

/// Contains the parsed information of the release notes text file.
type ReleaseNotes =
    { /// The parsed version.
      AssemblyVersion: string
      /// The nuget package version.
      NugetVersion: string
      /// Semantic version
      SemVer: SemVerHelper.SemVerInfo
      /// Release date
      Date : DateTime option
      // The parsed release notes.
      Notes: string list }
    override x.ToString() = sprintf "%A" x

    static member New(assemblyVersion,nugetVersion,date,notes) = { 
        AssemblyVersion = assemblyVersion
        NugetVersion = nugetVersion
        SemVer = SemVerHelper.parse nugetVersion
        Date = date
        Notes = notes }

    static member New(assemblyVersion,nugetVersion,notes) = ReleaseNotes.New(assemblyVersion,nugetVersion,None,notes)

let parseVersions =
    let nugetRegex = getRegEx @"([0-9]+.)+[0-9]+(-[a-zA-Z]+\d*)?(.[0-9]+)?"
    fun line ->
        let assemblyVersion = assemblyVersionRegex.Match line
        if not assemblyVersion.Success
        then failwithf "Unable to parse valid Assembly version from release notes (%s)." line

        let nugetVersion = nugetRegex.Match line
        if not nugetVersion.Success
        then failwithf "Unable to parse valid NuGet version from release notes (%s)." line
        assemblyVersion, nugetVersion

let parseDate =
    let dateRegex = getRegEx @"(19|20)\d\d([- /.])(0[1-9]|1[012]|[1-9])\2(0[1-9]|[12][0-9]|3[01]|[1-9])"
    fun line ->
        let possibleDate = dateRegex.Match line
        match possibleDate.Success with
        | false -> None
        | true ->
            match DateTime.TryParse possibleDate.Value with
            | false, _ -> None
            | true, x -> Some(x)

/// Parse simple release notes sequence
let private parseSimpleReleaseNotes line =
    let assemblyVersion, nugetVersion = parseVersions line
    let trimDot (s:string) = s.TrimEnd('.')

    let notes = 
        line.Substring (nugetVersion.Index + nugetVersion.Length)
        |> trimChars [|' '; '-'|]
        |> splitStr ". "
        |> List.map (trimDot >> trim)
        |> List.filter isNotNullOrEmpty
        |> List.map (fun x -> x + ".")
    ReleaseNotes.New(assemblyVersion.Value, nugetVersion.Value, None, notes)

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
            let assemblyVer, nugetVer = parseVersions header
            let date = parseDate header
            let newReleaseNotes = ReleaseNotes.New(assemblyVer.Value,nugetVer.Value,date,notes |> List.filter isNotNullOrEmpty |> List.rev)
            loop (newReleaseNotes::releaseNotes) rest
        | None -> releaseNotes

    loop [] (text |> Seq.map (trimStartChars [|' '; '*'|] >> trimEndChars [|' '|]) |> Seq.toList)


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

/// Parses a Release Notes text file and returns the lastest release notes.
///
/// ## Parameters
///  - `fileName` - Release notes text file name
let LoadReleaseNotes fileName =
    System.IO.File.ReadLines fileName
    |> parseReleaseNotes