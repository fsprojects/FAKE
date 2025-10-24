module Main.Tests

open Expecto
open Expecto.Impl
open Expecto.Logging
open Fake.ExpectoSupport
open System

[<EntryPoint>]
let main argv =

    testFromThisAssembly ()
    |> Option.defaultValue (TestList([], Normal))
    |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(20.))
    |> Tests.runTestsWithCLIArgs
        [| Tests.CLIArguments.Sequenced
           Tests.CLIArguments.Verbosity LogLevel.Debug
           Tests.CLIArguments.Printer ExpectoHelpers.fakeDefaultPrinter |]
        argv
