module Main.Tests

open Expecto
open System
open Expecto.Impl
open Expecto.Logging
open Fake.ExpectoSupport


[<EntryPoint>]
let main argv =
    let config = defaultConfig |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(30.))

    Tests.runTestsInAssemblyWithCLIArgs [| Tests.CLIArguments.Verbosity LogLevel.Debug |] argv
