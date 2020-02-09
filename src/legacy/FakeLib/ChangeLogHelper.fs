/// Contains helpers which allow to parse Change log text files.
/// These files have to be in a format as described on http://keepachangelog.com/en/1.1.0/
///
/// ## Sample
///
///     let changeLogFile = "CHANGELOG.md"
///     let newVersion = "1.0.0"
///     
///     Target "AssemblyInfo" (fun _ ->
///         let changeLog = changeLogFile |> ChangeLogHelper.LoadChangeLog
///         CreateFSharpAssemblyInfo "src/Common/AssemblyInfo.fs"
///           [ Attribute.Title project
///             Attribute.Product project
///             Attribute.Description summary
///             Attribute.Version changeLog.LatestEntry.AssemblyVersion
///             Attribute.FileVersion changeLog.LatestEntry.AssemblyVersion]
///     )
///
///     Target "Promote Unreleased to new version" (fun _ ->
///         let newChangeLog = 
///             changeLogFile 
///             |> ChangeLogHelper.LoadChangeLog
///             |> ChangeLogHelper.PromoteUnreleased newVersion
///             |> ChangeLogHelper.SavceChangeLog changeLogFile
///     )
[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog)")>]
module Fake.ChangeLogHelper

open System
open System.Text.RegularExpressions
open Fake.AssemblyInfoFile

let private trimLine = trimStartChars [|' '; '*'; '#'; '-'|] >> trimEndChars [|' '|]
let private trimLines lines = lines |> Seq.map trimLine |> Seq.toList

[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Change)")>]
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

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Change, member: ToString)")>]
    override x.ToString() = 
        match x with
        | Added s -> sprintf "Added: %s" s
        | Changed s -> sprintf "Changed: %s" s
        | Deprecated s -> sprintf "Deprecated: %s" s
        | Removed s -> sprintf "Removed: %s" s
        | Fixed s -> sprintf "Fixed: %s" s
        | Security s -> sprintf "Security: %s" s
        | Custom (h, s) -> sprintf "%s: %s" h s

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Change, member: New)")>]
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


let private makeEntry change =
    let bullet text = sprintf "- %s" text

    match change with 
    | Added c -> @"\n### Added", (bullet c)
    | Changed c -> @"\n### Changed", (bullet c)
    | Deprecated c -> @"\n### Deprecated", (bullet c)
    | Removed c -> @"\n### Removed", (bullet c)
    | Fixed c -> @"\n### Fixed", (bullet c)
    | Security c -> @"\n### Security", (bullet c)
    | Custom (h, c) -> (sprintf @"\n### %s" h), (bullet c)

[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: ChangelogEntry)")>]
type ChangeLogEntry =
    { /// the parsed Version
      AssemblyVersion: string
      /// the NuGet package version
      NuGetVersion: string
      /// Semantic version
      SemVer: SemVerHelper.SemVerInfo
      /// Release DateTime
      Date: DateTime option
      /// a descriptive text (after the header)
      Description: string option
      /// The parsed list of changes
      Changes: Change list 
      /// True, if the entry was yanked 
      IsYanked: bool }

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: ChangelogEntry, member: ToString)")>]
    override x.ToString() = 
        let header = 
            let isoDate =
                match x.Date with
                | Some d -> d.ToString(" - yyyy-MM-dd")
                | None -> ""

            let yanked = if x.IsYanked then " [YANKED]" else ""

            sprintf "## %s%s%s\n" x.NuGetVersion isoDate yanked 

        let description =
            match x.Description with
            | Some text -> sprintf @"\n%s\n" (text |> trim)
            | None -> ""

        let changes = 
            x.Changes
            |> List.map makeEntry
            |> Seq.groupBy fst
            |> Seq.map (fun (key, values) -> key :: (values |> Seq.map snd |> Seq.toList) |> separated @"\n")
            |> separated @"\n"


        (sprintf @"%s%s%s" header description changes).Replace(@"\n", Environment.NewLine).Trim()

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: ChangelogEntry, member: New)")>]
    static member New(assemblyVersion, nugetVersion, date, description, changes, isYanked) = {
        AssemblyVersion = assemblyVersion
        NuGetVersion = nugetVersion
        SemVer = SemVerHelper.parse nugetVersion
        Date = date
        Description = description
        Changes = changes
        IsYanked = isYanked }
    
    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type ChangelogEntry, member: ToString)")>]
    static member New(assemblyVersion, nugetVersion, changes) = ChangeLogEntry.New(assemblyVersion, nugetVersion, None, None, changes, false)

[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Unreleased)")>]
type Unreleased = 
    { Description: string option
      Changes: Change list }

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Unreleased, member: ToString)")>]
    override x.ToString() =
        let header = @"## Unreleased\n"
        
        let description =
            match x.Description with
            | Some text -> sprintf @"\n%s\n" (text |> trim)
            | None -> ""

        let changes = 
            x.Changes
            |> List.map makeEntry
            |> Seq.groupBy fst
            |> Seq.map (fun (key, values) -> key :: (values |> Seq.map snd |> Seq.toList) |> separated @"\n")
            |> separated @"\n"

        (sprintf @"%s%s%s" header description changes).Replace(@"\n", Environment.NewLine).Trim()

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Unreleased, member: New)")>]
    static member New(description, changes) =
        match description with
        | Some _ -> Some { Description = description; Changes = changes }
        | None ->
            match changes with
            | [] -> None
            | _ -> Some { Description = description; Changes = changes }

