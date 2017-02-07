/// Contains helpers which allow to parse Change log text files.
/// These files have to be in a format as described on http://keepachangelog.com/en/0.3.0/
///
/// ## Sample
///
///     let changeLog =
///         ReadFile "RELEASE_NOTES.md"
///         |> ChangeLogHelper.parseChangeLog
///
///
///     Target "AssemblyInfo" (fun _ ->
///         CreateFSharpAssemblyInfo "src/Common/AssemblyInfo.fs"
///           [ Attribute.Title project
///             Attribute.Product project
///             Attribute.Description summary
///             Attribute.Version changeLog.AssemblyVersion
///             Attribute.FileVersion changeLog.AssemblyVersion]
///     )
module Fake.ChangeLogHelper

open System
open System.Text.RegularExpressions
open Fake.AssemblyInfoFile

type ChangeLog =
    { /// the parsed Version
      AssemblyVersion: string
      /// the NuGet package version
      NuGetVersion: string
      /// Semantic version
      SemVer: SemVerHelper.SemVerInfo
      /// Release DateTime
      Date: DateTime option
      /// The parsed list of changes
      Changes: string list }

    override x.ToString() = sprintf "%A" x

    static member New(assemblyVersion, nugetVersion, date, changes) = {
        AssemblyVersion = assemblyVersion
        NuGetVersion = nugetVersion
        SemVer = SemVerHelper.parse nugetVersion
        Date = date
        Changes = changes }
    
    static member New(assemblyVersion, nugetVersion, changes) = ChangeLog.New(assemblyVersion, nugetVersion, None, changes)

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

let parseChanges (text: seq<string>) =
    let text = text |> Seq.toList |> List.filter (not << isNullOrWhiteSpace)
    match text with
    | [] -> failwith "Empty change log file."
    | _ :: __ -> 
        let isBlockHeader line = "## " <* line && not <| line.Contains("[Unreleased]")
        let isCategoryHeader line = "### " <* line
        let isAnyHeader line = isBlockHeader line || isCategoryHeader line
        let trimLine = trimStartChars [|' '; '*'; '#'; '-'|] >> trimEndChars [|' '|]
        let trimLines lines = lines |> Seq.map trimLine |> Seq.toList

        let rec findNextChangesBlock text = 
            let rec findEnd changes text =
                match text with
                | [] -> changes,[]
                | h :: rest when h |> isBlockHeader -> changes,text 
                | h :: rest -> findEnd (h :: changes) rest

            match text with
            | [] -> None
            | h :: rest -> if isBlockHeader h then Some(h, findEnd [] rest) else findNextChangesBlock rest

        let rec findNextCategoryBlock text = 
            let rec findEnd changes text =
                match text with
                | [] -> changes |> List.filter isNotNullOrEmpty,[]
                | h :: rest when h |> isAnyHeader -> changes |> List.filter isNotNullOrEmpty,text
                | h :: rest -> findEnd (h :: changes) rest

            match text with
            | [] -> None
            | h :: rest when h |> isCategoryHeader -> Some(h, findEnd [] rest) 
            | h :: rest -> findNextCategoryBlock rest

        let rec categoryLoop (changes: string list) (text: string list) : string list =
            match findNextCategoryBlock text with
            | Some (header, (changeLines, rest)) ->
                categoryLoop (changes |> List.append (changeLines |> List.map trimLine |> List.filter isNotNullOrEmpty |> List.rev |> List.map (sprintf "%s: %s" (header |> trimLine)))) rest
            | None -> changes

        let rec loop changeLog text =
            match findNextChangesBlock text with
            | Some (header, (changes, rest)) ->
                let assemblyVer, nugetVer = parseVersions header
                let date = parseDate header
                let changeLines = categoryLoop [] (changes |> List.filter isNotNullOrEmpty |> List.rev)
                let newChangeLog = ChangeLog.New(assemblyVer.Value, nugetVer.Value, date, changeLines)
                loop (newChangeLog::changeLog) rest
            | None -> changeLog

        (loop [] text |> List.sortBy (fun x -> x.SemVer) |> List.rev)

/// Parses a change log text and returns the lastest change log.
///
/// ## Parameters
///  - `data` - change log text
let parseChangeLog (data: seq<string>) =
    data
    |> parseChanges
    |> Seq.head

/// Parses a Change log text file and returns the lastest change log.
///
/// ## Parameters
///  - `fileName` - ChangeLog text file name
let LoadChangeLog fileName =
    System.IO.File.ReadLines fileName
    |> parseChangeLog

let changes = System.IO.File.ReadLines  "C:/temp/Changelog.txt"

parseChanges changes
