#r @"../../app/FAKE/bin/Debug/FakeLib.dll"

open Fake

Target "Default" DoNothing


RunTargetOrDefault "Default"

System.Console.ReadKey() |> ignore