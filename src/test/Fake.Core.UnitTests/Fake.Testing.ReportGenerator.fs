module Fake.Testing.ReportGeneratorTests

open System.IO
open Fake.Core
open Fake.Testing
open Expecto

let rawCreateProcess setParams =
  ["report1.xml"; "report2.xml"]
  |> Fake.Testing.ReportGenerator.createProcess (fun param ->
       { setParams param with
           ExePath = Path.Combine("reportgenerator", "ReportGenerator.exe")
           TargetDir = "targetDir"})

let runCreateProcess setParams =
  let cp = rawCreateProcess setParams

  let file, args =
    match cp.Command with
    | RawCommand(file, args) -> file, args
    | _ -> failwithf "expected RawCommand"
    |> ArgumentHelper.checkIfMono

  let expectedPath = Path.Combine("reportgenerator", "ReportGenerator.exe")
  Expect.equal file expectedPath "Expected ReportGenerator.exe"

  expectedPath, (RawCommand(file, args)).CommandLine

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

    testCase "Test that ReportType Cobertura works" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess (fun p ->
          { p with
              ReportTypes = [ ReportGenerator.ReportType.Cobertura ] })

      Expect.equal commandLine
        (sprintf "%s -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Cobertura -verbosity:Verbose" expectedPath) "expected proper command line"

    testCase "Test that Framework can override" <| fun _ ->
      let path = "another/path/for/tool.exe"
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType = CreateProcess.ToolType.Framework (Some path) })
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        |> ArgumentHelper.checkIfMono

      Expect.equal file path "Expected tool.exe"
      Expect.equal (RawCommand(file, args)).CommandLine
         (sprintf "%s -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Html -verbosity:Verbose" path) "expected proper command line"

    testCase "Test that Global does override" <| fun _ ->
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType = CreateProcess.ToolType.Global })
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"

      Expect.equal file "reportgenerator" "Expected reportgenerator"
      Expect.equal (RawCommand(file, args)).CommandLine
         (sprintf "%s -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Html -verbosity:Verbose" "reportgenerator") "expected proper command line"

    testCase "Test that DotNet does default tool" <| fun _ ->
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType = CreateProcess.ToolType.DotNet (CreateProcess.DotNetTool.Create()) })
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"

      Expect.equal file "dotnet" "Expected reportgenerator"
      Expect.equal (RawCommand(file, args)).CommandLine
       (sprintf "%s reportgenerator -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Html -verbosity:Verbose" file) "expected proper command line"

    testCase "Test that DotNet can override tool" <| fun _ ->
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType = CreateProcess.ToolType.DotNet { Options = id; Tool = Some "cli-tool"} })
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"

      Expect.equal file "dotnet" "Expected reportgenerator"
      Expect.equal (RawCommand(file, args)).CommandLine
       (sprintf "%s cli-tool -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Html -verbosity:Verbose" file) "expected proper command line"

    testCase "Test that DotNet can override dotnet" <| fun _ ->
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType = CreateProcess.ToolType.DotNet { Options = (fun o -> {o with DotNetCliPath = "some/dotnet/path"}); Tool = None } })
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"

      Expect.equal file "some/dotnet/path" "Expected reportgenerator"
      Expect.equal (RawCommand(file, args)).CommandLine
       (sprintf "%s reportgenerator -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Html -verbosity:Verbose" file) "expected proper command line"

    testCase "Test that DotNet can override both" <| fun _ ->
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType = CreateProcess.ToolType.DotNet { Options = (fun o -> {o with DotNetCliPath = "some/dotnet/path"}); Tool = Some "cli-tool"}  })
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"

      Expect.equal file "some/dotnet/path" "Expected reportgenerator"
      Expect.equal (RawCommand(file, args)).CommandLine
       (sprintf "%s cli-tool -reports:report1.xml;report2.xml -targetdir:targetDir -reporttypes:Html -verbosity:Verbose" file) "expected proper command line"

  ]
