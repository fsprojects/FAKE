#r @"FakeLib.dll"

open Fake

Target "Default" DoNothing

RunTargetOrDefault "Default"
