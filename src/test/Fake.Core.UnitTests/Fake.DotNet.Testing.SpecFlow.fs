module Fake.DotNet.Testing.SpecFlowTests

open System.IO
open Fake.Core
open Fake.DotNet.Testing
open Fake.Testing
open Expecto

let runCreateProcess setParams =
  let _, cp = 
    "projectfile.csproj"
    |> SpecFlowNext.createProcess (fun param -> 
         { setParams param with ToolPath = "specflow" })

  let file, args =
    match cp.Command with
    | RawCommand(file, args) -> file, args
    | _ -> failwithf "expected RawCommand"
    |> ArgumentHelper.checkIfMono
   
  let expectedPath = Path.Combine("specflow", "specflow.exe")
  Expect.equal file expectedPath "Expected specflow.exe"

  expectedPath, (RawCommand(file, args)).CommandLine

[<Tests>]
let tests =
  testList "Fake.DotNet.Testing.SpecFlow.Tests" [
    testCase "Test that new argument generation works" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess (fun param -> 
          { param with 
              SubCommand = SpecFlowNext.MsTestExecutionReport })

      Expect.equal commandLine 
        (sprintf "%s MsTestExecutionReport --ProjectFile projectfile.csproj" expectedPath) "expected proper command line"

    testCase "Test that argument generation fails with exception if project file is not given" <| fun _ ->
      Expect.throws 
        (fun _ -> SpecFlowNext.createProcess id "" |> ignore)
        "expected to throw an exception"

    testCase "Test that argument generation works with default arguments" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess id

      Expect.equal commandLine 
        (sprintf "%s GenerateAll --ProjectFile projectfile.csproj" expectedPath) "expected proper command line"
    
    testCase "Test that argument generation works with all arguments set" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess (fun param -> 
          { param with 
              SubCommand = SpecFlowNext.NUnitExecutionReport
              BinFolder = Some "bin/debug"
              OutputFile = Some "output.html"
              XmlTestResultFile = Some "testresult.xml"
              TestOutputFile = Some "testoutput.txt"
              FeatureLanguage = Some "de-DE"
              Verbose = true
              ForceRegeneration = true
              XsltFile = Some "transform.xsl" })

      Expect.equal commandLine 
        (sprintf "%s NUnitExecutionReport --ProjectFile projectfile.csproj --binFolder bin/debug --OutputFile output.html --xmlTestResult testresult.xml --testOutput testoutput.txt --FeatureLanguage de-DE --verbose --force --XsltFile transform.xsl" expectedPath ) "expected proper command line"
  ]
