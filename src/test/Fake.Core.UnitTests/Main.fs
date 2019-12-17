module Main.Tests
open Expecto
open System
open Fake.ExpectoSupport


[<EntryPoint>]
let main argv =
    let writeResults = TestResults.writeNUnitSummary ("Fake_Core_UnitTests.TestResults.xml", "Fake.Core.UnitTests")
    let config = 
        defaultConfig 
        |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(20.))
        |> ExpectoHelpers.setFakePrinter
        |> ExpectoHelpers.appendSummaryHandler writeResults
    Expecto.Tests.runTestsInAssembly config argv
