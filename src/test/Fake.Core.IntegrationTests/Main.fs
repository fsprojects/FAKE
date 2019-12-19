module Main.Tests
open Expecto
open Fake.ExpectoSupport
open System

[<EntryPoint>]
let main argv =
    let writeResults = TestResults.writeNUnitSummary ("Fake_Core_IntegrationTests.TestResults.xml", "Fake.Core.IntegrationTests")
    let config =
        defaultConfig
        |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(20.))
        |> ExpectoHelpers.setFakePrinter
        |> ExpectoHelpers.appendSummaryHandler writeResults

    Expecto.Tests.runTestsInAssembly { config with parallel = false } argv
