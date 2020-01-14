/// Contains helpers which allow to parse Change log text files.
/// These files have to be in a format as described on http://keepachangelog.com/en/1.1.0/
///
/// ## Sample
///
///     let changelogFile = "CHANGELOG.md"
///     let newVersion = "1.0.0"
///     
///     Target.create "AssemblyInfo" (fun _ ->
///         let changelog = changeLogFile |> Changelog.load
///         CreateFSharpAssemblyInfo "src/Common/AssemblyInfo.fs"
///           [ Attribute.Title project
///             Attribute.Product project
///             Attribute.Description summary
///             Attribute.Version changelog.LatestEntry.AssemblyVersion
///             Attribute.FileVersion changelog.LatestEntry.AssemblyVersion]
///     )
///
///     Target.create "Promote Unreleased to new version" (fun _ ->
///         let newChangelog = 
///             changelogFile 
///             |> ChangeLog.load
///             |> ChangeLog.promoteUnreleased newVersion
///             |> ChangeLog.save changelogFile
///     )
[<RequireQualifiedAccess>]
module Fake.Core.Changelog

open System
open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.String.Operators

let private multiNewLineRegex = String.getRegEx @"(\r\n|\n\r|\n|\r)(\r\n|\n\r|\n|\r)(?:\s*(\r\n|\n\r|\n|\r))+"
let private trimEnd = String.trimEndChars [|' '; '\n'; '\t'; '\r' |]
let private trimLine = String.trimStartChars [|' '; '*'; '#'; '-'|] >> trimEnd
let private trimLines lines = lines |> Seq.map trimLine |> Seq.toList
let private joinLines lines = lines |> String.separated "\n"
let private concat strings = strings |> String.separated ""
let internal contains (search: string) (line: string) = line.Contains(search)
let internal isNotNullOrWhitespace = String.isNullOrWhiteSpace >> not
let internal fixMultipleNewlines x = multiNewLineRegex.Replace(x, "\n\n")
let internal betweenNewlines x = "\n" + x + "\n"
let internal appendNewlines = String.trim >> sprintf "%s\n\n"

type ChangeText =  { CleanedText: string
                     OriginalText: string option }

type Change =
    /// for new features
    | Added of ChangeText
    /// for changes in existing functionality
    | Changed of ChangeText
    /// for once-stable features removed in upcoming releases
    | Deprecated of ChangeText
    /// for deprecated features removed in this release
    | Removed of ChangeText
    /// for any bug fixes
    | Fixed of ChangeText
    /// to invite users to upgrade in case of vulnerabilities
    | Security of ChangeText
    /// Custom entry (Header, Description)
    | Custom of string * ChangeText

    override x.ToString() = 
        match x with
        | Added s -> sprintf "Added: %s" s.CleanedText
        | Changed s -> sprintf "Changed: %s" s.CleanedText
        | Deprecated s -> sprintf "Deprecated: %s" s.CleanedText
        | Removed s -> sprintf "Removed: %s" s.CleanedText
        | Fixed s -> sprintf "Fixed: %s" s.CleanedText
        | Security s -> sprintf "Security: %s" s.CleanedText
        | Custom (h, s) -> sprintf "%s: %s" h s.CleanedText

    static member New(header: string, line: string): Change = 
        let text = { CleanedText = line |> trimLine
                     OriginalText = match line with
                                    | l when l |> String.isNullOrWhiteSpace -> l |> String.trim |> Some
                                    | l when not ("-" <* l) -> Some (l |> trimEnd)
                                    | _ -> None }

        match header |> trimLine |> String.toLower with
        | "added" -> Added text
        | "changed" -> Changed text
        | "deprecated" -> Deprecated text
        | "removed" -> Removed text
        | "fixed" -> Fixed text
        | "security" -> Security text
        | _ -> Custom (header |> trimLine, text)

let private makeEntry change =
    let bullet text = 
        match text.OriginalText with
        | Some text -> text
        | None -> sprintf "- %s" text.CleanedText

    match change with 
    | Added c -> "\n### Added", (bullet c)
    | Changed c -> "\n### Changed", (bullet c)
    | Deprecated c -> "\n### Deprecated", (bullet c)
    | Removed c -> "\n### Removed", (bullet c)
    | Fixed c -> "\n### Fixed", (bullet c)
    | Security c -> "\n### Security", (bullet c)
    | Custom (h, c) -> (sprintf "\n### %s" h), (bullet c)

let private makeDescriptionText text =
    match text with
    | Some text -> text |> String.trim
    | None -> ""

let private makeChangesText changes = 
    changes
    |> List.map makeEntry
    |> Seq.groupBy fst
    |> Seq.map (fun (key, values) -> key :: (values |> Seq.map (snd >> trimEnd) |> Seq.toList) |> joinLines)
    |> joinLines
    |> fixMultipleNewlines
    |> String.trim

