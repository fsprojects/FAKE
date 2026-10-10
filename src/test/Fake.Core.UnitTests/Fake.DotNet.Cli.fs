module Fake.DotNet.CliTests

open Fake.Core
open Fake.DotNet
open Expecto

[<Tests>]
let tests =
    testList
        "Fake.DotNet.Cli.Tests"
        [ testCase "Test that we can use Process-Helpers on Cli Parameters"
          <| fun _ ->
              let cli =
                  DotNet.Options.Create() |> Process.setEnvironmentVariable "Somevar" "someval"

              Expect.equal cli.Environment.["Somevar"] "someval" "Retrieving the correct environment variable failed."

          testCase "Test that the default dotnet nuget push arguments returns empty string"
          <| fun _ ->
              let cli =
                  DotNet.NuGetPushOptions.Create().PushParams
                  |> DotNet.buildNugetPushArgs
                  |> Args.toWindowsCommandLine

              Expect.isEmpty cli "Empty push args."

          testCase "Test that the dotnet nuget push arguments with all params set returns correct string"
          <| fun _ ->
              let param =
                  { DotNet.NuGetPushOptions.Create().PushParams with
                      DisableBuffering = true
                      ApiKey = Some "abc123"
                      NoSymbols = true
                      NoServiceEndpoint = true
                      Source = Some "MyNuGetSource"
                      SymbolApiKey = Some "MySymbolApiKey"
                      SymbolSource = Some "MySymbolSource"
                      Timeout = Some <| System.TimeSpan.FromMinutes 5.0
                      PushTrials = 5 }

              let cli = param |> DotNet.buildNugetPushArgs |> Args.toWindowsCommandLine

              let expected =
                  "--disable-buffering --api-key abc123 --no-symbols --no-service-endpoint --source MyNuGetSource --symbol-api-key MySymbolApiKey --symbol-source MySymbolSource --timeout 300"

              Expect.equal cli expected "Push args generated correctly."

          testCase "Test that the dotnet publish self-contained works as expected"
          <| fun _ ->
              let param = { DotNet.PublishOptions.Create() with SelfContained = Some false }
              let cli = param |> DotNet.buildPublishArgs |> Args.toWindowsCommandLine

              let expected = "--configuration Release --self-contained=false"

              Expect.equal cli expected "Push args generated correctly."

          testCase "Test that the dotnet publish force works as expected"
          <| fun _ ->
              let param = { DotNet.PublishOptions.Create() with Force = Some true }
              let cli = param |> DotNet.buildPublishArgs |> Args.toWindowsCommandLine

              let expected = "--configuration Release --force"

              Expect.equal cli expected "Push args generated correctly."

          testCase "Test that the dotnet publish manifest works as expected"
          <| fun _ ->
              let param =
                  { DotNet.PublishOptions.Create() with Manifest = Some [ "Path1"; "Path2" ] }

              let cli = param |> DotNet.buildPublishArgs |> Args.toWindowsCommandLine

              let expected = "--configuration Release --manifest Path1 --manifest Path2"

              Expect.equal cli expected "Push args generated correctly."

          testCase "Test that the dotnet new command works as expected"
          <| fun _ ->
              let param =
                  { DotNet.NewOptions.Create() with
                      DryRun = true
                      Force = true
                      Language = DotNet.NewLanguage.FSharp
                      Name = Some("my-awesome-project")
                      NoUpdateCheck = true
                      Output = Some("/path/to/code") }

              let cli = param |> DotNet.buildNewArgs |> Args.toWindowsCommandLine

              let expected =
                  "--dry-run --force --language F# --name my-awesome-project --no-update-check --output /path/to/code"

              Expect.equal cli expected "New args generated correctly."

          testCase "Test that the dotnet new command works as expected with spaces in arguments"
          <| fun _ ->
              let param =
                  { DotNet.NewOptions.Create() with
                      DryRun = true
                      Force = true
                      Language = DotNet.NewLanguage.FSharp
                      Name = Some("my awesome project")
                      NoUpdateCheck = true
                      Output = Some("/path to/code") }

              let cli = param |> DotNet.buildNewArgs |> Args.toWindowsCommandLine

              let expected =
                  "--dry-run --force --language F# --name \"my awesome project\" --no-update-check --output \"/path to/code\""

              Expect.equal cli expected "New args generated correctly."

          testCase "Test that the dotnet new --install command works as expected"
          <| fun _ ->
              let param =
                  { DotNet.TemplateInstallOptions.Create("my-awesome-template") with
                      NugetSource = Some("C:\\path\\to\\tool") }

              let cli = param |> DotNet.buildTemplateInstallArgs |> Args.toWindowsCommandLine

              let expected = "--install my-awesome-template --nuget-source \"C:\\path\\to\\tool\""

              Expect.equal cli expected "New --install args generated correctly."

          testCase "Test that the dotnet new --uninstall command works as expected"
          <| fun _ ->
              let param = DotNet.TemplateUninstallOptions.Create("my-awesome-template")
              let cli = param |> DotNet.buildTemplateUninstallArgs |> Args.toWindowsCommandLine

              let expected = "--uninstall my-awesome-template"

              Expect.equal cli expected "New --uninstall args generated correctly."

          testCase "Test buildAfterArgs with no after args"
          <| fun _ ->
              let expected = "hello"
              let cli = DotNet.buildAfterArgs [ "hello" ] None |> Args.toWindowsCommandLine

              Expect.equal cli expected "Empty after args."

          testCase "Test buildAfterArgs with after args"
          <| fun _ ->
              let expected = "hello -- lol=foo"

              let cli =
                  DotNet.buildAfterArgs [ "hello" ] (Some "lol=foo") |> Args.toWindowsCommandLine

              Expect.equal cli expected "Empty after args."

          testCase "Test that dotnet test for Microsoft.Testing.Platform passes no arguments by default"
          <| fun _ ->
              let args =
                  DotNet.buildMTPTestArgs DotNet.MTPTestTarget.CurrentDirectory (DotNet.MTPTestOptions.Create())

              Expect.isEmpty args "The project or solution of the working directory needs no arguments."

          testCase "Test that dotnet test for Microsoft.Testing.Platform names the project with an option"
          <| fun _ ->
              let args =
                  DotNet.buildMTPTestArgs
                      (DotNet.MTPTestTarget.Project "tests/My.Tests/My.Tests.fsproj")
                      (DotNet.MTPTestOptions.Create())

              Expect.equal
                  args
                  [ "--project"; "tests/My.Tests/My.Tests.fsproj" ]
                  "The project is not the first, unnamed argument."

          testCase "Test that dotnet test for Microsoft.Testing.Platform names the solution with an option"
          <| fun _ ->
              let args =
                  DotNet.buildMTPTestArgs (DotNet.MTPTestTarget.Solution "My.slnx") (DotNet.MTPTestOptions.Create())

              Expect.equal args [ "--solution"; "My.slnx" ] "The solution has its own option."

          testCase "Test that dotnet test for Microsoft.Testing.Platform selects test modules"
          <| fun _ ->
              let expression = "**/bin/Release/net10.0/*.Tests.dll"

              let withoutRoot =
                  DotNet.buildMTPTestArgs
                      (DotNet.MTPTestTarget.TestModules(expression, None))
                      (DotNet.MTPTestOptions.Create())

              let withRoot =
                  DotNet.buildMTPTestArgs
                      (DotNet.MTPTestTarget.TestModules(expression, Some "artifacts"))
                      (DotNet.MTPTestOptions.Create())

              Expect.equal withoutRoot [ "--test-modules"; expression ] "Test modules without a root directory."

              Expect.equal
                  withRoot
                  [ "--test-modules"; expression; "--root-directory"; "artifacts" ]
                  "Test modules with a root directory."

          testCase "Test that dotnet test for Microsoft.Testing.Platform collects coverage of a built project"
          <| fun _ ->
              let param =
                  { DotNet.MTPTestOptions.Create() with
                      NoBuild = true
                      Configuration = Some DotNet.BuildConfiguration.Release
                      ResultsDirectory = Some "TestResults"
                      CodeCoverage =
                          Some
                              { DotNet.MTPCodeCoverageOptions.Create() with
                                  OutputFormat = Some DotNet.MTPCoverageFormat.Cobertura } }

              let args =
                  DotNet.buildMTPTestArgs (DotNet.MTPTestTarget.Project "tests/My.Tests/My.Tests.fsproj") param

              let expected =
                  [ "--project"
                    "tests/My.Tests/My.Tests.fsproj"
                    "--configuration"
                    "Release"
                    "--no-build"
                    "--results-directory"
                    "TestResults"
                    "--"
                    "--coverage"
                    "--coverage-output-format"
                    "cobertura" ]

              Expect.equal args expected "The options of the coverage extension follow the options of dotnet test."

          testCase "Test that dotnet test for Microsoft.Testing.Platform lists the tests of a built project"
          <| fun _ ->
              let param =
                  { DotNet.MTPTestOptions.Create() with
                      NoBuild = true
                      Configuration = Some DotNet.BuildConfiguration.Debug
                      ListTests = true }

              let args =
                  DotNet.buildMTPTestArgs (DotNet.MTPTestTarget.Project "tests/My.Tests/My.Tests.fsproj") param

              let expected =
                  [ "--project"
                    "tests/My.Tests/My.Tests.fsproj"
                    "--configuration"
                    "Debug"
                    "--no-build"
                    "--list-tests" ]

              Expect.equal args expected "Listing the tests needs no separator, it is an option of dotnet test."

          testCase "Test that dotnet test for Microsoft.Testing.Platform passes all options of dotnet test"
          <| fun _ ->
              let param =
                  { DotNet.MTPTestOptions.Create() with
                      Configuration = Some(DotNet.BuildConfiguration.Custom "Staging")
                      Framework = Some "net10.0"
                      Runtime = Some "linux-x64"
                      Architecture = Some "x64"
                      OS = Some "linux"
                      NoRestore = true
                      NoBuild = true
                      Properties = [ "DefineConstants", "A;B"; "Version", "1.2.3" ]
                      MaxParallelTestModules = Some 2
                      ListTests = true
                      MinimumExpectedTests = Some 10
                      ResultsDirectory = Some "TestResults"
                      ConfigFile = Some "my.testconfig.json"
                      NoLaunchProfile = true
                      NoLaunchProfileArguments = true
                      NoProgress = true
                      NoAnsi = true
                      Output = Some DotNet.MTPOutputVerbosity.Detailed }

              let args = DotNet.buildMTPTestArgs (DotNet.MTPTestTarget.Solution "My.slnx") param

              let expected =
                  [ "--solution"
                    "My.slnx"
                    "--configuration"
                    "Staging"
                    "--framework"
                    "net10.0"
                    "--runtime"
                    "linux-x64"
                    "--arch"
                    "x64"
                    "--os"
                    "linux"
                    "--no-restore"
                    "--no-build"
                    "--property:DefineConstants=A%3BB"
                    "--property:Version=1.2.3"
                    "--max-parallel-test-modules"
                    "2"
                    "--list-tests"
                    "--minimum-expected-tests"
                    "10"
                    "--results-directory"
                    "TestResults"
                    "--config-file"
                    "my.testconfig.json"
                    "--no-launch-profile"
                    "--no-launch-profile-arguments"
                    "--no-progress"
                    "--no-ansi"
                    "--output"
                    "Detailed" ]

              Expect.equal args expected "No separator is added when nothing is passed to the test applications."

          testCase "Test that dotnet test for Microsoft.Testing.Platform passes the options of the test applications"
          <| fun _ ->
              let param =
                  { DotNet.MTPTestOptions.Create() with
                      Filter = Some "TestCategory=Unit&Priority=1"
                      Timeout = Some(System.TimeSpan.FromMinutes 5.0)
                      IgnoreExitCodes = [ 2; 8 ]
                      Diagnostic =
                          Some
                              { DotNet.MTPDiagnosticOptions.Create() with
                                  Verbosity = Some DotNet.MTPDiagnosticVerbosity.Debug
                                  OutputDirectory = Some "TestResults/diagnostic"
                                  FilePrefix = Some "my"
                                  SynchronousWrite = true }
                      TrxReport = Some { DotNet.MTPTrxReportOptions.Create() with FileName = Some "my.trx" }
                      CodeCoverage =
                          Some
                              { DotNet.MTPCodeCoverageOptions.Create() with
                                  Output = Some "my.coverage.xml"
                                  OutputFormat = Some DotNet.MTPCoverageFormat.Xml
                                  Settings = Some "coverage.settings.xml" }
                      CrashDump =
                          Some
                              { DotNet.MTPCrashDumpOptions.Create() with
                                  FileName = Some "crash.dmp"
                                  DumpType = Some DotNet.MTPDumpType.Heap }
                      HangDump =
                          Some
                              { DotNet.MTPHangDumpOptions.Create() with
                                  FileName = Some "hang.dmp"
                                  Timeout = Some(System.TimeSpan.FromSeconds 90.5)
                                  DumpType = Some DotNet.MTPDumpType.Mini }
                      ApplicationArguments = [ "--retry-failed-tests"; "3" ] }

              let args = DotNet.buildMTPTestArgs DotNet.MTPTestTarget.CurrentDirectory param

              let expected =
                  [ "--diagnostic-output-directory"
                    "TestResults/diagnostic"
                    "--"
                    "--filter"
                    "TestCategory=Unit&Priority=1"
                    "--timeout"
                    "300s"
                    "--ignore-exit-code"
                    "2;8"
                    "--diagnostic"
                    "--diagnostic-verbosity"
                    "Debug"
                    "--diagnostic-file-prefix"
                    "my"
                    "--diagnostic-synchronous-write"
                    "--report-trx"
                    "--report-trx-filename"
                    "my.trx"
                    "--coverage"
                    "--coverage-output"
                    "my.coverage.xml"
                    "--coverage-output-format"
                    "xml"
                    "--coverage-settings"
                    "coverage.settings.xml"
                    "--crashdump"
                    "--crashdump-filename"
                    "crash.dmp"
                    "--crashdump-type"
                    "Heap"
                    "--hangdump"
                    "--hangdump-filename"
                    "hang.dmp"
                    "--hangdump-timeout"
                    "91s"
                    "--hangdump-type"
                    "Mini"
                    "--retry-failed-tests"
                    "3" ]

              Expect.equal
                  args
                  expected
                  "Only the diagnostic output directory is an option of dotnet test, and durations are whole seconds."

          testCase "Test that dotnet test for Microsoft.Testing.Platform turns extensions on without their details"
          <| fun _ ->
              let param =
                  { DotNet.MTPTestOptions.Create() with
                      Diagnostic = Some(DotNet.MTPDiagnosticOptions.Create())
                      TrxReport = Some(DotNet.MTPTrxReportOptions.Create())
                      CodeCoverage = Some(DotNet.MTPCodeCoverageOptions.Create())
                      CrashDump = Some(DotNet.MTPCrashDumpOptions.Create())
                      HangDump = Some(DotNet.MTPHangDumpOptions.Create()) }

              let args = DotNet.buildMTPTestArgs DotNet.MTPTestTarget.CurrentDirectory param

              Expect.equal
                  args
                  [ "--"
                    "--diagnostic"
                    "--report-trx"
                    "--coverage"
                    "--crashdump"
                    "--hangdump" ]
                  "Every extension is turned on by its own switch."

          testCase "Test that dotnet test for Microsoft.Testing.Platform never passes an empty argument"
          <| fun _ ->
              let param =
                  { DotNet.MTPTestOptions.Create() with
                      Configuration = Some(DotNet.BuildConfiguration.Custom "")
                      Framework = Some ""
                      ResultsDirectory = Some " "
                      Filter = Some ""
                      Diagnostic = Some { DotNet.MTPDiagnosticOptions.Create() with OutputDirectory = Some "" }
                      TrxReport = Some { DotNet.MTPTrxReportOptions.Create() with FileName = Some "" }
                      ApplicationArguments = [ ""; "--no-banner"; " " ] }

              let args =
                  DotNet.buildMTPTestArgs (DotNet.MTPTestTarget.TestModules("**/*.Tests.dll", Some "")) param

              Expect.equal
                  args
                  [ "--test-modules"
                    "**/*.Tests.dll"
                    "--"
                    "--diagnostic"
                    "--report-trx"
                    "--no-banner" ]
                  "A blank value removes its option, a test application rejects an empty argument."

          testCase "Test that dotnet test for Microsoft.Testing.Platform rejects a blank project or solution"
          <| fun _ ->
              let build target () =
                  DotNet.buildMTPTestArgs target (DotNet.MTPTestOptions.Create()) |> ignore

              Expect.throwsT<System.ArgumentException>
                  (build (DotNet.MTPTestTarget.Project ""))
                  "A blank project is not the working directory."

              Expect.throwsT<System.ArgumentException>
                  (build (DotNet.MTPTestTarget.Solution " "))
                  "A blank solution is not the working directory."

              Expect.throwsT<System.ArgumentException>
                  (build (DotNet.MTPTestTarget.TestModules("", None)))
                  "A blank expression selects no test modules."

          testCase "Test that dotnet test for Microsoft.Testing.Platform quotes paths with spaces"
          <| fun _ ->
              let param =
                  { DotNet.MTPTestOptions.Create() with
                      NoBuild = true
                      ResultsDirectory = Some "C:\\My Work\\TestResults"
                      TrxReport = Some(DotNet.MTPTrxReportOptions.Create()) }

              let cli =
                  DotNet.buildMTPTestArgs (DotNet.MTPTestTarget.Project "C:\\My Work\\My.Tests.fsproj") param
                  |> Args.toWindowsCommandLine

              let expected =
                  "--project \"C:\\My Work\\My.Tests.fsproj\" --no-build --results-directory \"C:\\My Work\\TestResults\" -- --report-trx"

              Expect.equal cli expected "Arguments are quoted one by one."

          ]
