module Main.Tests
open Expecto
open Fake.ExpectoSupport
open System

[<EntryPoint>]
let main argv =
    let writeResults = TestResults.writeNUnitSummary ("Fake_Core_CommandLine_UnitTests.TestResults.xml", "Fake.Core.CommandLine.UnitTests")
    let config =
        defaultConfig.appendSummaryHandler writeResults
        |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(20.))

    FakeExpecto.Tests.runTestsInAssembly { config with parallel = false } argv
