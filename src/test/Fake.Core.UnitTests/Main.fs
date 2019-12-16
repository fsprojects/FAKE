module Main.Tests
open Expecto
open System
open Fake.ExpectoSupport


[<EntryPoint>]
let main argv =
    ExpectoHelpers.setThreadPool()
    let writeResults = TestResults.writeNUnitSummary ("Fake_Core_UnitTests.TestResults.xml", "Fake.Core.UnitTests")
    let config = 
        defaultConfig.appendSummaryHandler writeResults
        |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(20.))
    Tests.runTestsInAssembly config argv