type ChangelogEntry =
    { /// the parsed Version
      AssemblyVersion: string
      /// the NuGet package version
      NuGetVersion: string
      /// Semantic version
      SemVer: SemVerInfo
      /// Release DateTime
      Date: DateTime option
      /// a descriptive text (after the header)
      Description: string option
      /// The parsed list of changes
      Changes: Change list 
      /// True, if the entry was yanked 
      IsYanked: bool }

    override x.ToString() = 
        let header = 
            let isoDate =
                match x.Date with
                | Some d -> d.ToString(" - yyyy-MM-dd")
                | None -> ""

            let yanked = if x.IsYanked then " [YANKED]" else ""

            sprintf "## [%s]%s%s" x.NuGetVersion isoDate yanked

        (sprintf "%s\n\n%s\n\n%s" header (makeDescriptionText x.Description) (makeChangesText x.Changes))
        |> fixMultipleNewlines
        |> String.trim

    static member New(assemblyVersion, nugetVersion, date, description, changes, isYanked) = {
        AssemblyVersion = assemblyVersion
        NuGetVersion = nugetVersion
        SemVer = SemVer.parse nugetVersion
        Date = date
        Description = description
        Changes = changes
        IsYanked = isYanked }
    
    static member New(assemblyVersion, nugetVersion, changes) = 
        ChangelogEntry.New(assemblyVersion, nugetVersion, None, None, changes, false)

type Unreleased = 
    { Description: string option
      Changes: Change list }

    override x.ToString() =
        (sprintf "## [Unreleased]\n\n%s\n\n%s" (makeDescriptionText x.Description) (makeChangesText x.Changes))
        |> fixMultipleNewlines
        |> String.trim

    static member New(description, changes) =
        match description with
        | Some _ -> Some { Description = description; Changes = changes }
        | None ->
            match changes with
            | [] -> None
            | _ -> Some { Description = description; Changes = changes }

let internal nugetRegex = String.getRegEx @"([0-9]+.)+[0-9]+(-[a-zA-Z]+\d*)?(.[0-9]+)?"
let internal assemblyVersionRegex = String.getRegEx @"([0-9]+\.)+[0-9]+"

let parseVersions = 
    fun line ->
        let assemblyVersion = assemblyVersionRegex.Match line
        if not assemblyVersion.Success
        then failwithf "Unable to parse valid Assembly version from change log(%s)." line

        let nugetVersion = nugetRegex.Match line
        if not nugetVersion.Success
        then failwithf "Unable to parse valid NuGet version from change log (%s)." line
        assemblyVersion, nugetVersion

type Changelog =
    { /// the header line
      Header: string
      /// The description
      Description: string option
      /// The Unreleased section
      Unreleased: Unreleased option
      /// The change log entries
      Entries: ChangelogEntry list }

    /// the latest change log entry
    member x.LatestEntry = x.Entries |> Seq.head

    static member New(header, description, unreleased, entries) = 
        {
            Header = header
            Description = description
            Unreleased = unreleased
            Entries = entries 
        }

    member x.PromoteUnreleased(assemblyVersion: string, nugetVersion: string) : Changelog =
        match x.Unreleased with
        | None -> x
        | Some u -> 
            let newEntry = ChangelogEntry.New(assemblyVersion, nugetVersion, Some (System.DateTime.Today), u.Description, u.Changes, false)

            Changelog.New(x.Header, x.Description, None, newEntry :: x.Entries)

    member x.PromoteUnreleased(version: string) : Changelog =
        let assemblyVersion, nugetVersion = version |> parseVersions
        x.PromoteUnreleased(assemblyVersion.Value, nugetVersion.Value)

    override x.ToString() =
        let description = makeDescriptionText x.Description
        
        let unreleased =
            match x.Unreleased with
            | Some u -> match u |> string with
                        | text when text |> String.isNullOrWhiteSpace -> ""
                        | text -> text |> appendNewlines
            | _ -> ""

        let entries =
            x.Entries
            |> List.map (fun entry -> match entry |> string with
                                      | text when text |> String.isNullOrWhiteSpace -> ""
                                      | text -> text |> appendNewlines )
            |> concat
            |> String.trim
            |> fun e -> unreleased + e
            |> fixMultipleNewlines
            |> String.trim

        let header = 
            match x.Header |> String.trim with
            | "" -> "Changelog"
            | h -> h

        (sprintf "# %s\n\n%s\n\n%s" header description entries)
        |> fixMultipleNewlines
        |> String.trim

let createWithCustomHeader header description unreleased entries = Changelog.New (header, description, unreleased, entries)
let create description unreleased entries = createWithCustomHeader "Changelog" description unreleased entries
let fromEntries entries = create None None entries

let internal isMainHeader line : bool = "# " <* line
let internal isVersionHeader line : bool = "## " <* line
let internal isCategoryHeader line = "### " <* line
let internal containsUnreleasedHeader (line: string) : bool = (line |> contains "## Unreleased") || (line |> contains "[Unreleased]")
let internal isUnreleasedHeader line = (isVersionHeader line) && (line |> containsUnreleasedHeader)
let internal isBlockHeader line = (isVersionHeader line) && (line |> containsUnreleasedHeader |> not)
let internal isAnyHeader line = isBlockHeader line || isCategoryHeader line

