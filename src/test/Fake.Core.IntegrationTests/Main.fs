module Main.Tests
open Expecto

[<EntryPoint>]
let main argv =
    let writeResults = TestResults.writeNUnitSummary ("Fake_Core_IntegrationTests.TestResults.xml", "Fake.Core.IntegrationTests")
    let config = defaultConfig.appendSummaryHandler writeResults
    Tests.runTestsInAssembly { config with parallel = false } argv
