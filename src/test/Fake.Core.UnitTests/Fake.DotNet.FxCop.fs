module Fake.DotNet.FxCop

open System
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Expecto

let serializingObject = Object()

let testCases =
    if Environment.isWindows then
        [ testCase "Test that default arguments are processed as expected" <| fun _ ->
              let p = FxCop.Params.Create()
              let dummy = Guid.NewGuid().ToString()
              let args = FxCop.createArgs p [ dummy ]
              let pwd = Shell.pwd()
              Expect.isTrue (p.IncludeSummaryReport)
                  "A field should have non-default value for a bool"
              Expect.equal args [ "/c"
                                  "/f:" + dummy + ""
                                  "/o:" + pwd @@ "FXCopResults.html" + ""
                                  "/s"
                                  "/v" ] "The defaults should be simple"
          testCase "Test that default arguments are processed as expected (containing spaces)" <| fun _ ->
              let p = FxCop.Params.Create()
              let dummy = Guid.NewGuid().ToString() + " " + Guid.NewGuid().ToString()
              let proc = FxCop.composeCommandLine p [ dummy ]
              Expect.isTrue (p.IncludeSummaryReport)
                  "A field should have non-default value for a bool"
              let expected = [ p.ToolPath
                               "/c"
                               "\"/f:" + dummy + "\""
                               "\"/o:" + Shell.pwd() @@ "FXCopResults.html\""
                               "/s"
                               "/v" ] |> fun xs -> String.Join(" ", xs)
                
              Expect.equal proc.CommandLine expected "The defaults should be simple"                          
          testCase "Test that all arguments are processed as expected" <| fun _ ->
              let p =
                  { FxCop.Params.Create() with DependencyDirectories =
                                                   [ Guid.NewGuid().ToString()
                                                     Guid.NewGuid().ToString() ]
                                               ImportFiles =
                                                   [ Guid.NewGuid().ToString()
                                                     Guid.NewGuid().ToString() ]
                                               RuleLibraries =
                                                   [ Guid.NewGuid().ToString()
                                                     Guid.NewGuid().ToString() ]
                                               Rules =
                                                   [ Guid.NewGuid().ToString()
                                                     Guid.NewGuid().ToString() ]
                                               CustomRuleset = Guid.NewGuid().ToString()
                                               ConsoleXslFileName =
                                                   Guid.NewGuid().ToString()
                                               ReportFileName = Guid.NewGuid().ToString()
                                               OutputXslFileName =
                                                   Guid.NewGuid().ToString()
                                               PlatformDirectory =
                                                   Guid.NewGuid().ToString()
                                               ProjectFile = Guid.NewGuid().ToString()
                                               Types =
                                                   [ Guid.NewGuid().ToString()
                                                     Guid.NewGuid().ToString() ]
                                               WorkingDirectory =
                                                   Guid.NewGuid().ToString()
                                               CustomDictionary =
                                                   Guid.NewGuid().ToString()
                                               ApplyOutXsl = true
                                               ToolPath = Guid.NewGuid().ToString() }

              let dummy = Guid.NewGuid().ToString()
              let args = FxCop.createArgs p [ dummy ]
              let wrap s a =
                s + a
                //s + "\"" + a + "\""

              let expected =
                  [ "/aXsl"
                    "/c"
                    wrap "/cXsl:" p.ConsoleXslFileName
                    wrap "/d:" (p.DependencyDirectories |> Seq.head)
                    wrap "/d:" (p.DependencyDirectories |> Seq.last)
                    wrap "/f:" dummy
                    wrap "/i:" (p.ImportFiles |> Seq.head)
                    wrap "/i:" (p.ImportFiles |> Seq.last)
                    wrap "/o:" p.ReportFileName
                    wrap "/oXsl:" p.OutputXslFileName
                    wrap "/plat:" p.PlatformDirectory
                    wrap "/p:" p.ProjectFile
                    wrap "/ruleset:=" p.CustomRuleset
                    wrap "/r:" (p.ToolPath @@ "Rules" @@ (p.RuleLibraries |> Seq.head))
                    wrap "/r:" (p.ToolPath @@ "Rules" @@ (p.RuleLibraries |> Seq.last))
                    "/rid:" + (p.Rules |> Seq.head)
                    "/rid:" + (p.Rules |> Seq.last)
                    "/s"
                    "/t:" + (p.Types |> Seq.head) + "," + (p.Types |> Seq.last)
                    "/v"
                    wrap "/dic:" p.CustomDictionary ]
              Expect.equal args expected "The Xsl should be applied"
          testCase "Test that generated code should be ignored" <| fun _ ->
              let p1 = { FxCop.Params.Create() with IgnoreGeneratedCode = true }
              let dummy = Guid.NewGuid().ToString()
              let args = FxCop.createArgs p1 [ dummy ]
              Expect.equal args [ "/c"
                                  "/f:" + dummy + ""
                                  "/o:" + Shell.pwd() @@ "FXCopResults.html" + ""
                                  "/ignoregeneratedcode"
                                  "/s"
                                  "/v" ] "Generated code should be ignored"
          testCase "Test that console output can be switched off" <| fun _ ->
              let p2 = { FxCop.Params.Create() with DirectOutputToConsole = false }
              let dummy = Guid.NewGuid().ToString()
              let args = FxCop.createArgs p2 [ dummy ]
              Expect.equal args [ "/f:" + dummy + ""
                                  "/o:" + Shell.pwd() @@ "FXCopResults.html" + ""
                                  "/s"
                                  "/v" ] "No output to console expected"
          testCase "Test that summary reporting can be switched off" <| fun _ ->
              let p3 = { FxCop.Params.Create() with IncludeSummaryReport = false }
              let dummy = Guid.NewGuid().ToString()
              let args = FxCop.createArgs p3 [ dummy ]
              Expect.equal args [ "/c"
                                  "/f:" + dummy + ""
                                  "/o:" + Shell.pwd() @@ "FXCopResults.html" + ""
                                  "/v" ] "No summary expected"
          testCase "Test project file update can be enabled" <| fun _ ->
              let p4 = { FxCop.Params.Create() with SaveResultsInProjectFile = true }
              let dummy = Guid.NewGuid().ToString()
              let args = FxCop.createArgs p4 [ dummy ]
              Expect.equal args [ "/c"
                                  "/f:" + dummy + ""
                                  "/o:" + Shell.pwd() @@ "FXCopResults.html" + ""
                                  "/s"
                                  "/u"
                                  "/v" ] "results should be in project file"
          testCase "Test that output can be forced" <| fun _ ->
              let p5 = { FxCop.Params.Create() with ForceOutput = true }
              let dummy = Guid.NewGuid().ToString()
              let args = FxCop.createArgs p5 [ dummy ]
              Expect.equal args [ "/c"
                                  "/fo"
                                  "/f:" + dummy + ""
                                  "/o:" + Shell.pwd() @@ "FXCopResults.html" + ""
                                  "/s"
                                  "/v" ] "Output should be forced"
          testCase "Test that Xsl is defaulted" <| fun _ ->
              let p0 = { FxCop.Params.Create() with ApplyOutXsl = true }
              let dummy = Guid.NewGuid().ToString()
              let args = FxCop.createArgs p0 [ dummy ]
              Expect.equal args [ "/aXsl"
                                  "/c"
                                  "/f:" + dummy + ""
                                  "/o:" + Shell.pwd() @@ "FXCopResults.html" + ""

                                  "/oXsl:" + p0.ToolPath @@ "Xml" @@ "FxCopReport.xsl" + ""
                                  "/s"
                                  "/v" ] "Xsl should be defaulted"

          testCase "Test process is created" <| fun _ ->
              let dummy = Guid.NewGuid().ToString()
              let p = { FxCop.Params.Create() with ToolPath = dummy }
              let args = [ Guid.NewGuid().ToString() ]
              let proc = FxCop.createProcess p args
              Expect.equal proc.CommandLine (dummy + " " + String.Join(" ", args))
                  "tool should match"
              Expect.equal proc.WorkingDirectory (Some <| Shell.pwd())
                  "WorkingDirectory should default"

          testCase "Test process is created with working directory" <| fun _ ->
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()

              let p =
                  { FxCop.Params.Create() with ToolPath = dummy
                                               WorkingDirectory = dummy2 }

              let args = [ Guid.NewGuid().ToString() ]
              let proc = FxCop.createProcess p args
              Expect.equal proc.CommandLine (dummy + " " + String.Join(" ", args))
                  "tool should match"
              Expect.equal proc.WorkingDirectory (Some dummy2)
                  "WorkingDirectory should match input"

          testCase "Test full command line is created" <| fun _ ->
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()

              let p =
                  { FxCop.Params.Create() with ToolPath = dummy
                                               ReportFileName = dummy2 }

              let assemblies = [ Guid.NewGuid().ToString() ]
              let proc = FxCop.composeCommandLine p assemblies
              let expected =
                  sprintf """%s /c /f:%s /o:%s /s /v""" dummy
                      (Seq.head assemblies) dummy2
              Expect.equal proc.CommandLine expected "composed command line should match"
              Expect.equal proc.WorkingDirectory (Some <| Shell.pwd())
                  "WorkingDirectory should default"
          testCase "Test errors are read as expected" <| fun _ ->
              let dummy = Guid.NewGuid().ToString()

              let data =
                  [ ("string(count(//Issue[@Level='CriticalError']))", 23)
                    ("string(count(//Issue[@Level='Error']))", 42)
                    ("string(count(//Issue[@Level='CriticalWarning']))", 17)
                    ("string(count(//Issue[@Level='Warning']))", 5) ]
                  |> Map.ofList

              let XmlMock failOnError xmlFileName nameSpace prefix xPath =
                  Expect.isFalse failOnError "no fail on error"
                  Expect.equal xmlFileName dummy "file name should pass through"
                  Expect.isEmpty nameSpace "no namespace wanted"
                  Expect.isEmpty prefix "no prefix wanted"
                  Expect.isTrue (data.ContainsKey xPath) "key should be in map"
                  Map.find xPath data
              lock serializingObject (fun _ ->
                  let saved = FxCop.XmlReadInt
                  try
                      FxCop.XmlReadInt <- XmlMock
                      Expect.equal (FxCop.checkForErrors dummy) (23, 42, 17, 5)
                          "Results should match"
                  finally
                      FxCop.XmlReadInt <- saved)

          testCase "Tool failure is handled" <| fun _ ->
              let dummy = Guid.NewGuid().ToString()

              let p =
                  { FxCop.Params.Create() with ReportFileName = dummy
                                               FailOnError = FxCop.ErrorLevel.ToolError }

              let result =
                  { ExitCode = 1
                    Result = () }

              let XmlMock _ _ _ _ _ =
                  Expect.isTrue false "should not be called"
                  0

              Expect.throwsC (fun () ->
                  lock serializingObject (fun _ ->
                      let saved = FxCop.XmlReadInt
                      try
                          FxCop.XmlReadInt <- XmlMock
                          FxCop.failAsrequired p result
                      finally
                          FxCop.XmlReadInt <- saved))
                  (fun ex ->
                  Expect.equal ex.Message "FxCop test failed."
                      "should have simple failure")
          testCase "Analysis failure is handled" <| fun _ ->
              let crossproduct l1 l2 =
                  seq {
                      for el1 in l1 do
                          for el2 in l2 do
                              yield el1, el2
                  }

              let dummy = Guid.NewGuid().ToString()
              let levels = { 0..5 }

              let maps =
                  [| [ ("string(count(//Issue[@Level='CriticalError']))", 1)
                       ("string(count(//Issue[@Level='Error']))", 2)
                       ("string(count(//Issue[@Level='CriticalWarning']))", 3)
                       ("string(count(//Issue[@Level='Warning']))", 4) ]
                     |> Map.ofList
                     [ ("string(count(//Issue[@Level='CriticalError']))", 0)
                       ("string(count(//Issue[@Level='Error']))", 1)
                       ("string(count(//Issue[@Level='CriticalWarning']))", 3)
                       ("string(count(//Issue[@Level='Warning']))", 4) ]
                     |> Map.ofList
                     [ ("string(count(//Issue[@Level='CriticalError']))", 0)
                       ("string(count(//Issue[@Level='Error']))", 0)
                       ("string(count(//Issue[@Level='CriticalWarning']))", 1)
                       ("string(count(//Issue[@Level='Warning']))", 4) ]
                     |> Map.ofList
                     [ ("string(count(//Issue[@Level='CriticalError']))", 0)
                       ("string(count(//Issue[@Level='Error']))", 0)
                       ("string(count(//Issue[@Level='CriticalWarning']))", 0)
                       ("string(count(//Issue[@Level='Warning']))", 1) ]
                     |> Map.ofList
                     [ ("string(count(//Issue[@Level='CriticalError']))", 0)
                       ("string(count(//Issue[@Level='Error']))", 0)
                       ("string(count(//Issue[@Level='CriticalWarning']))", 0)
                       ("string(count(//Issue[@Level='Warning']))", 0) ]
                     |> Map.ofList |]

              let mapIndexes = { 0..4 }
              let messages =
                  [| "FxCop found 1 critical errors."; "FxCop found 1 errors.";
                     "FxCop found 1 critical warnings."; "FxCop found 1 warnings.";
                     String.Empty |]
              crossproduct levels mapIndexes
              |> Seq.iter (fun (level, mapIndex) ->
                     let p =
                         { FxCop.Params.Create() with ReportFileName = dummy
                                                      FailOnError =
                                                          enum<FxCop.ErrorLevel> level }

                     let result =
                         { ExitCode = 0
                           Result = () }

                     let XmlMock _ _ _ _ xPath = Map.find xPath maps.[mapIndex]

                     let op() =
                         lock serializingObject (fun _ ->
                             let saved = FxCop.XmlReadInt
                             try
                                 FxCop.XmlReadInt <- XmlMock
                                 FxCop.failAsrequired p result
                             finally
                                 FxCop.XmlReadInt <- saved)
                     if level >= int FxCop.ErrorLevel.CriticalError
                        && level >= mapIndex + 2 then
                         Expect.throwsC op
                             (fun ex ->
                             Expect.equal ex.Message messages.[mapIndex]
                                 (sprintf "should have expected failure level=%d index=%d"
                                      level mapIndex))
                     else
                         try
                             op()
                         with x ->
                             printfn "Unexpected failure %A level=%d index=%d" x level
                                 mapIndex
                             reraise()) ]
    else
        [ testCase "Test failure on non-Windows platforms" <| fun _ ->
              let p = FxCop.Params.Create()
              Expect.throwsC (fun () -> FxCop.run p [])
                  (fun ex ->
                  Expect.equal (ex.GetType()) typeof<NotSupportedException>
                      "Exception type should be as expected"
                  Expect.equal ex.Message "FxCop is currently not supported on mono"
                      "Exception message should be as expected") ]

[<Tests>]
let tests = testList "Fake.DotNet.FxCop.Tests" testCases