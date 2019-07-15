module Fake.Core.ReleaseNotesTests

open Fake.Core
open Expecto

[<Tests>]
let tests = 
  testList "Fake.Core.ReleaseNotes.Tests" [
    testCase "Test that we can handle same day releases" <| fun _ ->
      let releaseNotesLines = [
        "# Release Notes"
        ""
        "## 5.15.3 - 2019-07-03"
        ""
        "* BUGFIX: Disable fast restore for MSBuild version < 15.8 - https://github.com/fsprojects/Paket/pull/3611"
        ""
        "## 5.15.2 - 2019-07-03"
        ""
        "* BUGFIX: Fast Restore (bugfix from paket) - https://github.com/fsprojects/Paket/pull/3608"
        ""
      ]
      let releaseNotes = ReleaseNotes.parse releaseNotesLines
      
      let (expected:ReleaseNotes.ReleaseNotes) = 
        // For historic reasons notes get appended a "."
        { AssemblyVersion = "5.15.3"; NugetVersion = "5.15.3"; SemVer = SemVer.parse "5.15.3"; Date = Some (System.DateTime(2019,07,3)); Notes = ["BUGFIX: Disable fast restore for MSBuild version < 15.8 - https://github.com/fsprojects/Paket/pull/3611"] }
      
      Expect.equal releaseNotes expected "Simple parse failure"
    
    testCase "Test that we can parse simple release notes" <| fun _ ->
      let releaseNotesLines = [
        "* 1.0.0 - Initial version"
        "* 1.1.0 - First change"
      ]
      let releaseNotes = ReleaseNotes.parse releaseNotesLines
      
      let (expected:ReleaseNotes.ReleaseNotes) = 
        // For historic reasons notes get appended a "."
        { AssemblyVersion = "1.1.0"; NugetVersion = "1.1.0"; SemVer = SemVer.parse "1.1.0"; Date = None; Notes = ["First change."] }
      
      Expect.equal releaseNotes expected "Simple parse failure"

    testCase "Test that we can parse simple release notes (reversed)" <| fun _ ->
      let releaseNotesLines = [
        "* 1.1.0 - First change"
        "* 1.0.0 - Initial version"
      ]
      let releaseNotes = ReleaseNotes.parse releaseNotesLines
      
      let (expected:ReleaseNotes.ReleaseNotes) = 
        // For historic reasons notes get appended a "."
        { AssemblyVersion = "1.1.0"; NugetVersion = "1.1.0"; SemVer = SemVer.parse "1.1.0"; Date = None; Notes = ["First change."] }
      
      Expect.equal releaseNotes expected "Simple parse failure"

    testCase "Test that we can parse complex release notes" <| fun _ ->
      let releaseNotesLines = [
        "## 1.0.0 - Released on 2017-02-03"
        "- Some change 1"
        "- Some change 2"
        ""
        "## 1.1.0 - Released on 2017-04-12"
        "- Some change 3"
        "- Some change 4"
      ]
      let releaseNotes = ReleaseNotes.parse releaseNotesLines
      
      let (expected:ReleaseNotes.ReleaseNotes) = 
         { AssemblyVersion = "1.1.0"; NugetVersion = "1.1.0"; SemVer = SemVer.parse "1.1.0"; Date = Some (System.DateTime(2017,04,12)); Notes = ["- Some change 3"; "- Some change 4"] }
      
      Expect.equal releaseNotes expected "Simple parse failure"

    testCase "Test that we can parse complex release notes (reversed)" <| fun _ ->
      let releaseNotesLines = [
        "## 1.1.0 - Released on 2017-04-12"
        ""
        "- Some change 3"
        "- Some change 4"
        ""
        "## 1.0.0 - Released on 2017-02-03"
        "- Some change 1"
        "- Some change 2"
      ]
      let releaseNotes = ReleaseNotes.parse releaseNotesLines
      
      let (expected:ReleaseNotes.ReleaseNotes) = 
         { AssemblyVersion = "1.1.0"; NugetVersion = "1.1.0"; SemVer = SemVer.parse "1.1.0"; Date = Some (System.DateTime(2017,04,12)); Notes = ["- Some change 3"; "- Some change 4"] }
      
      Expect.equal releaseNotes expected "Simple parse failure"

    testCase "Test that we can parse complex release notes with header" <| fun _ ->
      let releaseNotesLines = [
        "# Global header"
        ""
        "## 1.1.0 - Released on 2017-04-12"
        "- Some change 3"
        "- Some change 4"
        ""
        "## 1.0.0 - Released on 2017-02-03"
        "- Some change 1"
        "- Some change 2"
      ]
      let releaseNotes = ReleaseNotes.parse releaseNotesLines
      
      let (expected:ReleaseNotes.ReleaseNotes) = 
         { AssemblyVersion = "1.1.0"; NugetVersion = "1.1.0"; SemVer = SemVer.parse "1.1.0"; Date = Some (System.DateTime(2017,04,12)); Notes = ["- Some change 3"; "- Some change 4"] }
      
      Expect.equal releaseNotes expected "Simple parse failure"

    testCase "Test that we provide proper error #2085" <| fun _ ->
      let releaseNotesLines = [
        "# 1.3.7"
        ""
        "* Bugfixes and public release"
        ""
        "# 1.3.2"
        ""
        "* Fix various bugs in the FAKE runner."
        ""
      ]
      Expect.throwsC
        (fun () -> ignore <| ReleaseNotes.parse releaseNotesLines)
        (fun e ->
          Expect.stringContains e.Message "files containing only top level headers are not allowed" "Expected nice error message")
  ]    