[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, function: parseVersions)")>]
let internal parseVersions = 
    let nugetRegex = getRegEx @"([0-9]+.)+[0-9]+(-[a-zA-Z]+\d*)?(.[0-9]+)?"
    fun line ->
        let assemblyVersion = assemblyVersionRegex.Match line
        if not assemblyVersion.Success
        then failwithf "Unable to parse valid Assembly version from change log(%s)." line

        let nugetVersion = nugetRegex.Match line
        if not nugetVersion.Success
        then failwithf "Unable to parse valid NuGet version from change log (%s)." line
        assemblyVersion, nugetVersion

[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Changelog)")>]
type ChangeLog =
    { /// the header line
      Header: string
      /// The description
      Description: string option
      /// The Unreleased section
      Unreleased: Unreleased option
      /// The change log entries
      Entries: ChangeLogEntry list }

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Changelog, member: LatestEntry)")>]
    /// the latest change log entry
    member x.LatestEntry = x.Entries |> Seq.head

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Changelog, member: New)")>]
    static member New(header, description, unreleased, entries) = 
        {
            Header = header
            Description = description
            Unreleased = unreleased
            Entries = entries 
        }

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Changelog, member: New)")>]
    static member New(description, unreleased, entries) =
        ChangeLog.New("Changelog", description, unreleased, entries)

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Changelog, member: New)")>]
    static member New(entries) =
        ChangeLog.New(None, None, entries)

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Changelog, member: PromoteUnreleased)")>]
    member x.PromoteUnreleased(assemblyVersion: string, nugetVersion: string) : ChangeLog =
        match x.Unreleased with
        | None -> x
        | Some u -> 
            let newEntry = ChangeLogEntry.New(assemblyVersion, nugetVersion, Some (System.DateTime.Today), u.Description, u.Changes, false)

            ChangeLog.New(x.Header, x.Description, None, newEntry :: x.Entries)

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Changelog, member: PromoteUnreleased)")>]
    member x.PromoteUnreleased(version: string) : ChangeLog =
        let assemblyVersion, nugetVersion = version |> parseVersions
        x.PromoteUnreleased(assemblyVersion.Value, nugetVersion.Value)

    [<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, type: Changelog, member: ToString)")>]
    override x.ToString() =
        let description = 
            match x.Description with
            | Some d -> sprintf @"\n%s\n" d
            | _ -> ""

        let unreleased =
            match x.Unreleased with
            | Some u -> sprintf @"\n%s\n" (u.ToString())
            | _ -> ""

        let entries =
            x.Entries
            |> List.map (fun e -> sprintf @"\n%s\n" (e.ToString()))
            |> separated @""

        let header = 
            match x.Header |> trim with
            | "" -> "Changelog"
            | h -> h

        (sprintf @"# %s\n%s%s%s" header description unreleased entries).Replace(@"\n", Environment.NewLine) |> trim

/// Parses a change log text and returns the change log.
///
/// ## Parameters
///  - `data` - change log text
[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, function: parse)")>]
let parseChangeLog (data: seq<string>) : ChangeLog =
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
    | header :: text ->
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
                let description = 
                    let descriptionLines, _ =
                        let isBlockOrCategoryHeader line = isCategoryHeader line || isBlockHeader line 
                        findEnd isBlockOrCategoryHeader [] (changes |> Seq.toList |> List.rev)

                    match descriptionLines |> List.rev with
                    | [] -> None 
                    | lines -> lines |> List.map trim |> separated "\n" |> trim |> Some

                let newChangeLogEntry = ChangeLogEntry.New(assemblyVer.Value, nugetVer.Value, date, description, changeLines, isYanked)
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
                let unreleasedChanges = categoryLoop [] (changes |> List.filter isNotNullOrEmpty |> List.rev)

                let description = 
                    let descriptionLines, _ = 
                        let isBlockOrCategoryHeader line = isCategoryHeader line || isBlockHeader line 
                        findEnd isBlockOrCategoryHeader [] (changes |> Seq.toList |> List.rev)

                    match descriptionLines |> List.rev with
                    | [] -> None 
                    | lines -> lines |> List.map trim |> separated "\n" |> trim |> Some

                Unreleased.New(description, unreleasedChanges)
            | _ -> None

        let entries = (loop [] text |> List.sortBy (fun x -> x.SemVer) |> List.rev)
        
        let header = 
            if "# " <* header then
                header |> trimLine
            else
                match text |> List.filter (startsWith "# ") with
                | h :: _ -> h |> trimLine
                | _ -> "Changelog"

        ChangeLog.New(header, description, unreleased, entries)


/// Parses a Change log text file and returns the lastest change log.
///
/// ## Parameters
///  - `fileName` - ChangeLog text file name
/// 
/// ## Returns
/// The loaded change log (or throws an exception, if the change log could not be parsed)
[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, function: load)")>]
let LoadChangeLog fileName =
    System.IO.File.ReadLines fileName
    |> parseChangeLog

/// Saves a Change log to a text file.
///
/// ## Parameters
///  - `fileName` - ChangeLog text file name
///  - `changeLog` - the change log data
[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, function: save)")>]
let SaveChangeLog (fileName: string) (changeLog: ChangeLog) : unit =
    System.IO.File.WriteAllText(fileName, changeLog.ToString())

/// Promotes the `Unreleased` section of a changelog
/// to a new change log entry with the given version
///
/// ## Parameters
/// - `version` - The version (in NuGet-Version format, e.g. `3.13.4-alpha1.212`
/// - `changeLog` - The change log to promote
///
/// ## Returns
/// The promoted change log
[<System.Obsolete("Open Fake.Core instead (FAKE0001 - package: Fake.Core.ReleaseNotes, module: Changelog, function: promoteUnreleased)")>]
let PromoteUnreleased (version: string) (changeLog: ChangeLog) : ChangeLog =
    changeLog.PromoteUnreleased(version)
