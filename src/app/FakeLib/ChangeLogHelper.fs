/// Contains helpers which allow to parse Change log text files.
/// These files have to be in a format as described on http://keepachangelog.com/en/0.3.0/
///
/// ## Sample
///
///     let changeLog = LoadChangeLog "CHANGELOG.md"
///
///     Target "AssemblyInfo" (fun _ ->
///         CreateFSharpAssemblyInfo "src/Common/AssemblyInfo.fs"
///           [ Attribute.Title project
///             Attribute.Product project
///             Attribute.Description summary
///             Attribute.Version changeLog.LatestEntry.AssemblyVersion
///             Attribute.FileVersion changeLog.LatestEntry.AssemblyVersion]
///     )
module Fake.ChangeLogHelper

open System
open System.Text.RegularExpressions
open Fake.AssemblyInfoFile

let private trimLine = trimStartChars [|' '; '*'; '#'; '-'|] >> trimEndChars [|' '|]
let private trimLines lines = lines |> Seq.map trimLine |> Seq.toList

type Change =
    /// for new features
    | Added of string
    /// for changes in existing functionality
    | Changed of string
    /// for once-stable features removed in upcoming releases
    | Deprecated of string
    /// for deprecated features removed in this release
    | Removed of string
    /// for any bug fixes
    | Fixed of string
    /// to invite users to upgrade in case of vulnerabilities
    | Security of string
    /// Custom entry (Header, Description)
    | Custom of string * string

    override x.ToString() = 
        match x with
        | Added s -> sprintf "Added: %s" s
        | Changed s -> sprintf "Changed: %s" s
        | Deprecated s -> sprintf "Deprecated: %s" s
        | Removed s -> sprintf "Removed: %s" s
        | Fixed s -> sprintf "Fixed: %s" s
        | Security s -> sprintf "Security: %s" s
        | Custom (h, s) -> sprintf "%s: %s" h s

    static member New(header: string, line: string): Change = 
        let line = line |> trimLine

        match header |> trimLine |> toLower with
        | "added" -> Added line
        | "changed" -> Changed line
        | "deprecated" -> Deprecated line
        | "removed" -> Removed line
        | "fixed" -> Fixed line
        | "security" -> Security line
        | _ -> Custom (header |> trimLine, line)


type ChangeLogEntry =
    { /// the parsed Version
      AssemblyVersion: string
      /// the NuGet package version
      NuGetVersion: string
      /// Semantic version
      SemVer: SemVerHelper.SemVerInfo
      /// Release DateTime
      Date: DateTime option
      /// The parsed list of changes
      Changes: Change list 
      /// True, if the entry was yanked 
      IsYanked: bool }

    override x.ToString() = sprintf "%A" x

    static member New(assemblyVersion, nugetVersion, date, changes, isYanked) = {
        AssemblyVersion = assemblyVersion
        NuGetVersion = nugetVersion
        SemVer = SemVerHelper.parse nugetVersion
        Date = date
        Changes = changes
        IsYanked = isYanked }
    
    static member New(assemblyVersion, nugetVersion, changes) = ChangeLogEntry.New(assemblyVersion, nugetVersion, None, changes, false)

type ChangeLog =
    { /// The description
      Description: string option
      Unreleased: Change list
      Entries: ChangeLogEntry list }

    /// the latest change log entry
    member x.LatestEntry = x.Entries |> Seq.head

    static member New(description, unreleased, entries) = 
        {
            Description = description
            Unreleased = unreleased
            Entries = entries 
        }

