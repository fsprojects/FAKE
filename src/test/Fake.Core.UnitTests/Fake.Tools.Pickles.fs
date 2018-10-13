module Fake.Tools.Pickles.Tests

open System.IO
open Fake.Core
open Fake.Testing
open Fake.Tools
open Expecto

let runCreateProcess setParams =
  let cp =
    Pickles.createProcess 
      (fun param -> 
        { setParams param with
            ToolPath = Path.Combine("pickles", "pickles.exe") } )
  
  let file, args =
    match cp.Command with
    | RawCommand(file, args) -> file, args
    | _ -> failwithf "expected RawCommand"
    |> ArgumentHelper.checkIfMono
    
  let expectedPath = Path.Combine("pickles", "pickles.exe")
  Expect.equal file expectedPath "Expected pickles.exe"
  
  expectedPath, (RawCommand(file, args)).CommandLine


[<Tests>]
let tests =
  testList "Fake.Tools.Pickles.Tests" [
    testCase "Test that new argument generation works with minimal parameters" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess id

      Expect.equal 
        commandLine 
        (sprintf "%s --df dhtml" expectedPath)
        "expected proper command line"

    testCase "Test that new argument generation works with all parameters" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess 
          (fun param ->
            { param with 
                FeatureDirectory = "features"
                OutputDirectory = "output"
                SystemUnderTestName = Some "sut"
                SystemUnderTestVersion = Some "sutver"
                FeatureFileLanguage = Some "de"
                OutputFileFormat = Pickles.DocumentationFormat.DHTML
                LinkedTestResultFiles = [ "TestResult1.xml"; "TestResult2.xml" ] 
                TestResultsFormat = Pickles.TestResultsFormat.XUnit2
                IncludeExperimentalFeatures = Some true
                EnableComments = Some false
                ExcludeTags = [ "et1"; "et2" ]
                HideTags = [ "ht1"; "ht2" ] })

      Expect.equal 
        commandLine 
        (sprintf "%s -f features -o output --sn sut --sv sutver --l de --df dhtml --trfmt xunit2 --lr TestResult1.xml;TestResult2.xml --exp --enableComments=false --et et1;et2 --ht ht1;ht2" expectedPath) "expected proper command line"

    testCase "Test that output file format is ommitted if it is HTML" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess 
          (fun param ->
            { param with 
                OutputFileFormat = Pickles.DocumentationFormat.HTML })

      Expect.equal 
        commandLine 
        (sprintf "%s " expectedPath)
        "expected proper command line"
  ]
