module Fake.Core.ReleaseNotesTests

open Fake.Core
open Expecto

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

              Expect.throwsC (fun () -> ignore <| ReleaseNotes.parse releaseNotesLines) (fun e ->
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
                    checkPreRelease releaseNotesLines_case5 (Some "0A.is.legal") (None) ] ]
