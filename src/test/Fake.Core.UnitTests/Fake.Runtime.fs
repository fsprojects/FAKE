module Fake.RuntimeTests

open Fake.Runtime
open Expecto
open Expecto.Flip

[<Tests>]
let tests = 
  testList "Fake.Runtime.Tests" [
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