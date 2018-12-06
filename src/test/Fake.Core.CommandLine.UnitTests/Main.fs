module Main.Tests
open Expecto

[<EntryPoint>]
let main argv =
    let writeResults = TestResults.writeNUnitSummary ("Fake_Core_CommandLine_UnitTests.TestResults.xml", "Fake.Core.CommandLine.UnitTests")
    let config = defaultConfig.appendSummaryHandler writeResults
    Tests.runTestsInAssembly { config with parallel = false } argv
