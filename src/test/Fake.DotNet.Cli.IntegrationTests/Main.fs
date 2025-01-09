module Main.Tests

open Expecto
open Expecto.Impl
open Expecto.Logging
open Fake.ExpectoSupport
open System

[<EntryPoint>]
let main argv =
    let config = defaultConfig |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(30.))

    Tests.runTestsInAssemblyWithCLIArgs
        [| Tests.CLIArguments.Sequenced; Tests.CLIArguments.Verbosity LogLevel.Debug |]
        argv