/// Parses a change log text and returns the change log.
///
/// ## Parameters
///  - `data` - change log text
let parseChangeLog (data: seq<string>) : ChangeLog =
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
    
    let rec findFirstHeader accumulator lines =
        match lines with
        | [] -> accumulator |> List.filter (not << isNullOrWhiteSpace), []
        | line :: rest when "# " <* line -> accumulator, lines
        | _ :: rest -> rest |> findFirstHeader accumulator

    let preHeaderLines, data = data |> Seq.toList |> findFirstHeader []
    
    if preHeaderLines|> List.exists (not << isNullOrWhiteSpace)
    then failwith "Invalid format: Changelog must begin with a Top level header!"

    match data |> List.filter (not << isNullOrWhiteSpace) with
    | [] -> failwith "Empty change log file."
    | _ :: text ->
        let isUnreleasedHeader line = "## " <* line && line.Contains("[Unreleased]")
        let isBlockHeader line = "## " <* line && not <| line.Contains("[Unreleased]")
        let isCategoryHeader line = "### " <* line
        let isAnyHeader line = isBlockHeader line || isCategoryHeader line

        let rec findEnd headerPredicate accumulator lines =
            match lines with
            | [] -> accumulator,[]
            | line :: rest when line |> headerPredicate -> accumulator, lines
            | line :: rest -> rest |> findEnd headerPredicate (line :: accumulator)

        let rec findBlockEnd accumulator lines = findEnd isBlockHeader accumulator lines

        let rec findUnreleasedBlock (text: string list): (string list * string list) option = 
            match text with
            | [] -> None
            | h :: rest when h |> isUnreleasedHeader -> rest|> findBlockEnd [] |> Some
            | _ :: rest -> findUnreleasedBlock rest

        let rec findNextChangesBlock text = 
            match text with
            | [] -> None
            | h :: rest when h |> isBlockHeader -> Some(h, rest |> findBlockEnd [])
            | _ :: rest -> findNextChangesBlock rest

        let rec findNextCategoryBlock text = 
            let rec findCategoryEnd changes text =
                match text with
                | [] -> changes |> List.filter isNotNullOrEmpty,[]
                | h :: rest when h |> isAnyHeader -> changes |> List.filter isNotNullOrEmpty, text
                | h :: rest -> rest |> findCategoryEnd (h :: changes)

            match text with
            | [] -> None
            | h :: rest when h |> isCategoryHeader -> Some(h, findCategoryEnd [] rest) 
            | _ :: rest -> findNextCategoryBlock rest

        let rec categoryLoop (changes: Change list) (text: string list) : Change list =
            match findNextCategoryBlock text with
            | Some (header, (changeLines, rest)) ->
                categoryLoop ((changeLines |> List.map trimLine |> List.filter isNotNullOrEmpty |> List.rev |> List.map (fun line -> Change.New(header,line))) |> List.append changes) rest
            | None -> changes

        let rec loop changeLogEntries text =
            match findNextChangesBlock text with
            | Some (header, (changes, rest)) ->
                let assemblyVer, nugetVer = parseVersions header
                let date = parseDate header
                let changeLines = categoryLoop [] (changes |> List.filter isNotNullOrEmpty |> List.rev)
                let isYanked = (header |> toLower).Contains("[yanked]")
                let newChangeLogEntry = ChangeLogEntry.New(assemblyVer.Value, nugetVer.Value, date, changeLines, isYanked)
                loop (newChangeLogEntry::changeLogEntries) rest
            | None -> changeLogEntries
        
        let description = 
            let descriptionLines, _ = 
                let isBlockOrUnreleasedHeader line = isUnreleasedHeader line || isBlockHeader line 
                findEnd isBlockOrUnreleasedHeader [] (data |> Seq.filter (not << (startsWith "# ")) |> Seq.toList)

            match descriptionLines |> List.rev with
            | [] -> None 
            | lines -> lines |> List.map trim |> separated "\n" |> trim |> Some

        let unreleased =
            match findUnreleasedBlock text with
            | Some (changes, _) ->
                categoryLoop [] (changes |> List.filter isNotNullOrEmpty |> List.rev)
            | None -> []

        let entries = (loop [] text |> List.sortBy (fun x -> x.SemVer) |> List.rev)
        
        ChangeLog.New(description, unreleased, entries)


/// Parses a Change log text file and returns the lastest change log.
///
/// ## Parameters
///  - `fileName` - ChangeLog text file name
let LoadChangeLog fileName =
    System.IO.File.ReadLines fileName
    |> parseChangeLog
