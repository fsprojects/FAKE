module Main.Tests
open Expecto

[<EntryPoint>]
let main argv =
    let writeResults = TestResults.writeNUnitSummary ("Fake_Core_UnitTests.TestResults.xml", "Fake.Core.UnitTests")
    let config = defaultConfig.appendSummaryHandler writeResults
    Tests.runTestsInAssembly config argv
