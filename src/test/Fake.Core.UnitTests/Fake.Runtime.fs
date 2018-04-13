module Fake.RuntimeTests

open Fake.Runtime
open Expecto
open Expecto.Flip

[<Tests>]
let tests = 
  testList "Fake.Runtime.Tests" [
    testCase "Test that we can properly find type names when the file name contains '.'" <| fun _ ->
      // Add test if everything works with __SOURCE_FILE__
      let name, parser =
          CompileRunner.nameParser
             "build.test1.test2_E294A5A65B9A06E0358F991A589AC7246FA6677BA99829862925EF343588E50D"
             "build.test1.test2.fsx"

      Expect.equal
        "Expected to have correct full type name"
        "<StartupCode$build-test1-test2_E294A5A65B9A06E0358F991A589AC7246FA6677BA99829862925EF343588E50D>.$Build.test1.test2$fsx"
        name
    testCase "Test that we can tokenize __SOURCE_FILE__" <| fun _ ->
      // Add test if everything works with __SOURCE_FILE__
      
      Expect.equal "." "" ""

    // Add test if everything works with #ifdefed #r "paket: line"
    testCase "Test that we find the correct references" <| fun _ ->
      let scriptText = """
#if BOOTSTRAP && DOTNETCORE
#r "paket:
nuget Fake.Core.SemVer prerelease //"
#endif
      """
      let interesting = Fake.Runtime.FSharpParser.findInterestingItems ["BOOTSTRAP"; "DOTNETCORE"; "FAKE"] "testfile.fsx" scriptText
      let expected =
        [Fake.Runtime.FSharpParser.InterestingItem.Reference (sprintf "paket:\nnuget Fake.Core.SemVer prerelease //") ]
      Expect.equal "Expected to find reference." expected interesting 
      
    // Add test if everything works with #ifdefed #r "paket: line"
    testCase "Test that we find the correct references without defines" <| fun _ ->
      let scriptText = """
#if BOOTSTRAP && DOTNETCORE
#r "paket:
nuget Fake.Core.SemVer prerelease //"
#endif
      """
      let interesting = Fake.Runtime.FSharpParser.findInterestingItems ["DOTNETCORE"; "FAKE"] "testfile.fsx" scriptText
      let expected = []
      Expect.equal "Expected to find reference." expected interesting 
  ]    