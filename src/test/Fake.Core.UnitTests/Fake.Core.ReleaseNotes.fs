module Fake.Core.ReleaseNotesTests

open Fake.Core
open Expecto


[<Tests>]
let tests = 
  testList "Fake.Core.ReleaseNotes.Tests" [
    testCase "Test that we can parse simple release notes" <| fun _ ->
      let releaseNotesLines = [
        "* 1.0.0 - Initial version"
        "* 1.1.0 - First change"
      ]
      let releaseNotes = ReleaseNotes.parse releaseNotesLines
      
      let (expected:ReleaseNotes.ReleaseNotes) = 
        // For historic reasons notes get appended a "."
        { AssemblyVersion = "1.1.0"; NugetVersion = "1.1.0"; SemVer = SemVer.parse "1.1.0"; Date = None; Notes = ["First change."] }
      
      Expect.equal expected releaseNotes "Simple parse failure"

    testCase "Test that we can parse simple release notes (reversed)" <| fun _ ->
      let releaseNotesLines = [
        "* 1.1.0 - First change"
        "* 1.0.0 - Initial version"
      ]
      let releaseNotes = ReleaseNotes.parse releaseNotesLines
      
      let (expected:ReleaseNotes.ReleaseNotes) = 
        // For historic reasons notes get appended a "."
        { AssemblyVersion = "1.1.0"; NugetVersion = "1.1.0"; SemVer = SemVer.parse "1.1.0"; Date = None; Notes = ["First change."] }
      
      Expect.equal expected releaseNotes "Simple parse failure"

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
      
      Expect.equal expected releaseNotes "Simple parse failure"

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
      
      Expect.equal expected releaseNotes "Simple parse failure"

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
      
      Expect.equal expected releaseNotes "Simple parse failure"
  ]    
