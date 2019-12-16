module Main.Tests
open Expecto
open Fake.ExpectoSupport
open System

[<EntryPoint>]
let main argv =
    ExpectoHelpers.setThreadPool()
    let writeResults = TestResults.writeNUnitSummary ("Fake_Core_IntegrationTests.TestResults.xml", "Fake.Core.IntegrationTests")
    let config =
        defaultConfig.appendSummaryHandler writeResults
        |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(20.))
    Tests.runTestsInAssembly { config with parallel = false } argv