/// Parses a change log text and returns the change log.
///
/// ## Parameters
///  - `data` - change log text
let parse (data: seq<string>) : Changelog =
    let parseDate =
        let dateRegex = String.getRegEx @"(19|20)\d\d([- /.])(0[1-9]|1[012]|[1-9])\2(0[1-9]|[12][0-9]|3[01]|[1-9])"
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
        | [] -> accumulator, []
        | line :: _ when isMainHeader line -> accumulator, lines
        | _ :: rest -> rest |> findFirstHeader accumulator

    let preHeaderLines, data = data |> Seq.toList |> findFirstHeader []
    
    if preHeaderLines |> List.exists isNotNullOrWhitespace
    then failwith "Invalid format: Changelog must begin with a Top level header!"
    
    if not (data |> List.exists isNotNullOrWhitespace)
    then failwith "Empty change log file."

    match data with
    | [] -> failwith "Empty change log file."
    | header :: text ->
        let rec findEnd headerPredicate accumulator lines =
            match lines with
            | [] -> accumulator, []
            | line :: _ when line |> headerPredicate -> accumulator, lines
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
                | [] -> changes, []
                | h :: _ when h |> isAnyHeader -> changes, text
                | h :: rest -> rest |> findCategoryEnd (h :: changes)

            match text with
            | [] -> None
            | h :: rest when h |> isCategoryHeader -> Some(h, findCategoryEnd [] rest) 
            | _ :: rest -> findNextCategoryBlock rest

        let rec categoryLoop (changes: Change list) (text: string list) : Change list =
            match findNextCategoryBlock text with
            | Some (header, (changeLines, rest)) ->
                categoryLoop ((changeLines |> List.rev |> List.map (fun line -> Change.New(header,line))) |> List.append changes) rest
            | None -> changes

        let rec loop changeLogEntries text =
            match findNextChangesBlock text with
            | Some (header, (changes, rest)) ->
                let assemblyVer, nugetVer = parseVersions header
                let date = parseDate header
                let changeLines = categoryLoop [] (changes |> List.rev)
                let isYanked = header |> String.toLower |> contains "[yanked]"
                let description = 
                    let descriptionLines, _ =
                        let isBlockOrCategoryHeader line = isCategoryHeader line || isBlockHeader line 
                        findEnd isBlockOrCategoryHeader [] (changes |> Seq.toList |> List.rev)

                    match descriptionLines |> List.rev with
                    | [] -> None 
                    | lines -> lines |> joinLines |> trimEnd |> Some

                let newChangeLogEntry = ChangelogEntry.New(assemblyVer.Value, nugetVer.Value, date, description, changeLines, isYanked)
                loop (newChangeLogEntry::changeLogEntries) rest
            | None -> changeLogEntries
        
        let description = 
            let descriptionLines, _ = 
                let isBlockOrUnreleasedHeader line = isUnreleasedHeader line || isBlockHeader line 
                findEnd isBlockOrUnreleasedHeader [] (data |> Seq.filter (not << isMainHeader) |> Seq.toList)

            match descriptionLines |> List.rev with
            | [] -> None 
            | lines -> lines |> List.map String.trim |> joinLines |> String.trim |> Some

        let unreleased =
            match findUnreleasedBlock text with
            | Some (changes, _) ->
                let unreleasedChanges = categoryLoop [] (changes |> List.rev)

                let description = 
                    let descriptionLines, _ = 
                        let isBlockOrCategoryHeader line = isCategoryHeader line || isBlockHeader line 
                        findEnd isBlockOrCategoryHeader [] (changes |> Seq.toList |> List.rev)

                    match descriptionLines |> List.rev with
                    | [] -> None 
                    | lines -> lines |> List.map String.trim |> joinLines |> String.trim |> Some

                Unreleased.New(description, unreleasedChanges)
            | _ -> None

        let entries = (loop [] text |> List.sortBy (fun x -> x.SemVer) |> List.rev)
        
        let header = 
            if isMainHeader header then
                header |> trimLine
            else
                match text |> List.filter isMainHeader with
                | h :: _ -> h
                | _ -> "Changelog"

        Changelog.New(header, description, unreleased, entries)

/// Parses a Changelog text file and returns the lastest changelog.
///
/// ## Parameters
///  - `filename` - Changelog text file name
/// 
/// ## Returns
/// The loaded changelog (or throws an exception, if the changelog could not be parsed)
let load filename =
    System.IO.File.ReadLines filename
    |> parse

/// Saves a Changelog to a text file.
///
/// ## Parameters
///  - `filename` - Changelog text file name
///  - `changelog` - the changelog data
let save (filename: string) (changelog: Changelog) : unit =
    System.IO.File.WriteAllText(filename, changelog |> string)

/// Promotes the `Unreleased` section of a changelog
/// to a new changelog entry with the given version
///
/// ## Parameters
/// - `version` - The version (in NuGet-Version format, e.g. `3.13.4-alpha1.212`
/// - `changelog` - The changelog to promote
///
/// ## Returns
/// The promoted changelog
let promoteUnreleased (version: string) (changelog: Changelog) : Changelog =
    changelog.PromoteUnreleased(version)


