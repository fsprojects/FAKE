module Fake.Core.ReleaseNotesTests

open Fake.Core
open Expecto
open System

[<Literal>]
let private changelogReleasesText =
    """# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Changed
- Foo 2

## [0.1.0-pre.2] - 2023-10-19

### Added
- Foo 1

## [0.1.0-pre.1] - 2023-10-11

### Added
- Foo 0"""

[<Literal>]
let private changelogReferencesText =
    """[Unreleased]: https://github.com/bogus/Foo/compare/v0.1.0-pre.2...HEAD
[0.1.0-pre.2]: https://github.com/bogus/Foo/releases/tag/v0.1.0-pre.2
[0.1.0-pre.1]: https://github.com/bogus/Foo/releases/tag/v0.1.0-pre.1"""

[<Tests>]
let tests =
    testList
        "Fake.Core.ReleaseNotes.Tests"
        [ testCase "Test that we can handle same day releases"
          <| fun _ ->
              let releaseNotesLines =
                  [ "# Release Notes"
                    ""
                    "## 5.15.3 - 2019-07-03"
                    ""
                    "* BUGFIX: Disable fast restore for MSBuild version < 15.8 - https://github.com/fsprojects/Paket/pull/3611"
                    ""
                    "## 5.15.2 - 2019-07-03"
                    ""
                    "* BUGFIX: Fast Restore (bugfix from paket) - https://github.com/fsprojects/Paket/pull/3608"
                    "" ]

              let releaseNotes = ReleaseNotes.parse releaseNotesLines

              let (expected: ReleaseNotes.ReleaseNotes) =
                  // For historic reasons notes get appended a "."
                  { AssemblyVersion = "5.15.3"
                    NugetVersion = "5.15.3"
                    SemVer = SemVer.parse "5.15.3"
                    Date = Some(System.DateTime(2019, 07, 3))
                    Notes =
                      [ "BUGFIX: Disable fast restore for MSBuild version < 15.8 - https://github.com/fsprojects/Paket/pull/3611" ] }

              Expect.equal releaseNotes expected "Simple parse failure"

          testCase "Test that we can parse simple release notes"
          <| fun _ ->
              let releaseNotesLines = [ "* 1.0.0 - Initial version"; "* 1.1.0 - First change" ]
              let releaseNotes = ReleaseNotes.parse releaseNotesLines

              let (expected: ReleaseNotes.ReleaseNotes) =
                  // For historic reasons notes get appended a "."
                  { AssemblyVersion = "1.1.0"
                    NugetVersion = "1.1.0"
                    SemVer = SemVer.parse "1.1.0"
                    Date = None
                    Notes = [ "First change." ] }

              Expect.equal releaseNotes expected "Simple parse failure"

          testCase "Test that we can parse simple release notes (reversed)"
          <| fun _ ->
              let releaseNotesLines = [ "* 1.1.0 - First change"; "* 1.0.0 - Initial version" ]
              let releaseNotes = ReleaseNotes.parse releaseNotesLines

              let (expected: ReleaseNotes.ReleaseNotes) =
                  // For historic reasons notes get appended a "."
                  { AssemblyVersion = "1.1.0"
                    NugetVersion = "1.1.0"
                    SemVer = SemVer.parse "1.1.0"
                    Date = None
                    Notes = [ "First change." ] }

              Expect.equal releaseNotes expected "Simple parse failure"

          testCase "Test that we can parse complex release notes"
          <| fun _ ->
              let releaseNotesLines =
                  [ "## 1.0.0 - Released on 2017-02-03"
                    "- Some change 1"
                    "- Some change 2"
                    ""
                    "## 1.1.0 - Released on 2017-04-12"
                    "- Some change 3"
                    "- Some change 4" ]

              let releaseNotes = ReleaseNotes.parse releaseNotesLines

              let (expected: ReleaseNotes.ReleaseNotes) =
                  { AssemblyVersion = "1.1.0"
                    NugetVersion = "1.1.0"
                    SemVer = SemVer.parse "1.1.0"
                    Date = Some(System.DateTime(2017, 04, 12))
                    Notes = [ "- Some change 3"; "- Some change 4" ] }

              Expect.equal releaseNotes expected "Simple parse failure"

          testCase "Test that we can parse complex release notes (reversed)"
          <| fun _ ->
              let releaseNotesLines =
                  [ "## 1.1.0 - Released on 2017-04-12"
                    ""
                    "- Some change 3"
                    "- Some change 4"
                    ""
                    "## 1.0.0 - Released on 2017-02-03"
                    "- Some change 1"
                    "- Some change 2" ]

              let releaseNotes = ReleaseNotes.parse releaseNotesLines

              let (expected: ReleaseNotes.ReleaseNotes) =
                  { AssemblyVersion = "1.1.0"
                    NugetVersion = "1.1.0"
                    SemVer = SemVer.parse "1.1.0"
                    Date = Some(System.DateTime(2017, 04, 12))
                    Notes = [ "- Some change 3"; "- Some change 4" ] }

              Expect.equal releaseNotes expected "Simple parse failure"

          testCase "Test that we can parse complex release notes with header"
          <| fun _ ->
              let releaseNotesLines =
                  [ "# Global header"
                    ""
                    "## 1.1.0 - Released on 2017-04-12"
                    "- Some change 3"
                    "- Some change 4"
                    ""
                    "## 1.0.0 - Released on 2017-02-03"
                    "- Some change 1"
                    "- Some change 2" ]

              let releaseNotes = ReleaseNotes.parse releaseNotesLines

              let (expected: ReleaseNotes.ReleaseNotes) =
                  { AssemblyVersion = "1.1.0"
                    NugetVersion = "1.1.0"
                    SemVer = SemVer.parse "1.1.0"
                    Date = Some(System.DateTime(2017, 04, 12))
                    Notes = [ "- Some change 3"; "- Some change 4" ] }

              Expect.equal releaseNotes expected "Simple parse failure"

          testCase "Test that we provide proper error #2085"
          <| fun _ ->
              let releaseNotesLines =
                  [ "# 1.3.7"
                    ""
                    "* Bugfixes and public release"
                    ""
                    "# 1.3.2"
                    ""
                    "* Fix various bugs in the FAKE runner."
                    "" ]

              Expect.throwsC
                  (fun () -> ignore <| ReleaseNotes.parse releaseNotesLines)
                  (fun e ->
                      Expect.stringContains
                          e.Message
                          "files containing only top level headers are not allowed"
                          "Expected nice error message")

          // https://semver.org
          testList
              "SemVer"
              [ testCase "Test that we can parse basic SemVer."
                <| fun _ ->
                    let releaseNotesLines = [ "## 5.15.3 - 2019-07-03" ]
                    let releaseNotes = ReleaseNotes.parse releaseNotesLines
                    let semVer = releaseNotes.SemVer
                    Expect.equal semVer.Major 5u "Major"
                    Expect.equal semVer.Minor 15u "Minor"
                    Expect.equal semVer.Patch 3u "Patch"
                testCase "Test that we can parse SemVer with pre release."
                <| fun _ ->
                    let releaseNotesLines_case1 = [ "## 1.0.0-alpha - 2019-07-03" ]
                    let releaseNotesLines_case2 = [ "## 1.0.0-alpha.1 - 2019-07-03" ]
                    let releaseNotesLines_case3 = [ "## 1.0.0-0.3.7 - 2019-07-03" ]
                    let releaseNotesLines_case4 = [ "## 1.0.0-x.7.z.92 - 2019-07-03" ]

                    let checkPreRelease (rn: string list) (preReleaseName: string) =
                        let releaseNotes = ReleaseNotes.parse rn
                        let semVer = releaseNotes.SemVer
                        Expect.equal semVer.Major 1u $"{rn.[0]} Major"
                        Expect.equal semVer.Minor 0u $"{rn.[0]} Minor"
                        Expect.equal semVer.Patch 0u $"{rn.[0]} Patch"
                        Expect.isTrue semVer.PreRelease.IsSome $"{rn.[0]} PreRelease.IsSome"
                        Expect.equal semVer.PreRelease.Value.Origin preReleaseName $"{rn.[0]} PreRelease.Value.Origin"

                    checkPreRelease releaseNotesLines_case1 "alpha"
                    checkPreRelease releaseNotesLines_case2 "alpha.1"
                    checkPreRelease releaseNotesLines_case3 "0.3.7"
                    checkPreRelease releaseNotesLines_case4 "x.7.z.92"
                    checkPreRelease releaseNotesLines_case4 "x.7.z.92"
                testCase "Test that we can parse SemVer with build metadata."
                <| fun _ ->
                    let releaseNotesLines_case1 = [ "## 1.0.0-alpha+001 - 2019-07-03" ]
                    let releaseNotesLines_case2 = [ "## 1.0.0+20130313144700 - 2019-07-03" ]
                    let releaseNotesLines_case3 = [ "## 1.0.0-beta+exp.sha.5114f85 - 2019-07-03" ]

                    let checkPreRelease (rn: string list) (metadata: string) =
                        let releaseNotes = ReleaseNotes.parse rn
                        let semVer = releaseNotes.SemVer
                        Expect.equal semVer.Major 1u $"{rn.[0]} Major"
                        Expect.equal semVer.Minor 0u $"{rn.[0]} Minor"
                        Expect.equal semVer.Patch 0u $"{rn.[0]} Patch"
                        Expect.equal semVer.BuildMetaData metadata $"{rn.[0]} BuildMetaData"

                    checkPreRelease releaseNotesLines_case1 "001"
                    checkPreRelease releaseNotesLines_case2 "20130313144700"
                    checkPreRelease releaseNotesLines_case3 "exp.sha.5114f85"
                // Ignored simple patterns tested above.
                // https://regex101.com/r/Ly7O1x/3/
                testCase "Test that we can parse SemVer tests (link in comment)."
                <| fun _ ->
                    let releaseNotesLines_case1 =
                        [ "## 1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay - 2019-07-03" ]

                    let releaseNotesLines_case2 =
                        [ "## 1.0.0----RC-SNAPSHOT.12.9.1--.12+788 - 2019-07-03" ]

                    let releaseNotesLines_case3 = [ "## 1.0.0----R-S.12.9.1--.12+meta - 2019-07-03" ]

                    let releaseNotesLines_case4 =
                        [ "## 1.0.0+0.build.1-rc.10000aaa-kk-0.1 - 2019-07-03" ]

                    let releaseNotesLines_case5 = [ "## 1.0.0-0A.is.legal - 2019-07-03" ]

                    let checkPreRelease (rn: string list) (preReleaseName: string option) (metadata: string option) =
                        let releaseNotes = ReleaseNotes.parse rn
                        let semVer = releaseNotes.SemVer
                        Expect.equal semVer.Major 1u $"{rn.[0]} Major"
                        Expect.equal semVer.Minor 0u $"{rn.[0]} Minor"
                        Expect.equal semVer.Patch 0u $"{rn.[0]} Patch"

                        if preReleaseName.IsSome then
                            Expect.isTrue semVer.PreRelease.IsSome $"{rn.[0]} PreRelease.IsSome"

                            Expect.equal
                                semVer.PreRelease.Value.Origin
                                preReleaseName.Value
                                $"{rn.[0]} PreRelease.Value.Origin"

                        if metadata.IsSome then
                            Expect.equal semVer.BuildMetaData metadata.Value $"{rn.[0]} BuildMetaData"

                    checkPreRelease
                        releaseNotesLines_case1
                        (Some "alpha-a.b-c-somethinglong")
                        (Some "build.1-aef.1-its-okay")

                    checkPreRelease releaseNotesLines_case2 (Some "---RC-SNAPSHOT.12.9.1--.12") (Some "788")
                    checkPreRelease releaseNotesLines_case3 (Some "---R-S.12.9.1--.12") (Some "meta")
                    checkPreRelease releaseNotesLines_case4 (None) (Some "0.build.1-rc.10000aaa-kk-0.1")
                    checkPreRelease releaseNotesLines_case5 (Some "0A.is.legal") (None) ]

          // https://keepachangelog.com
          testList
              "Changelog"
              [ testCase "Test that we can parse changelog without references"
                <| fun _ ->
                    let changelog = changelogReleasesText |> String.splitStr "\n" |> Changelog.parse

                    Expect.isEmpty changelog.References "References not empty"
                    Expect.isSome changelog.Unreleased "Unreleased section empty"
                    Expect.hasLength changelog.Entries 2 "Wrong number of release entries parsed"
                testCase "Test that we can parse changelog with references"
                <| fun _ ->
                    let changelogText = changelogReleasesText + "\n\n" + changelogReferencesText
                    let changelog = changelogText |> String.splitStr "\n" |> Changelog.parse

                    Expect.hasLength changelog.References 3 "Wrong number of references parsed"

                    Expect.hasLength
                        (changelog.References
                         |> List.filter (fun r ->
                             match r.SemVer with
                             | Changelog.SemVerRef (_) -> true
                             | _ -> false))
                        2
                        "Wrong number of released references parsed"

                    Expect.hasLength
                        (changelog.References
                         |> List.filter (fun r ->
                             match r.SemVer with
                             | Changelog.SemVerRef (_) -> false
                             | _ -> true))
                        1
                        "Wrong number of unreleased references parsed"

                    Expect.hasLength changelog.References 3 "Wrong number of references parsed"
                    Expect.isSome changelog.Unreleased "Unreleased section empty"
                    Expect.hasLength changelog.Entries 2 "Wrong number of release entries parsed"
                testCase "Test that references are not in the last changelog entry"
                <| fun _ ->
                    let changelogText = changelogReleasesText + "\n\n" + changelogReferencesText
                    let changelog = changelogText |> String.splitStr "\n" |> Changelog.parse
                    let lastEntry = changelog.Entries |> List.last
                    let lastChanges = lastEntry.Changes

                    Expect.isFalse
                        (lastChanges
                         |> List.exists (fun change ->
                             change.ChangeText().CleanedText.Contains("https://github.com/bogus/Foo/")))
                        "URL of reference contained in change text"
                testCase "Test that a release and reference can be added and correctly turned into a string"
                <| fun _ ->
                    let changelogText = changelogReleasesText + "\n\n" + changelogReferencesText
                    let changelog = changelogText |> String.splitStr "\n" |> Changelog.parse
                    let versionText = "0.1.0-pre.3"
                    let semVerInfo = SemVer.parse versionText

                    let newUnreleasedRef =
                        { Changelog.Reference.SemVer = Changelog.UnreleasedRef
                          Changelog.Reference.RepoUrl = Uri("https://github.com/bogus/Foo/compare/v0.1.0-pre.3...HEAD") }

                    let releasedRefs =
                        changelog.References
                        |> List.filter (fun r ->
                            match r.SemVer with
                            | Changelog.SemVerRef (_) -> true
                            | _ -> false)

                    let newReference =
                        { Changelog.Reference.SemVer = Changelog.SemVerRef(semVerInfo)
                          Changelog.Reference.RepoUrl = Uri("https://github.com/bogus/Foo/releases/tag/v0.1.0-pre.3") }

                    let newFixed =
                        Changelog.Fixed(
                            { CleanedText = "Foo 3"
                              OriginalText = None }
                        )

                    let newReleaseEntry =
                        Changelog.ChangelogEntry.New(
                            "",
                            versionText,
                            Some(DateTime(2023, 11, 23)),
                            None,
                            [ newFixed ],
                            false
                        )

                    let changelogNew =
                        { changelog with
                            Entries = newReleaseEntry :: changelog.Entries
                            References = [ newUnreleasedRef; newReference ] @ releasedRefs }

                    let expectedEnd =
                        """[Unreleased]: https://github.com/bogus/Foo/compare/v0.1.0-pre.3...HEAD
[0.1.0-pre.3]: https://github.com/bogus/Foo/releases/tag/v0.1.0-pre.3
[0.1.0-pre.2]: https://github.com/bogus/Foo/releases/tag/v0.1.0-pre.2
[0.1.0-pre.1]: https://github.com/bogus/Foo/releases/tag/v0.1.0-pre.1"""

                    Expect.stringEnds
                        (changelogNew.ToString())
                        expectedEnd
                        "Invalid references at end of changelog text" ] ]
