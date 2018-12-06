module Main.Tests
open Expecto

[<EntryPoint>]
let main argv =
    let writeResults = TestResults.writeNUnitSummary ("Fake_DotNet_Cli_IntegrationTests.TestResults.xml", "Fake.DotNet.Cli.IntegrationTests")
    let config = defaultConfig.appendSummaryHandler writeResults
    Tests.runTestsInAssembly { config with parallel = false } argv
