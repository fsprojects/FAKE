module Main.Tests

open Expecto
open System
open Expecto.Impl
open Expecto.Logging
open Fake.ExpectoSupport


[<EntryPoint>]
let main argv =
    let config =
        defaultConfig
        |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(30.))

    Tests.runTestsInAssembly
        { config with
              runInParallel = false
              parallelWorkers = 0
              printer = TestPrinters.summaryWithLocationPrinter (TestPrinters.defaultPrinter)
              verbosity = LogLevel.Debug }
        argv
