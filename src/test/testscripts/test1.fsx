#r @"../../app/FAKE/bin/Debug/FakeLib.dll"

open Fake

Target "Default" DoNothing


RunTargetOrDefault "Default"

if getBuildParam "test" = "" then
    failwith "test param is missing"


System.Console.ReadKey() |> ignore