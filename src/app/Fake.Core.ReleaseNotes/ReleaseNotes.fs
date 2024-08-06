﻿namespace Fake.Core

/// <summary>
/// Contains helpers which allow to parse Release Notes text files. Either "simple" or "complex" format is accepted.
/// </summary>
/// <remarks>
/// <br/> Formats: <br/>
/// - Simple format
/// <example>
/// <code lang="markdown">
/// 1.1.10 - Support for heterogeneous XML attributes. Make CsvFile re-entrant.
/// 1.1.9 - Infer booleans for ints that only manifest 0 and 1.
/// </code>
/// </example>
/// <br/>
/// - Complex format
/// <example>
/// <code lang="markdown">
/// ### New in 1.1.10 (Released 2013/09/12)
///     * Support for heterogeneous XML attributes.
///     * Make CsvFile re-entrant.
///     * Support for compressed HTTP responses.
///     * Fix JSON conversion of 0 and 1 to booleans.
///
///     ### New in 1.1.9 (Released 2013/07/21)
///     * Infer booleans for ints that only manifest 0 and 1.
///     * Support for partially overriding the Schema in CsvProvider.
///     * PreferOptionals and SafeMode parameters for CsvProvider.
/// </code>
/// </example>
/// </remarks>
///
/// <example>
/// <code lang="fsharp">
/// #r &quot;paket:
///     nuget Fake.Core.ReleaseNotes //&quot;
///
///     let release = ReleaseNotes.load &quot;RELEASE_NOTES.md&quot;
///
///     Target &quot;AssemblyInfo&quot; (fun _ -&gt;
///         CreateFSharpAssemblyInfo &quot;src/Common/AssemblyInfo.fs&quot;
///           [ Attribute.Title project
///             Attribute.Product project
///             Attribute.Description summary
///             Attribute.Version release.AssemblyVersion
///             Attribute.FileVersion release.AssemblyVersion]
///     )
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module ReleaseNotes =

    open System
    open Fake.Core

    /// <summary>
    /// Contains the parsed information of the release notes text file.
    /// </summary>
    type ReleaseNotes =
        {
            /// The parsed version
            AssemblyVersion: string

            /// The nuget package version
            NugetVersion: string

            /// Semantic version
            SemVer: SemVerInfo

            /// Release date
            Date: DateTime option

            // The parsed release notes
            Notes: string list
        }

        override x.ToString() = sprintf "%A" x

        /// Create an instance of Release notes with given data
        static member New(assemblyVersion, nugetVersion, date, notes) =
            { AssemblyVersion = assemblyVersion
              NugetVersion = nugetVersion
              SemVer = SemVer.parse nugetVersion
              Date = date
              Notes = notes }

        /// Create an instance of Release notes with given data except release date
        static member New(assemblyVersion, nugetVersion, notes) =
            ReleaseNotes.New(assemblyVersion, nugetVersion, None, notes)

    let private parseVersions =
        // https://github.com/fsprojects/FAKE/issues/2557
        let nugetRegexLegacy = String.getRegEx @"([0-9]+.)+[0-9]+(-[a-zA-Z]+\d*)?(.[0-9]+)?"

        let nugetRegex =
            /// From Fake.Core.SemVer
            let pattern = SemVerActivePattern.Pattern
            String.getRegEx pattern

        let assemblyVersionRegex = String.getRegEx @"([0-9]+.)+[0-9]+"

        fun line ->
            let assemblyVersion = assemblyVersionRegex.Match line

            if not assemblyVersion.Success then
                failwithf "Unable to parse valid Assembly version from release notes (%s)." line

            let nugetVersion =
                let nugetVersion =
                    // Must split by whitespace to try match start of line and end of line in SemVer regex pattern
                    line.Split(' ')
                    |> Array.tryPick (fun segment ->
                        // Trim() might be unnecessary
                        let m = segment.Trim() |> nugetRegex.Match
                        if m.Success then Some m else None)
                // Add support for "nugetRegexLegacy" after change to correct SemVer parsing.
                // This should lead to the least disruption to users.
                let nugetVersionLegacy =
                    let m = nugetRegexLegacy.Match line
                    if m.Success then Some m else None

                match nugetVersion, nugetVersionLegacy with
                // if nugetVersion.IsSome then it must be Success, so no need to check for that
                | Some nugetVersionValue, _ -> nugetVersionValue
                | None, Some nugetVersionLegacyValue -> nugetVersionLegacyValue
                | None, none -> failwithf "Unable to parse valid Nuget version from release notes (%s)." line

            assemblyVersion, nugetVersion

    let private parseDate =
        let dateRegex =
            String.getRegEx @"(19|20)\d\d([- /.])(0[1-9]|1[012]|[1-9])\2(0[1-9]|[12][0-9]|3[01]|[1-9])"

        fun line ->
            let possibleDate = dateRegex.Match line

            match possibleDate.Success with
            | false -> None
            | true ->
                match DateTime.TryParse possibleDate.Value with
                | false, _ -> None
                | true, x -> Some(x)

    /// Parse simple release notes sequence
    let private parseSimple line =
        let assemblyVersion, nugetVersion = parseVersions line
        let trimDot (s: string) = s.TrimEnd('.')
        /// Find nugetVersion index in line. Necessary, since "nugetVersion" is created from line.Split(' ').
        let nugetVersionIndex = line.IndexOf nugetVersion.Value

        let notes =
            line.Substring(nugetVersionIndex + nugetVersion.Length)
            |> String.trimChars [| ' '; '-' |]
            |> String.splitStr ". "
            |> List.map (trimDot >> String.trim)
            |> List.filter String.isNotNullOrEmpty
            |> List.map (fun x -> x + ".")

        ReleaseNotes.New(assemblyVersion.Value, nugetVersion.Value, None, notes)

    open Fake.Core.String.Operators

    /// Parse "complex" release notes text sequence
    let private parseAllComplex (text: seq<string>) =
        let rec findNextNotesBlock text =
            let isHeader line = "##" <* line

            let rec findEnd notes text =
                match text with
                | [] -> notes, []
                | h :: rest ->
                    if isHeader h then
                        notes, text
                    else
                        findEnd (h :: notes) rest

            match text with
            | [] -> None
            | h :: rest ->
                if isHeader h then
                    Some(h, findEnd [] rest)
                else
                    findNextNotesBlock rest

        let rec loop releaseNotes text =
            match findNextNotesBlock text with
            | Some(header, (notes, rest)) ->
                let assemblyVer, nugetVer = parseVersions header
                let date = parseDate header

                let newReleaseNotes =
                    ReleaseNotes.New(
                        assemblyVer.Value,
                        nugetVer.Value,
                        date,
                        notes |> List.filter String.isNotNullOrEmpty |> List.rev
                    )

                loop (newReleaseNotes :: releaseNotes) rest
            | None -> releaseNotes

        let result =
            loop
                []
                (text
                 |> Seq.map (String.trimStartChars [| ' '; '*' |] >> String.trimEndChars [| ' ' |])
                 |> Seq.toList)

        if List.isEmpty result then
            failwithf "release note files containing only top level headers are not allowed"
        else
            result


    /// <summary>
    /// Parses a Release Notes text and returns all release notes.
    /// </summary>
    ///
    /// <param name="data">Release notes text</param>
    let parseAll (data: seq<string>) =
        let data = data |> Seq.toList |> List.filter (not << String.isNullOrWhiteSpace)

        match data with
        | [] -> failwith "Empty Release file."
        | h :: _ ->
            let (|Simple|Complex|Invalid|) =
                function
                | '*' -> Simple
                | '#' -> Complex
                | _ -> Invalid

            let firstNonEmptyChar = h.Trim([| '-'; ' ' |]).[0]

            match firstNonEmptyChar with
            | Simple -> data |> Seq.map parseSimple |> Seq.toList
            | Complex -> parseAllComplex data
            | Invalid -> failwith "Invalid Release Notes format."
            |> List.sortBy (fun x -> x.SemVer)
            |> List.rev


    /// <summary>
    /// Parses a Release Notes text and returns the latest release notes.
    /// </summary>
    ///
    /// <param name="data">Release notes text</param>
    let parse (data: seq<string>) =
        match data |> parseAll |> Seq.tryHead with
        | Some head -> head
        | None ->
            failwithf
                "The release notes document was not valid, see https://fake.build/reference/fake-core-releasenotes.html for the allowed formats"

    /// <summary>
    /// Parses a Release Notes text file and returns the latest release notes.
    /// </summary>
    ///
    /// <param name="fileName">Release notes text file name</param>
    let load fileName =
        System.IO.File.ReadLines fileName |> parse
