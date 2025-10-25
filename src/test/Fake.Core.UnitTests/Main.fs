module Main.Tests

open Expecto
open System
open Expecto.Impl
open Expecto.Logging
open Fake.ExpectoSupport


[<EntryPoint>]
let main argv =

    testFromThisAssembly ()
    |> Option.defaultValue (TestList([], Normal))
    |> ExpectoHelpers.addTimeout (TimeSpan.FromMinutes(20.))
    |> Tests.runTestsWithCLIArgs
        [| Tests.CLIArguments.Verbosity LogLevel.Debug
           Tests.CLIArguments.Printer ExpectoHelpers.fakeDefaultPrinter |]
        argv
