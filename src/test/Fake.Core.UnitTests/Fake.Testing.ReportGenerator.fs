module Fake.Testing.ReportGeneratorTests

open System.IO
open Fake.Core
open Fake.Testing
open Expecto

let runCreateProcess setParams =
  let cp = 
    ["report1.xml"; "report2.xml"]
    |> Fake.Testing.ReportGenerator.createProcess (fun param -> 
         { setParams param with 
             ExePath = Path.Combine("reportgenerator", "ReportGenerator.exe") 
             TargetDir = "targetDir"})

  let file, args =
    match cp.Command with
    | RawCommand(file, args) -> file, args
    | _ -> failwithf "expected RawCommand"
    |> ArgumentHelper.checkIfMono
   
  let expectedPath = Path.Combine("reportgenerator", "ReportGenerator.exe")
  Expect.equal file expectedPath "Expected ReportGenerator.exe"
  expectedPath, (Command.RawCommand(file, Arguments.OfArgs args)).CommandLine

[<Tests>]
let tests =
  testList "Fake.Testing.ReportGenerator.Tests" [
    testCase "Test that new argument generation  with default parameters" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess id

      Expect.equal commandLine
        (sprintf "%s -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Html -verbosity:Verbose" expectedPath) "expected proper command line"

    testCase "Test that new argument generation with all parameters" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess (fun p ->
          { p with 
              ReportTypes = [ ReportGenerator.ReportType.Html
                              ReportGenerator.ReportType.MHtml ]
              SourceDirs = [ "source1"; "source2" ]
              HistoryDir = "history"
              Filters = [ "+a1*"; "-a2*" ]
              ClassFilters = [ "+c1*"; "-c2*" ]
              FileFilters = [ "+f1*"; "-f2*" ]
              Tag = Some "mytag" })

      Expect.equal commandLine
        (sprintf "%s -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Html;MHtml -sourcedirs:source1;source2 -historydir:history -assemblyfilters:+a1*;-a2* -classfilters:+c1*;-c2* -filefilters:+f1*;-f2* -tag:mytag -verbosity:Verbose" expectedPath) "expected proper command line"
  ]
