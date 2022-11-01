module Main.Tests

open Expecto
open Expecto.Impl
open Expecto.Logging
open Fake.ExpectoSupport
open System

[<EntryPoint>]
let main argv =
    let config = defaultConfig |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(20.))

    Tests.runTestsInAssembly
        { config with
            runInParallel = false
            parallelWorkers = 0
            printer = ExpectoHelpers.fakeDefaultPrinter
            verbosity = LogLevel.Debug }
        argv
