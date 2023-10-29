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
              let param =
                  { DotNet.PublishOptions.Create() with
                      SelfContained = Some false }

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
                  { DotNet.PublishOptions.Create() with
                      Manifest = Some [ "Path1"; "Path2" ] }

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

              Expect.equal cli expected "New --uninstall args generated correctly." ]
